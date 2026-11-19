using System;
using System.Collections.Generic;

namespace Elonia.DataManager
{
    public sealed class WorkbookInfo
    {
        public string Name { get; }
        public IReadOnlyList<string> SheetNames { get; }

        public WorkbookInfo(string name, IReadOnlyList<string> sheetNames)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SheetNames = sheetNames ?? throw new ArgumentNullException(nameof(sheetNames));
        }

        public static WorkbookInfo FromWorkbook(Workbook workbook)
        {
            if (workbook == null)
            {
                throw new ArgumentNullException(nameof(workbook));
            }

            string[] names = new string[workbook.Sheets.Count];
            for (int index = 0; index < names.Length; index++)
            {
                names[index] = workbook.Sheets[index].Name;
            }
            return new WorkbookInfo(workbook.Name, names);
        }
    }
}
