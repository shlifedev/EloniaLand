#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Checks.Generated
{
    public sealed partial class PhysicsTable
    {
        public const string TABLE_NAME = "Physics";

        private readonly PhysicsRow[] _rows;

        public IReadOnlyList<PhysicsRow> Rows => _rows;
        public int Count => _rows.Length;

        public PhysicsTable(PhysicsRow[] rows)
        {
            _rows = rows;
        }
    }
}
