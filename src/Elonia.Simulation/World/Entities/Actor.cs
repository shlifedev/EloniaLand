using System;
using System.Diagnostics;

namespace Elonia.Simulation
{
    public sealed class Actor : Entity
    {
        public ActorType Type { get; }
        public int Hp { get; private set; }
        public int Gold { get; private set; }
        public long ActionCount { get; private set; }
        public long Energy { get; private set; }

        public bool IsAlive => Hp > 0;

        public IActorController? ControllerOrNull { get; }

        public Inventory Inventory { get; } = new Inventory();

        public Actor(int id, ActorType type, int x, int y, IActorController? controllerOrNull = null)
            : base(id, x, y)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
            Hp = type.MaxHp;
            ControllerOrNull = controllerOrNull;
        }

        internal void AddEnergy(long amount)
        {
            Debug.Assert(amount > 0);
            Energy += amount;
        }

        internal void RecordAction(int cost)
        {
            Debug.Assert(cost > 0);
            Energy -= cost;
            ActionCount++;
        }

        internal void TakeDamage(int amount)
        {
            Debug.Assert(amount >= 0);
            Hp = Math.Max(0, Hp - amount);
        }

        internal void Heal(int amount)
        {
            Debug.Assert(amount >= 0);
            Hp = Math.Min(Type.MaxHp, Hp + amount);
        }

        internal void AddGold(int amount)
        {
            Debug.Assert(amount >= 0);
            Gold += amount;
        }
    }
}
