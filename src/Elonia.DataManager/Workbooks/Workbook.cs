using System;
using System.Collections.Generic;

namespace Elonia.DataManager
{
    public sealed class Workbook
    {
        private readonly List<Sheet> _sheets;

        public string Name { get; }
        public IReadOnlyList<Sheet> Sheets => _sheets;

        public Workbook(string name, IEnumerable<Sheet> sheets)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (sheets == null)
            {
                throw new ArgumentNullException(nameof(sheets));
            }

            Name = name;
            _sheets = new List<Sheet>(sheets);
        }
    }

    public interface IWorkbookSource
    {
        IReadOnlyList<Workbook> Load(DiagnosticBag diagnostics);
    }

    public sealed class MemoryWorkbookSource : IWorkbookSource
    {
        private readonly List<Workbook> _workbooks;

        public MemoryWorkbookSource(params Workbook[] workbooks)
        {
            if (workbooks == null)
            {
                throw new ArgumentNullException(nameof(workbooks));
            }

            _workbooks = new List<Workbook>(workbooks);
        }

        public IReadOnlyList<Workbook> Load(DiagnosticBag diagnostics)
        {
            return _workbooks;
        }
    }
}
