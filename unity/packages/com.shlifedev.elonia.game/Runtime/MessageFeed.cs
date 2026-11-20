using Cysharp.Text;
using Elonia.Events;
using Elonia.Simulation;

namespace Elonia.Game
{
    internal sealed class MessageFeed : IEventListener<ActorAttacked>, IEventListener<ActorDied>, IEventListener<ItemPickedUp>, IEventListener<PropHarvested>,
        IEventListener<StepTriggered>, IEventListener<ActorDamaged>
    {
        private readonly Actor _player;
        private readonly EventBus _events;

        public MessageFeed(Actor player, EventBus events)
        {
            _player = player;
            _events = events;
        }

        public void OnEvent(ActorAttacked message)
        {
            if (message.Attacker == _player)
            {
                Log(ZString.Concat(GameText.GetActorTypeName(message.Target.Type), "에게 ", message.Damage, "의 피해를 주었다."), ELogTone.Normal);
            }
            else if (message.Target == _player)
            {
                Log(ZString.Concat(GameText.GetActorTypeName(message.Attacker.Type), "의 공격! ", message.Damage, "의 피해를 입었다."), ELogTone.Danger);
            }
        }

        public void OnEvent(ActorDied message)
        {
            if (message.Actor == _player)
            {
                Log("당신은 쓰러졌다...", ELogTone.Danger);
            }
            else
            {
                Log(ZString.Concat(GameText.WithObjectParticle(GameText.GetActorTypeName(message.Actor.Type)), " 쓰러뜨렸다."), ELogTone.Good);
            }
        }

        public void OnEvent(ItemPickedUp message)
        {
            if (message.Actor != _player)
            {
                return;
            }

            string name = GameText.GetThingName(message.Thing.Type);
            switch (message.Effect)
            {
                case EItemEffect.Heal:
                    Log(ZString.Concat(GameText.WithObjectParticle(name), " 마셨다. (체력 +", message.Amount, ")"), ELogTone.Good);
                    break;
                case EItemEffect.Gold:
                    Log(ZString.Concat(message.Amount, " 골드를 주웠다."), ELogTone.Gold);
                    break;
                default:
                    Log(ZString.Concat(GameText.WithObjectParticle(name), " 주웠다. (x", message.Amount, ")"), ELogTone.Normal);
                    break;
            }
        }

        public void OnEvent(PropHarvested message)
        {
            if (message.Actor != _player)
            {
                return;
            }

            Log(ZString.Concat(GameText.WithObjectParticle(GameText.GetPropName(message.HarvestedProp)), " 베었다. ",
                GameText.WithSubjectParticle(GameText.GetThingName(message.DroppedThing.Type)), " 떨어졌다."), ELogTone.Normal);
        }

        public void OnEvent(StepTriggered message)
        {
            if (message.Actor == _player)
            {
                Log(ZString.Concat(GameText.WithObjectParticle(GameText.GetThingName(message.Thing.Type)), " 밟았다!"), ELogTone.Danger);
            }
        }

        public void OnEvent(ActorDamaged message)
        {
            if (message.Actor == _player)
            {
                Log(ZString.Concat(message.Damage, "의 피해를 입었다."), ELogTone.Danger);
            }
        }

        private void Log(string text, ELogTone tone)
        {
            _events.Broadcast(new MessageLogged(text, tone));
        }
    }
}
