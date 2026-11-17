#nullable enable

using System;
using System.Collections.Generic;
using MemoryPack;

namespace Elonia.Checks.Generated
{
    [MemoryPackable(SerializeLayout.Explicit)]
    public sealed partial class PhysicsRow
    {
        [MemoryPackOrder(0)]
        public float Gravity { get; }

        [MemoryPackOrder(1)]
        public int MaxSpeed { get; }

        [MemoryPackOrder(2)]
        public bool IsEnabled { get; }

        [MemoryPackConstructor]
        public PhysicsRow(float gravity, int maxSpeed, bool isEnabled)
        {
            Gravity = gravity;
            MaxSpeed = maxSpeed;
            IsEnabled = isEnabled;
        }
    }
}
