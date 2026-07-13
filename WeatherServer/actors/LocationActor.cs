using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Akka.Actor;
using Akka.Event;

public sealed class LocationActor : UntypedActor
{
    private Dictionary<string,WeatherData> _history = new();

    protected override void PreStart() => Log.Info("ActorManager started");
    protected override void PostStop() => Log.Info("ActorManager stopped");

    private ILoggingAdapter Log { get; } = Context.GetLogger();

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case ProcessWeatherData msg:
                var date = msg.Data["date"]?.Value<string>() ?? "";
                if (_history.TryGetValue(date, out var weatherData))
                {
                    _history[date].Avg = msg.Data["day"]["avgtemp_c"]?.Value<double>() ?? throw new Exception($"avg for date: {date} is null");
                    _history[date].Min = msg.Data["day"]["mintemp_c"]?.Value<double>() ?? throw new Exception($"min for date: {date} is null");
                    _history[date].Max = msg.Data["day"]["maxtemp_c"]?.Value<double>() ?? throw new Exception($"max for date: {date} is null");
                    if (msg.Data["day"]["air_quality"] is JObject aqij) {
                        _history[date].Aqi = new Aqi(aqij);
                    }
                    else {
                        Console.WriteLine("aqi je null");
                        _history[date].Aqi = new Aqi();
                    }
                }
                else
                {
                    var avg = msg.Data["day"]["avgtemp_c"]?.Value<double>() ?? throw new Exception($"avg for date: {date} is null");
                    var min = msg.Data["day"]["mintemp_c"]?.Value<double>() ?? throw new Exception($"min for date: {date} is null");
                    var max = msg.Data["day"]["maxtemp_c"]?.Value<double>() ?? throw new Exception($"max for date: {date} is null");
                    Aqi aqi;
                    if (msg.Data["day"]["air_quality"] is JObject aqij) {
                        aqi = new Aqi(aqij);
                    }
                    else {
                        Console.WriteLine("aqi je null");
                        aqi = new Aqi();
                    }
                    _history.Add(date, new WeatherData(date, avg, min, max, aqi));
                }
                break;
            case GetWeatherData msg:
                var fromDate = msg.FromDate.Date.ToString("yyyy-MM-dd");
                var toDate = msg.ToDate.Date.ToString("yyyy-MM-dd");
                if (_history.TryGetValue(fromDate, out var weather1) && _history.TryGetValue(toDate, out var weather2))
                {
                    double avgs = 0;
                    double mins = 100;
                    double maxs = 0;
                    Aqi aqis = new Aqi();
                    Dictionary<string,WeatherData> fullData = new Dictionary<string,WeatherData>();
                    for (var dat = msg.FromDate.Date; dat <= msg.ToDate.Date; dat = dat.AddDays(1)) {
                        var day = _history[dat.ToString("yyyy-MM-dd")];
                        avgs += day.Avg;
                        mins = (mins > day.Min && day.Min != 0) ? day.Min : mins;
                        maxs = (maxs < day.Max) ? day.Max : maxs;
                        aqis.Add(day.Aqi);
                        fullData.Add(dat.ToString("yyyy-MM-dd"), day);
                    }
                    var days = (msg.ToDate.Date - msg.FromDate.Date).TotalDays + 1;
                    var aqiAvailableDays = (msg.ToDate.Date > DateTime.Now.Date.AddDays(4)) ? days - (msg.ToDate.Date - DateTime.Now.Date.AddDays(4)).TotalDays : days;
                    aqis.Divide(aqiAvailableDays);
                    var result = new WeatherReport(msg.FromDate.Date, msg.ToDate.Date, avgs / days, mins, maxs, aqis);
                    Sender.Tell(new WeatherDataReport(result, fullData));
                    break;
                }
                else
                {
                    if ((msg.FromDate.Date < DateTime.Now.Date) || (msg.ToDate.Date > DateTime.Now.Date.AddDays(13)))
                    {
                        Sender.Tell(RequestOutOfBounds.Instance);
                    }
                    else
                    {
                        Sender.Tell(WeatherDataNotFound.Instance);
                    }
                    break;
                }
        }
    }

    protected override void Unhandled(object message)
    {
        Console.WriteLine("[Unsupported message] " + message.ToString());
    }

    public static Props Props() => Akka.Actor.Props.Create<LocationActor>();
}