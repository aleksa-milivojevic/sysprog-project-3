using Akka.Actor;
using Akka.Event;

namespace Actors;

public class ActorSupervisor : UntypedActor
{
    public ILoggingAdapter Log { get; } = Context.GetLogger();

    protected override void PreStart() => Log.Info("Supervisor start");
    protected override void PostStop() => Log.Info("Supervisor stop");

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case "start":
                var actorManagerRef = Context.ActorOf(ActorManager.Props(), "manager");
                Sender.Tell(actorManagerRef);
                break;
        }
    }

    public static Props Props() => Akka.Actor.Props.Create<ActorSupervisor>();
}