using System;
using System.Diagnostics;

namespace Elonia.Simulation
{
    public sealed class Actor : Entity
    {
        public IActorDef Def { get; }

        public IActorStats Stats { get; }

        public int Hp { get; private set; }
        public int MaxHp => Stats.MaxHp;
        public int Gold { get; private set; }
        public long ActionCount { get; private set; }
        public long Energy { get; private set; }

        public int Speed => Stats.Speed;

        public bool IsAlive => Hp > 0;

        public bool IsInvincible { get; set; }

        public IActorController? ControllerOrNull { get; }

        public Inventory Inventory { get; } = new Inventory();

        public Actor(int id, IActorDef def, IActorStats stats, int x, int y, IActorController? controllerOrNull = null)
            : base(id, x, y)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }

            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (stats.MaxHp <= 0 || stats.Speed <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stats), "An actor needs positive max HP and speed.");
            }

            Def = def;
            Stats = stats;
            Hp = stats.MaxHp;
            ControllerOrNull = controllerOrNull;
        }

        public void RestoreState(int hp, int gold, long actionCount, long energy, bool bInvincible)
        {
            if (MapOrNull != null)
            {
                throw new InvalidOperationException("맵에 배치한 액터의 상태를 직접 복원할 수 없습니다.");
            }

            if (hp < 0 || gold < 0 || actionCount < 0)
            {
                throw new ArgumentException("저장된 체력, 골드, 행동 횟수는 음수일 수 없습니다.");
            }

            if (energy < (long)GameRunner.READY_ENERGY - int.MaxValue || energy > (long)GameRunner.READY_ENERGY - 1 + int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(energy));
            }

            Hp = Math.Min(hp, MaxHp);
            Gold = gold;
            ActionCount = actionCount;
            Energy = energy;
            IsInvincible = bInvincible;
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

            if (IsInvincible)
            {
                return;
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

            if (Hp < MaxHp)
            {
                Hp += Math.Min(amount, MaxHp - Hp);
            }
        }

        public void ClampHpToMax()
        {
            if (Hp > MaxHp)
            {
                Hp = MaxHp;
            }
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
