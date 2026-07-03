using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http; 

public class ApiService
{
    private readonly HttpClient _http;
    private const string ApiKey = "";

    public ApiService(HttpClient http) {
        _http = http;
    }

    public async Task<List<JObject>> FetchWeather(string location, int days) {
        string url = $"http://api.weatherapi.com/v1/forecast.json?key={ApiKey}&q={location}&days={days}&aqi=yes&alerts=no";
        
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode) {
            throw new Exception ("Api call unsuccesfull.");
        }

        return await Format(response);
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