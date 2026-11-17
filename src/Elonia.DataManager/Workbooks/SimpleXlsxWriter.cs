using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Elonia.DataManager
{
    public sealed class XlsxSheetContent
    {
        public string Name { get; }
        public List<IReadOnlyList<object?>> Rows { get; } = new List<IReadOnlyList<object?>>();

        public List<string> MergedRanges { get; } = new List<string>();

        public XlsxSheetContent(string name)
        {
            Name = name;
        }

        public void AddRow(params object?[] cells)
        {
            Rows.Add(cells);
        }
    }

    public sealed class XlsxFormula
    {
        public string Formula { get; }
        public double CachedValue { get; }

        public XlsxFormula(string formula, double cachedValue)
        {
            Formula = formula;
            CachedValue = cachedValue;
        }
    }

    public static class SimpleXlsxWriter
    {
        private const string MAIN_NAMESPACE = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RELATIONSHIP_NAMESPACE = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string PACKAGE_RELATIONSHIP_NAMESPACE = "http://schemas.openxmlformats.org/package/2006/relationships";
        private const string XML_HEADER = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>";

        private const int STYLE_NORMAL = 0;
        private const int STYLE_HEADER = 1;
        private const int STYLE_DATE = 2;

        private const double MIN_COLUMN_WIDTH = 8.0;
        private const double MAX_COLUMN_WIDTH = 60.0;

        private static readonly DateTime EXCEL_EPOCH = new DateTime(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);

        public static void Write(Stream stream, IReadOnlyList<XlsxSheetContent> sheets, int headerRowCount)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (sheets == null || sheets.Count == 0)
            {
                throw new ArgumentException("A workbook needs at least one sheet.", nameof(sheets));
            }

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                AddEntry(archive, "[Content_Types].xml", CreateContentTypes(sheets.Count));
                AddEntry(archive, "_rels/.rels", XML_HEADER + "<Relationships xmlns=\"" + PACKAGE_RELATIONSHIP_NAMESPACE + "\"><Relationship Id=\"rId1\" Type=\"" + RELATIONSHIP_NAMESPACE + "/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                AddEntry(archive, "xl/workbook.xml", CreateWorkbook(sheets));
                AddEntry(archive, "xl/_rels/workbook.xml.rels", CreateWorkbookRelationships(sheets.Count));
                AddEntry(archive, "xl/styles.xml", CreateStyles());
                for (int index = 0; index < sheets.Count; index++)
                {
                    AddEntry(archive, "xl/worksheets/sheet" + ToText(index + 1) + ".xml", CreateSheet(sheets[index], headerRowCount));
                }
            }
        }

        private static string CreateContentTypes(int sheetCount)
        {
            StringBuilder builder = new StringBuilder(XML_HEADER);
            builder.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            builder.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            builder.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            builder.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            builder.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            for (int index = 1; index <= sheetCount; index++)
            {
                builder.Append("<Override PartName=\"/xl/worksheets/sheet").Append(ToText(index)).Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            }

            return builder.Append("</Types>").ToString();
        }

        private static string CreateWorkbook(IReadOnlyList<XlsxSheetContent> sheets)
        {
            StringBuilder builder = new StringBuilder(XML_HEADER);
            builder.Append("<workbook xmlns=\"").Append(MAIN_NAMESPACE).Append("\" xmlns:r=\"").Append(RELATIONSHIP_NAMESPACE).Append("\"><sheets>");
            for (int index = 0; index < sheets.Count; index++)
            {
                builder.Append("<sheet name=\"").Append(Escape(sheets[index].Name)).Append("\" sheetId=\"").Append(ToText(index + 1)).Append("\" r:id=\"rId").Append(ToText(index + 1)).Append("\"/>");
            }

            return builder.Append("</sheets></workbook>").ToString();
        }

        private static string CreateWorkbookRelationships(int sheetCount)
        {
            StringBuilder builder = new StringBuilder(XML_HEADER);
            builder.Append("<Relationships xmlns=\"").Append(PACKAGE_RELATIONSHIP_NAMESPACE).Append("\">");
            for (int index = 1; index <= sheetCount; index++)
            {
                builder.Append("<Relationship Id=\"rId").Append(ToText(index)).Append("\" Type=\"").Append(RELATIONSHIP_NAMESPACE).Append("/worksheet\" Target=\"worksheets/sheet").Append(ToText(index)).Append(".xml\"/>");
            }

            builder.Append("<Relationship Id=\"rId").Append(ToText(sheetCount + 1)).Append("\" Type=\"").Append(RELATIONSHIP_NAMESPACE).Append("/styles\" Target=\"styles.xml\"/>");
            return builder.Append("</Relationships>").ToString();
        }

        private static string CreateStyles()
        {
            return XML_HEADER
                + "<styleSheet xmlns=\"" + MAIN_NAMESPACE + "\">"
                + "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>"
                + "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>"
                + "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>"
                + "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>"
                + "<cellXfs count=\"3\">"
                + "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>"
                + "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/>"
                + "<xf numFmtId=\"14\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>"
                + "</cellXfs></styleSheet>";
        }

        private static string CreateSheet(XlsxSheetContent sheet, int headerRowCount)
        {
            StringBuilder builder = new StringBuilder(XML_HEADER);
            builder.Append("<worksheet xmlns=\"").Append(MAIN_NAMESPACE).Append("\">");
            if (headerRowCount > 0)
            {
                builder.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"").Append(ToText(headerRowCount))
                    .Append("\" topLeftCell=\"A").Append(ToText(headerRowCount + 1)).Append("\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            }

            AppendColumnWidths(builder, sheet);
            builder.Append("<sheetData>");
            for (int rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
            {
                IReadOnlyList<object?> cells = sheet.Rows[rowIndex];
                builder.Append("<row r=\"").Append(ToText(rowIndex + 1)).Append("\">");
                for (int columnIndex = 0; columnIndex < cells.Count; columnIndex++)
                {
                    string address = CellOrigin.GetColumnLetters(columnIndex + 1) + ToText(rowIndex + 1);
                    AppendCell(builder, address, cells[columnIndex], rowIndex < headerRowCount ? STYLE_HEADER : STYLE_NORMAL);
                }

                builder.Append("</row>");
            }

            builder.Append("</sheetData>");
            if (sheet.MergedRanges.Count > 0)
            {
                builder.Append("<mergeCells count=\"").Append(ToText(sheet.MergedRanges.Count)).Append("\">");
                for (int index = 0; index < sheet.MergedRanges.Count; index++)
                {
                    builder.Append("<mergeCell ref=\"").Append(Escape(sheet.MergedRanges[index])).Append("\"/>");
                }

                builder.Append("</mergeCells>");
            }

            return builder.Append("</worksheet>").ToString();
        }

        private static void AppendColumnWidths(StringBuilder builder, XlsxSheetContent sheet)
        {
            List<double> widths = new List<double>();
            for (int rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
            {
                IReadOnlyList<object?> cells = sheet.Rows[rowIndex];
                for (int columnIndex = 0; columnIndex < cells.Count; columnIndex++)
                {
                    while (widths.Count <= columnIndex)
                    {
                        widths.Add(MIN_COLUMN_WIDTH);
                    }

                    widths[columnIndex] = Math.Max(widths[columnIndex], Math.Min(MAX_COLUMN_WIDTH, MeasureText(cells[columnIndex]) + 2.0));
                }
            }

            if (widths.Count == 0)
            {
                return;
            }

            builder.Append("<cols>");
            for (int index = 0; index < widths.Count; index++)
            {
                builder.Append("<col min=\"").Append(ToText(index + 1)).Append("\" max=\"").Append(ToText(index + 1))
                    .Append("\" width=\"").Append(widths[index].ToString("0.##", CultureInfo.InvariantCulture)).Append("\" customWidth=\"1\"/>");
            }

            builder.Append("</cols>");
        }

        private static double MeasureText(object? valueOrNull)
        {
            string text = valueOrNull is XlsxFormula ? "0000000000" : Convert.ToString(valueOrNull, CultureInfo.InvariantCulture) ?? "";
            double width = 0.0;
            for (int index = 0; index < text.Length; index++)
            {
                width += text[index] > 0x2E7F ? 2.0 : 1.0;
            }

            return width;
        }

        private static void AppendCell(StringBuilder builder, string address, object? valueOrNull, int style)
        {
            switch (valueOrNull)
            {
                case null:
                    return;
                case string text:
                    builder.Append("<c r=\"").Append(address).Append("\" s=\"").Append(ToText(style)).Append("\" t=\"inlineStr\"><is><t xml:space=\"preserve\">").Append(Escape(text)).Append("</t></is></c>");
                    return;
                case bool bValue:
                    builder.Append("<c r=\"").Append(address).Append("\" s=\"").Append(ToText(style)).Append("\" t=\"b\"><v>").Append(bValue ? "1" : "0").Append("</v></c>");
                    return;
                case XlsxFormula formula:
                    builder.Append("<c r=\"").Append(address).Append("\" s=\"").Append(ToText(style)).Append("\"><f>").Append(Escape(formula.Formula)).Append("</f><v>").Append(formula.CachedValue.ToString("R", CultureInfo.InvariantCulture)).Append("</v></c>");
                    return;
                case DateTime dateTime:
                    double serial = (dateTime - EXCEL_EPOCH).TotalDays;
                    builder.Append("<c r=\"").Append(address).Append("\" s=\"").Append(ToText(STYLE_DATE)).Append("\"><v>").Append(serial.ToString("R", CultureInfo.InvariantCulture)).Append("</v></c>");
                    return;
                case IFormattable number:
                    builder.Append("<c r=\"").Append(address).Append("\" s=\"").Append(ToText(style)).Append("\"><v>").Append(number.ToString(null, CultureInfo.InvariantCulture)).Append("</v></c>");
                    return;
                default:
                    throw new ArgumentException($"A cell cannot hold a value of type {valueOrNull.GetType().Name}.");
            }
        }

        private static void AddEntry(ZipArchive archive, string name, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            using (Stream entryStream = entry.Open())
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(content);
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        private static string Escape(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string ToText(int number)
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }
    }
}
