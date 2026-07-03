using System.Collections.Generic;

public sealed class WeatherDataReport
{
    public WeatherReport Report { get; set; }
    public Dictionary<string,WeatherData> List { get; set; }

    public WeatherDataReport(WeatherReport report, Dictionary<string,WeatherData> list)
    {
        Report = report;
        List = list;
    }
}