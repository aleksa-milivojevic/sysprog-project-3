public sealed class GetWeatherData
{
    public string ActorId { get; }
    public DateTime FromDate { get; }
    public DateTime ToDate { get; }
    
    public GetWeatherData(string actorId, DateTime fromDate, DateTime toDate)
    {
        ActorId = actorId;
        FromDate = fromDate;
        ToDate = toDate;
    }
}