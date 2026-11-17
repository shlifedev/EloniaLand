using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ExcelDataReader;

namespace Elonia.DataManager
{
    public sealed class XlsxWorkbookSource : IWorkbookSource
    {
        private const string SEARCH_PATTERN = "*.xlsx";

        private const string LOCK_FILE_PREFIX = "~$";

        private static bool _isEncodingRegistered;

        private readonly string _directory;

        public XlsxWorkbookSource(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                throw new ArgumentException("A workbook source needs a directory.", nameof(directory));
            }

            _directory = directory;
        }

        public IReadOnlyList<Workbook> Load(DiagnosticBag diagnostics)
        {
            List<Workbook> workbooks = new List<Workbook>();
            if (!Directory.Exists(_directory))
            {
                diagnostics.AddError(_directory, "엑셀 파일을 둔 폴더가 없습니다.", "폴더 경로를 확인하세요.");
                return workbooks;
            }

            string[] paths = Directory.GetFiles(_directory, SEARCH_PATTERN, SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.Ordinal);
            for (int index = 0; index < paths.Length; index++)
            {
                string fileName = Path.GetFileName(paths[index]);
                if (fileName.StartsWith(LOCK_FILE_PREFIX, StringComparison.Ordinal))
                {
                    continue;
                }

                try
                {
                    using (FileStream stream = new FileStream(paths[index], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        workbooks.Add(Read(stream, fileName));
                    }
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ExcelDataReader.Exceptions.ExcelReaderException)
                {
                    diagnostics.AddError(fileName, "엑셀 파일을 읽지 못했습니다: " + exception.Message,
                        "파일이 깨지지 않았는지, 다른 프로그램이 잠그고 있지 않은지 확인하세요.");
                }
            }

            if (workbooks.Count == 0 && !diagnostics.HasErrors)
            {
                diagnostics.AddError(_directory, "폴더에 xlsx 파일이 없습니다.", "표를 담은 xlsx 파일을 이 폴더에 두세요.");
            }

            return workbooks;
        }

        public static Workbook Read(Stream stream, string name)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            RegisterEncoding();
            List<Sheet> sheets = new List<Sheet>();
            using (IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
            {
                do
                {
                    List<IReadOnlyList<string?>> rows = new List<IReadOnlyList<string?>>();
                    while (reader.Read())
                    {
                        string?[] cells = new string?[reader.FieldCount];
                        for (int columnIndex = 0; columnIndex < cells.Length; columnIndex++)
                        {
                            cells[columnIndex] = CellText.FromValue(reader.GetValue(columnIndex));
                        }

                        rows.Add(cells);
                    }

                    sheets.Add(new Sheet(name, reader.Name, rows));
                }
                while (reader.NextResult());
            }

            return new Workbook(name, sheets);
        }

        private static void RegisterEncoding()
        {
            if (_isEncodingRegistered)
            {
                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _isEncodingRegistered = true;
        }
    }
}
