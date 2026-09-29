namespace Crulanda.Gameplay
{
    /// <summary>Published on the EventBus when an actor's health reaches zero.</summary>
    public struct ActorDiedEvent
    {
        public Actor Actor;

        public ActorDiedEvent(Actor actor)
        {
            Actor = actor;
        }
    }
}
