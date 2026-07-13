using System;
using System.Reactive.Linq;
using Akka.Actor;
using System.Reactive.Concurrency;
using Newtonsoft.Json.Linq;

public class WeatherApi
{
    private readonly ApiService _apiService;
    private readonly IActorRef _actorManager;

    public WeatherApi(ApiService apiService, IActorRef actorManager)
    {
        _apiService = apiService;
        _actorManager = actorManager;
    }

    public Task FetchWeather(string location)
    {
        var tcs = new TaskCompletionSource<bool>();

        Observable.FromAsync(() => _apiService.FetchWeather(location))
                .SubscribeOn(TaskPoolScheduler.Default)
                .SelectMany((response) => {
                        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) {
                            Console.WriteLine("Api call for returned no content");
                            throw new Exception("Api call for returned no content");
                        }
                        if (!response.IsSuccessStatusCode) {
                            Console.WriteLine("Api call for unsuccesfull.");
                            throw new Exception("Api call for unsuccesfull.");
                        }
                        return Observable.FromAsync(() => Format(response));
                    }
                )
                .SelectMany(list => list)
                .Select(r => new ProcessWeatherData(location, r))
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(
                    onNext: msg => _actorManager.Tell(msg),
                    onError: err => {
                        Console.WriteLine($"Greška u Rx toku: {err.Message}");
                        tcs.SetException(err);
                    },
                    onCompleted: () => {
                        Console.WriteLine("Weather fetched");
                        tcs.SetResult(true);
                    }
                );

        return tcs.Task;
    }


    private async Task<List<JObject>> Format(HttpResponseMessage response) {
        var jsonString = await response.Content.ReadAsStringAsync();

        var json = JObject.Parse(jsonString);

        List<JObject> data = new List<JObject>();

        var days = json["forecast"]["forecastday"];

        foreach(JObject day in days) {
            data.Add(day);
        }

        return data;
    }
}