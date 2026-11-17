namespace Elonia.Simulation
{
    public sealed class PickUpAction : ActorAction
    {
        public static readonly PickUpAction DEFAULT = new PickUpAction();

        public const int REACH = 0;

        public override string Name => "PICK UP";

        public override ActionResult Execute(ActionContext context)
        {
            Actor actor = context.Actor;
            Item? itemOrNull = context.Map.GetItemAtOrNull(actor.X, actor.Y);
            if (itemOrNull == null)
            {
                return ActionResult.REJECTED;
            }

            switch (itemOrNull.Type)
            {
                case EItemType.Potion:
                    actor.Heal(itemOrNull.Amount);
                    break;
                case EItemType.Gold:
                    actor.AddGold(itemOrNull.Amount);
                    break;
                default:
                    actor.Inventory.Add(itemOrNull.Type, itemOrNull.Amount);
                    break;
            }

            context.Map.RemoveItem(itemOrNull);
            context.World.Events.Broadcast(new ItemPickedUp(actor, itemOrNull));
            return ActionResult.Executed(DEFAULT_COST);
        }
    }
}
