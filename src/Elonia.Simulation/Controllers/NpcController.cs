namespace Elonia.Simulation
{
    public sealed class NpcController : IActorController
    {
        private MoveAction _direction = MoveAction.RIGHT;

        public ActorAction ChooseAction(ActionContext context)
        {
            Actor actor = context.Actor;
            if (!context.Map.CanEnter(actor.X + _direction.DeltaX, actor.Y))
            {
                _direction = _direction == MoveAction.RIGHT ? MoveAction.LEFT : MoveAction.RIGHT;
            }

            return _direction;
        }
    }
}
