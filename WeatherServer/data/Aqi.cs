using Newtonsoft.Json.Linq;

public class Aqi
{
    public double Co { get; set; }
    public double No2 { get; set; }
    public double O3 { get; set; }
    public double So2 { get; set; }
    public double Pm2_5 { get; set; }
    public double Pm10 { get; set; }
    public int Us_Epa_Index { get; set; }
    public int Gb_Defra_Index { get; set; }

    public Aqi() {
        Co = 0;
        No2 = 0;
        O3 = 0;
        So2 = 0;
        Pm2_5 = 0;
        Pm10 = 0;
        Us_Epa_Index = 0;
        Gb_Defra_Index = 0;
    }

    public Aqi(JObject data) {
        Co = data["co"]?.Value<double>() ?? 0;
        No2 = data["no2"]?.Value<double>() ?? 0;
        O3 = data["o3"]?.Value<double>() ?? 0;
        So2 = data["so2"]?.Value<double>() ?? 0;
        Pm2_5 = data["pm2_5"]?.Value<double>() ?? 0;
        Pm10 = data["pm10"]?.Value<double>() ?? 0;
        Us_Epa_Index = data["us-epa-index"]?.Value<int>() ?? 0;
        Gb_Defra_Index = data["gb-defra-index"]?.Value<int>() ?? 0;
    }

    public override string ToString() {
        return $"co = {Co}, no = {No2}, o3 = {O3}, so2 = {So2}, pm2_5 = {Pm2_5}, pm10 = {Pm10}, us-epa-index = {Us_Epa_Index}, gb_defra_index = {Gb_Defra_Index}";
    }

    public void Add(Aqi aqi) {
        Co += aqi.Co;
        No2 += aqi.No2;
        O3 += aqi.O3;
        So2 += aqi.So2;
        Pm2_5 += aqi.Pm2_5;
        Pm10 += aqi.Pm10;
        Us_Epa_Index += aqi.Us_Epa_Index;
        Gb_Defra_Index += aqi.Gb_Defra_Index;
    }

    public void Divide(double num) {
        Co /= num;
        No2 /= num;
        O3 /= num;
        So2 /= num;
        Pm2_5 /= num;
        Pm10 /= num;
        Us_Epa_Index /= (int)num;
        Gb_Defra_Index /= (int)num;
    }
}