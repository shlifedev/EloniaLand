using Cysharp.Text;
using Elonia.Data;
using Elonia.Events;
using Elonia.Simulation;

namespace Elonia.Game
{
    internal sealed class MessageFeed : IEventListener<ActorDied>, IEventListener<ItemPickedUp>, IEventListener<PropHarvested>,
        IEventListener<StepTriggered>, IEventListener<ActorDamaged>
    {
        private readonly Actor _player;
        private readonly EventBus _events;

        public MessageFeed(Actor player, EventBus events)
        {
            _player = player;
            _events = events;
        }

        public void OnEvent(ActorDied message)
        {
            if (message.Actor == _player)
            {
                Log("당신은 쓰러졌다...", ELogTone.Danger);
            }
            else
            {
                string name = GameText.GetActorTypeName(message.Actor.Row());
                Log(ZString.Concat(name, GameText.GetObjectParticle(name), " 쓰러뜨렸다."), ELogTone.Good);
            }
        }

        public void OnEvent(ItemPickedUp message)
        {
            if (message.Actor != _player)
            {
                return;
            }

            string name = GameText.GetThingName(message.Thing.Row());
            switch (message.Effect)
            {
                case EItemEffect.Heal:
                    Log(ZString.Concat(name, GameText.GetObjectParticle(name), " 마셨다. (체력 +", message.Amount, ")"), ELogTone.Good);
                    break;
                case EItemEffect.Gold:
                    Log(ZString.Concat(message.Amount, " 골드를 주웠다."), ELogTone.Gold);
                    break;
                default:
                    Log(ZString.Concat(name, GameText.GetObjectParticle(name), " 주웠다. (x", message.Amount, ")"), ELogTone.Normal);
                    break;
            }
        }

        public void OnEvent(PropHarvested message)
        {
            if (message.Actor != _player)
            {
                return;
            }

            string propName = GameText.GetPropName(message.HarvestedProp);
            string thingName = GameText.GetThingName(message.DroppedThing.Row());
            Log(ZString.Concat(propName, GameText.GetObjectParticle(propName), " 베었다. ",
                thingName, GameText.GetSubjectParticle(thingName), " 떨어졌다."), ELogTone.Normal);
        }

        public void OnEvent(StepTriggered message)
        {
            if (message.Actor == _player)
            {
                string name = GameText.GetThingName(message.Thing.Row());
                Log(ZString.Concat(name, GameText.GetObjectParticle(name), " 밟았다!"), ELogTone.Danger);
            }
        }

        public void OnEvent(ActorDamaged message)
        {
            Actor? sourceOrNull = message.SourceOrNull;
            if (sourceOrNull == null)
            {
                if (message.Actor == _player)
                {
                    Log(ZString.Concat(message.Damage, "의 피해를 입었다."), ELogTone.Danger);
                }
            }
            else if (sourceOrNull == _player)
            {
                Log(ZString.Concat(GameText.GetActorTypeName(message.Actor.Row()), "에게 ", message.Damage, "의 피해를 주었다."), ELogTone.Normal);
            }
            else if (message.Actor == _player)
            {
                Log(ZString.Concat(GameText.GetActorTypeName(sourceOrNull.Row()), "의 공격! ", message.Damage, "의 피해를 입었다."), ELogTone.Danger);
            }
        }

        private void Log(string text, ELogTone tone)
        {
            _events.Broadcast(new MessageLogged(text, tone));
        }
    }
}
