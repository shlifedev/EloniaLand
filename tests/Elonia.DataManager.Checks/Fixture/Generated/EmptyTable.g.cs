#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Elonia.Checks.Generated
{
    public sealed partial class EmptyTable
    {
        public const string TABLE_NAME = "Empty";

        private readonly EmptyRow[] _rows;

        public IReadOnlyList<EmptyRow> Rows => _rows;
        public int Count => _rows.Length;

        public EmptyTable(EmptyRow[] rows)
        {
            _rows = rows;
        }
    }
}
