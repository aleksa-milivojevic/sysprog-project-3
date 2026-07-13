using Akka.Actor;
using Akka.Event;
using System.Collections.Generic;

namespace Actors;

public class ActorManager : UntypedActor
{
    private Dictionary<string, IActorRef> IdToActor = new();
    private Dictionary<IActorRef, string> ActorToId = new();

    protected override void PreStart() => Log.Info("ActorManager started");
    protected override void PostStop() => Log.Info("ActorManager stopped");

    protected ILoggingAdapter Log { get; } = Context.GetLogger();

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case GetWeatherData msg:
                if (IdToActor.TryGetValue(msg.ActorId, out var actorRef))
                {
                    actorRef.Forward(msg);
                }
                else
                {
                    Log.Warning($"Location actor not found for {msg.ActorId}");
                }
                break;
            case ProcessWeatherData msg:
                if (IdToActor.TryGetValue(msg.ActorId, out var aref))
                {
                    aref.Forward(msg);
                }
                else
                {
                    Log.Info($"Creating location actor for {msg.ActorId}");
                    var actor = Context.ActorOf(LocationActor.Props().WithDispatcher("weather-dispatcher"), $"actor-{msg.ActorId}");
                    Context.Watch(actor);
                    actor.Forward(msg);
                    IdToActor.Add(msg.ActorId, actor);
                    ActorToId.Add(actor, msg.ActorId);
                }
                break;
            case Terminated t:
                var actorId = ActorToId[t.ActorRef];
                Log.Info($"Location actor for {actorId} has been terminated");
                ActorToId.Remove(t.ActorRef);
                IdToActor.Remove(actorId);
                break;
        }
    }

    protected override void Unhandled(object message)
    {
        Console.WriteLine(message.ToString());
    }

    public static Props Props() => Akka.Actor.Props.Create<ActorManager>();
}