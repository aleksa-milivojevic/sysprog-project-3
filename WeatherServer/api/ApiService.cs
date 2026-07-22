using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http; 

public class ApiService
{
    private readonly HttpClient _http;
    private const string ApiKey = "057ba3b660b74297a14202941262606";

    public ApiService(HttpClient http) {
        _http = http;
    }

    public async Task<HttpResponseMessage> FetchWeather(string location) {
        string url = $"http://api.weatherapi.com/v1/forecast.json?key={ApiKey}&q={location}&days=14&aqi=yes&alerts=no";
        
        return await _http.GetAsync(url);
    }
}