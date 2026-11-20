using System;
using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
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
            if (thingOrNull == null)
            {
                return ActionResult.REJECTED;
            }

            ItemRow item = context.World.Tables().Item.Get(thingOrNull.Row().Item);
            int amount = checked(item.Amount * thingOrNull.Count);
            switch (item.Effect)
            {
                case EItemEffect.Heal:
                    actor.Heal(amount);
                    break;
                case EItemEffect.Gold:
                    actor.AddGold(amount);
                    break;
                case EItemEffect.None:
                    actor.Inventory.Add(thingOrNull.Def, amount);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown item effect {item.Effect}.");
            }

            context.Map.RemoveThing(thingOrNull);
            context.World.Events.Broadcast(new ItemPickedUp(actor, thingOrNull, item.Effect, amount));
            return ActionResult.Executed(DEFAULT_COST);
        }
    }
}
