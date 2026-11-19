using System;

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
            Thing? thingOrNull = context.Map.GetItemAtOrNull(actor.X, actor.Y);
            ItemPart? itemOrNull = thingOrNull?.Type.GetPartOrNull<ItemPart>();
            if (thingOrNull == null || itemOrNull == null)
            {
                return ActionResult.REJECTED;
            }

            int amount = itemOrNull.Amount * thingOrNull.Count;
            switch (itemOrNull.Effect)
            {
                case EItemEffect.Heal:
                    actor.Heal(amount);
                    break;
                case EItemEffect.Gold:
                    actor.AddGold(amount);
                    break;
                case EItemEffect.None:
                    actor.Inventory.Add(thingOrNull.Type, amount);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown item effect {itemOrNull.Effect}.");
            }

            context.Map.RemoveThing(thingOrNull);
            context.World.Events.Broadcast(new ItemPickedUp(actor, thingOrNull, itemOrNull.Effect, amount));
            return ActionResult.Executed(DEFAULT_COST);
        }
    }
}
