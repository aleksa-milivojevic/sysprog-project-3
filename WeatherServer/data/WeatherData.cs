public class WeatherData
{
    public string Date { get; set; }
    public double Avg { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public Aqi Aqi { get; set; }

    public WeatherData(string date, double avg, double min, double max, Aqi aqi) {
        Date = date;
        Avg = avg;
        Min = min;
        Max = max;
        Aqi = aqi;
    }

    public override string ToString() {
        return $"date = {Date}\n avg_temp = {Avg}\n min_temp = {Min}\n max_temp = {Max}\n aqi = " + Aqi.ToString();
    }
}