using System;
using System.Diagnostics;

namespace Elonia.Simulation
{
    public sealed class Actor : Entity
    {
        public IActorDef Def { get; }
        public int Hp { get; private set; }
        public int Gold { get; private set; }
        public long ActionCount { get; private set; }
        public long Energy { get; private set; }

        public bool IsAlive => Hp > 0;

        public IActorController? ControllerOrNull { get; }

        public Inventory Inventory { get; } = new Inventory();

        public Actor(int id, IActorDef def, int x, int y, IActorController? controllerOrNull = null)
            : base(id, x, y)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }

            if (def.MaxHp <= 0 || def.Speed <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(def), "An actor needs positive max HP and speed.");
            }

            Def = def;
            Hp = def.MaxHp;
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

        public void TakeDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Hp = Math.Max(0, Hp - amount);
        }

        public void Heal(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (!IsAlive && amount > 0)
            {
                throw new InvalidOperationException("A dead actor cannot be healed.");
            }

            Hp += Math.Min(amount, Def.MaxHp - Hp);
        }

        public void AddGold(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Gold = checked(Gold + amount);
        }
    }
}
