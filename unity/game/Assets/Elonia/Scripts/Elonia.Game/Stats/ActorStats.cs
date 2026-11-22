using System;
using System.Collections.Generic;
using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class ActorStats : IActorStats
    {
        public const string CHEAT_SOURCE = "cheat";

        public static readonly int STAT_COUNT = Enum.GetValues(typeof(EStat)).Length;

        private readonly int[] _baseValues = new int[STAT_COUNT];

        private readonly int[] _modifierSums = new int[STAT_COUNT];
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        public ActorStats(ActorRow row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            _baseValues[(int)EStat.MaxHp] = row.MaxHp;
            _baseValues[(int)EStat.Attack] = row.Attack;
            _baseValues[(int)EStat.Speed] = row.Speed;
        }

        public int MaxHp => Get(EStat.MaxHp);
        public int Speed => Get(EStat.Speed);

        public IReadOnlyList<StatModifier> Modifiers => _modifiers;

        public int Get(EStat stat)
        {
            int index = GetIndex(stat);
            return Math.Max(GetMinValue(stat), _baseValues[index] + _modifierSums[index]);
        }

        public int GetBase(EStat stat)
        {
            return _baseValues[GetIndex(stat)];
        }

        public int GetModifierSum(EStat stat)
        {
            return _modifierSums[GetIndex(stat)];
        }

        public int GetModifier(string source, EStat stat)
        {
            int index = FindModifier(source, stat);
            return index >= 0 ? _modifiers[index].Amount : 0;
        }

        internal void SetModifier(string source, EStat stat, int amount)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            int statIndex = GetIndex(stat);
            int index = FindModifier(source, stat);
            if (index >= 0)
            {
                _modifierSums[statIndex] -= _modifiers[index].Amount;
                _modifiers.RemoveAt(index);
            }

            if (amount != 0)
            {
                _modifiers.Add(new StatModifier(source, stat, amount));
                _modifierSums[statIndex] += amount;
            }
        }

        internal void RemoveModifiers(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            for (int index = _modifiers.Count - 1; index >= 0; index--)
            {
                StatModifier modifier = _modifiers[index];
                if (string.Equals(modifier.Source, source, StringComparison.Ordinal))
                {
                    _modifierSums[(int)modifier.Stat] -= modifier.Amount;
                    _modifiers.RemoveAt(index);
                }
            }
        }

        private int FindModifier(string source, EStat stat)
        {
            for (int index = 0; index < _modifiers.Count; index++)
            {
                StatModifier modifier = _modifiers[index];
                if (modifier.Stat == stat && string.Equals(modifier.Source, source, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        public static int GetMinValue(EStat stat)
        {
            switch (stat)
            {
                case EStat.MaxHp:
                case EStat.Speed:
                    return 1;
                default:
                    return 0;
            }
        }

        private static int GetIndex(EStat stat)
        {
            int index = (int)stat;
            if (index < 0 || index >= STAT_COUNT)
            {
                throw new ArgumentOutOfRangeException(nameof(stat));
            }

            return index;
        }
    }
}
