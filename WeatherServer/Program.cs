using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Actors;
using Akka.Configuration;
using System.Runtime.InteropServices;

class Program
{
    private static ActorSystem? _actorSystem;
    private static IActorRef? _actorManager;
    private static WeatherApi? _weatherApi;
    private static CancellationTokenSource? _cts;

    static async Task Main(string[] args)
    {
        string hoconConfig = @"
            weather-dispatcher {
                type = Dispatcher
                executor = ""fork-join-executor""
                fork-join-executor {
                    parallelism-min = 2
                    parallelism-factor = 2.0
                    parallelism-max = 8
                }
                throughput = 100
            }
        ";

        Config config = ConfigurationFactory.ParseString(hoconConfig);

        _actorSystem = ActorSystem.Create("WeatherSystem", config);
        
        var supervisor = _actorSystem.ActorOf(ActorSupervisor.Props(), "supervisor");
        _actorManager = await supervisor.Ask<IActorRef>("start");

        var httpClient = new HttpClient();
        var apiService = new ApiService(httpClient);
        _weatherApi = new WeatherApi(apiService, _actorManager);

        string url = "http://localhost:5000/";
        using HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);

        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;

        try
        {
            var gracefulShutdown = Task.Run(async () => await GracefulShutdown());

            listener.Start();
            Console.WriteLine($"[HTTP SERVER] Sluša na adresi {url}");
            Console.WriteLine("Primer upita: http://localhost:5000/?location=Belgrade&from=2026-06-28&to=2026-06-29");

            var weatherCaller = Task.Run( async () => {
                while (true) {
                    Console.WriteLine($"\nZahtev za lokaciju: Beograd");

                    await _weatherApi.FetchWeather("Belgrade");

                    await Task.Delay(10000);
                }
            }, token);

            while (listener.IsListening)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context), token);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Kritična greška na serveru: {ex.Message}");
        }
        finally
        {
            await _actorSystem.Terminate();
        }
    }

    private static async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        string? location = request.QueryString["location"];
        string? fromStr = request.QueryString["from"];
        string? toStr = request.QueryString["to"];

        if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(fromStr) || string.IsNullOrEmpty(toStr))
        {
            Respond(response, "Nedostaju parametri: location, from ili to.", HttpStatusCode.BadRequest);
            return;
        }

        if (!DateTime.TryParse(fromStr, out DateTime fromDate) || !DateTime.TryParse(toStr, out DateTime toDate))
        {
            Respond(response, "Format datuma mora biti yyyy-MM-dd.", HttpStatusCode.BadRequest);
            return;
        }

        try
        {
            var requestMessage = new GetWeatherData(
                location,
                fromDate,
                toDate
            );
            object actorResponse = await _actorManager.Ask(requestMessage, TimeSpan.FromSeconds(3));

            switch (actorResponse)
            {
                case WeatherDataReport message:
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                    Respond(response, json, HttpStatusCode.OK, "application/json");
                    break;

                case RequestOutOfBounds:
                    Respond(response, "Zahtevani datumi su van dozvoljenog opsega (istorijski podaci ili preko 14 dana unapred).", HttpStatusCode.BadRequest);
                    break;

                case WeatherDataNotFound:
                    Respond(response, "Podaci nisu dostupni.", HttpStatusCode.NotFound);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Greška prilikom obrade: {ex.Message}");
            Respond(response, $"Interna greška: {ex.Message}", HttpStatusCode.InternalServerError);
        }
    }

    private static void Respond(HttpListenerResponse response, string sadrzaj, HttpStatusCode status, string contentType = "text/plain")
    {
        byte[] buffer = Encoding.UTF8.GetBytes(sadrzaj);
        response.StatusCode = (int)status;
        response.ContentType = $"{contentType}; charset=utf-8";
        response.ContentLength64 = buffer.Length;
        
        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        response.Close();
    }

    private static async Task GracefulShutdown() {
            var waitForExit = new ManualResetEventSlim(false);
            
            PosixSignalRegistration.Create(PosixSignal.SIGINT, context => {
                Console.WriteLine($"\n[Server] [{DateTime.Now}] SIGINT called");
                Console.WriteLine($"[Server] [{DateTime.Now}] Shutting down gracefuly...");

                if (_cts != null) _cts.Cancel();

                waitForExit.Set();
            });

            waitForExit.Wait();
        }
}