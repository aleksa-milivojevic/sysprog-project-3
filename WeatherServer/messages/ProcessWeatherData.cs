using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public sealed class ProcessWeatherData
{
    public string ActorId { get; }
    public JObject Data { get; }

    public ProcessWeatherData(string actorId, JObject data)
    {
        ActorId = actorId;
        Data = data;
    }
}