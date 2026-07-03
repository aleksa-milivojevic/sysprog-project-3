using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Actors;
using Akka.Configuration;

class Program
{
    private static ActorSystem _actorSystem;
    private static IActorRef _actorManager;
    private static WeatherApi _weatherApi;

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

        try
        {
            listener.Start();
            Console.WriteLine($"[HTTP SERVER] Sluša na adresi {url}");
            Console.WriteLine("Primer upita: http://localhost:5000/?location=Belgrade&from=2026-06-28&to=2026-06-29");

            while (listener.IsListening)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context));
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

        string location = request.QueryString["location"];
        string fromStr = request.QueryString["from"];
        string toStr = request.QueryString["to"];

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
            Console.WriteLine($"\n[HTTP] Zahtev za lokaciju: {location} ({fromStr} do {toStr})");

            var requestMessage = new GetWeatherData(location, fromDate, toDate);
            object actorResponse = await _actorManager.Ask(requestMessage, TimeSpan.FromSeconds(3));

            if (actorResponse is WeatherDataNotFound)
            {
                Console.WriteLine($"[HTTP] Podaci za {location} nisu u aktorima. Salje se api zahtev...");
                
                int days = (toDate.Date - DateTime.Now.Date).Days + 1;
                if (days < 1) days = 1;

                await _weatherApi.FetchWeather(location, days);

                Console.WriteLine($"[HTTP] Ponovni upit ka aktoru za {location}...");
                actorResponse = await _actorManager.Ask(requestMessage, TimeSpan.FromSeconds(3));
            }

            switch (actorResponse)
            {
                case WeatherDataReport message:
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                    Respond(response, json, HttpStatusCode.OK, "application/json");
                    break;

                case RequestOutOfBounds:
                    Respond(response, "Zahtevani datumi su van dozvoljenog opsega (istorijski podaci ili preko 14 dana unapred).", HttpStatusCode.BadRequest);
                    break;

                default:
                    Respond(response, "Podaci nisu dostupni ni nakon API poziva.", HttpStatusCode.NotFound);
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
}