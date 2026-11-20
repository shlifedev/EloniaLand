using System;

namespace Elonia.Simulation
{
    public sealed class ActorType
    {
        public string Id { get; }
        public string Name { get; }
        public int MaxHp { get; }
        public int AttackPower { get; }
        public int Speed { get; }

        public bool IsHostile { get; }

        public bool IsThreat => IsHostile && AttackPower > 0;

        public ActorType(string name, int maxHp, int attackPower, int speed, bool bHostile = false)
            : this(name, name, maxHp, attackPower, speed, bHostile)
        {
        }

        public ActorType(string id, string name, int maxHp, int attackPower, int speed, bool bHostile = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("종류 ID가 필요합니다.", nameof(id));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("An actor type needs a name.", nameof(name));
            }

            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp), "A max HP must be positive.");
            }

            if (attackPower < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attackPower), "An attack power must not be negative.");
            }

            if (speed <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), "An actor speed must be positive.");
            }

            Id = id;
            Name = name;
            MaxHp = maxHp;
            AttackPower = attackPower;
            Speed = speed;
            IsHostile = bHostile;
        }
    }
}
