using System;
using System.Reactive.Linq;
using Akka.Actor;
using System.Reactive.Concurrency;

public class WeatherApi
{
    private readonly ApiService _apiService;
    private readonly IActorRef _actorManager;

    public WeatherApi(ApiService apiService, IActorRef actorManager)
    {
        _apiService = apiService;
        _actorManager = actorManager;
    }

    public Task FetchWeather(string location, int days)
    {
        var tcs = new TaskCompletionSource<bool>();

        Observable.FromAsync(() => _apiService.FetchWeather(location, days))
                .SubscribeOn(TaskPoolScheduler.Default)
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
}