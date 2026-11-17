#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Checks.Generated
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class AllTypesRow
    {
        [MemoryPackOrder(0)]
        public int Id { get; }

        [MemoryPackOrder(1)]
        public uint U { get; }

        [MemoryPackOrder(2)]
        public long L { get; }

        [MemoryPackOrder(3)]
        public ulong UL { get; }

        [MemoryPackOrder(4)]
        public short S { get; }

        [MemoryPackOrder(5)]
        public ushort US { get; }

        [MemoryPackOrder(6)]
        public byte B { get; }

        [MemoryPackOrder(7)]
        public float F { get; }

        [MemoryPackOrder(8)]
        public double D { get; }

        [MemoryPackOrder(9)]
        public decimal M { get; }

        [MemoryPackOrder(10)]
        public bool Flag { get; }

        [MemoryPackOrder(11)]
        public string Text { get; }

        [MemoryPackOrder(12)]
        public DateTime When { get; }

        [MemoryPackOrder(13)]
        public Guid G { get; }

        [MemoryPackOrder(14)]
        public int[] Numbers { get; }

        [MemoryPackOrder(15)]
        public List<string> Tags { get; }

        [MemoryPackOrder(16)]
        public float[] Rates { get; }

        [MemoryPackOrder(17)]
        public List<int> Counts { get; }

        [MemoryPackOrder(18)]
        public EKind[] Kinds { get; }

        [MemoryPackOrder(19)]
        public EAccess Access { get; }

        [MemoryPackOrder(20)]
        public ESmall Small { get; }

        [MemoryPackOrder(21)]
        public EPlain Plain { get; }

        [MemoryPackConstructor]
        public AllTypesRow(int id, uint u, long l, ulong uL, short s, ushort uS, byte b, float f, double d, decimal m, bool flag, string text, DateTime when, Guid g, int[] numbers, List<string> tags, float[] rates, List<int> counts, EKind[] kinds, EAccess access, ESmall small, EPlain plain)
        {
            Id = id;
            U = u;
            L = l;
            UL = uL;
            S = s;
            US = uS;
            B = b;
            F = f;
            D = d;
            M = m;
            Flag = flag;
            Text = text;
            When = when;
            G = g;
            Numbers = numbers;
            Tags = tags;
            Rates = rates;
            Counts = counts;
            Kinds = kinds;
            Access = access;
            Small = small;
            Plain = plain;
        }
    }
}
