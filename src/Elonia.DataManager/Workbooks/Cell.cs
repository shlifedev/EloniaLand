using System.Text;

namespace Elonia.DataManager
{
    public readonly struct CellOrigin
    {
        public string Workbook { get; }
        public string Sheet { get; }

        public int Row { get; }
        public int Column { get; }

        public CellOrigin(string workbook, string sheet, int row, int column)
        {
            Workbook = workbook;
            Sheet = sheet;
            Row = row;
            Column = column;
        }

        public string Address => GetColumnLetters(Column) + Row.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public override string ToString()
        {
            return Workbook + " / " + Sheet + "!" + Address;
        }

        public static string GetColumnLetters(int column)
        {
            StringBuilder builder = new StringBuilder();
            int remaining = column;
            while (remaining > 0)
            {
                int digit = (remaining - 1) % 26;
                builder.Insert(0, (char)('A' + digit));
                remaining = (remaining - 1) / 26;
            }

            return builder.ToString();
        }
    }

    public readonly struct Cell
    {
        public string Text { get; }
        public CellOrigin Origin { get; }

        public Cell(string text, CellOrigin origin)
        {
            Text = text;
            Origin = origin;
        }

        public string Trimmed => Text.Trim();

        public bool IsBlank => string.IsNullOrWhiteSpace(Text);
    }
}
