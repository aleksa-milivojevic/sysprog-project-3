using System;

public class WeatherReport
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public double Avg { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public Aqi Aqi { get; set; }

    public WeatherReport (DateTime fromDate, DateTime toDate, double avg, double min, double max, Aqi aqi) {
        FromDate = fromDate;
        ToDate = toDate;
        Avg = avg;
        Min = min;
        Max = max;
        Aqi = aqi;
    }

    public override string ToString() {
        return $"from = {FromDate.ToString("yyyy-MM-dd")}\n to = {ToDate.ToString("yyyy-MM-dd")}\n avg_temp = {Avg}\n min_temp = {Min}\n max_temp = {Max}\n aqi = " + Aqi.ToString();
    }
}