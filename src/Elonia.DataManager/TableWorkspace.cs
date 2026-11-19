using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Elonia.DataManager
{
    public sealed class TableWorkspace
    {
        private const string INPUT_PATTERN = "*.xlsx";

        private const string LOCK_FILE_PREFIX = "~$";

        private readonly IWorkbookSource _source;

        private readonly Dictionary<CellOrigin, Diagnostic> _diagnosticsByCell = new Dictionary<CellOrigin, Diagnostic>();

        private string _inputStamp = "";

        public string InputDirectory { get; } = string.Empty;
        public string CodeDirectory { get; }
        public string DataDirectory { get; }
        public TableBuildOptions Options { get; }

        public TableBuildResult? ResultOrNull { get; private set; }

        public TableWorkspace(string inputDirectory, string codeDirectory, string dataDirectory, TableBuildOptions options)
            : this(new XlsxWorkbookSource(inputDirectory), codeDirectory, dataDirectory, options)
        {
            string inputPath = Path.GetFullPath(inputDirectory);
            if (inputPath == Path.GetFullPath(codeDirectory) || inputPath == Path.GetFullPath(dataDirectory))
            {
                throw new ArgumentException("The input directory must differ from the output directories.", nameof(inputDirectory));
            }

            InputDirectory = inputDirectory;
        }

        public TableWorkspace(IWorkbookSource source, string codeDirectory, string dataDirectory, TableBuildOptions options)
        {
            if (string.IsNullOrEmpty(codeDirectory) || string.IsNullOrEmpty(dataDirectory))
            {
                throw new ArgumentException("A workspace needs a code directory and a data directory.");
            }

            _source = source ?? throw new ArgumentNullException(nameof(source));
            CodeDirectory = codeDirectory;
            DataDirectory = dataDirectory;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public TableBuildResult Refresh()
        {
            if (InputDirectory.Length > 0)
            {
                _inputStamp = ReadInputStamp();
            }

            TableBuildResult result = TableBuild.Run(_source, Options);
            ResultOrNull = result;

            _diagnosticsByCell.Clear();
            IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics.Items;
            for (int index = 0; index < diagnostics.Count; index++)
            {
                Diagnostic diagnostic = diagnostics[index];
                if (diagnostic.OriginOrNull == null)
                {
                    continue;
                }

                CellOrigin origin = diagnostic.OriginOrNull.Value;
                bool bReplace = !_diagnosticsByCell.TryGetValue(origin, out Diagnostic? existing)
                    || (existing.Severity == ESeverity.Warning && diagnostic.Severity == ESeverity.Error);
                if (bReplace)
                {
                    _diagnosticsByCell[origin] = diagnostic;
                }
            }

            return result;
        }

        public CommitReport? BuildOrNull()
        {
            TableBuildResult result = Refresh();
            if (result.OutputOrNull == null)
            {
                return null;
            }

            return OutputCommitter.Commit(result.OutputOrNull, CodeDirectory, DataDirectory);
        }

        public bool HasInputChanged()
        {
            return InputDirectory.Length > 0 && ReadInputStamp() != _inputStamp;
        }

        public Diagnostic? FindDiagnosticOrNull(CellOrigin origin)
        {
            return _diagnosticsByCell.TryGetValue(origin, out Diagnostic? diagnostic) ? diagnostic : null;
        }

        public void CountProblems(Sheet sheet, out int errorCount, out int warningCount)
        {
            errorCount = 0;
            warningCount = 0;
            if (ResultOrNull == null)
            {
                return;
            }

            IReadOnlyList<Diagnostic> diagnostics = ResultOrNull.Diagnostics.Items;
            for (int index = 0; index < diagnostics.Count; index++)
            {
                Diagnostic diagnostic = diagnostics[index];
                bool bInSheet = diagnostic.OriginOrNull != null
                    ? diagnostic.OriginOrNull.Value.Sheet == sheet.Name && diagnostic.OriginOrNull.Value.Workbook == sheet.WorkbookName
                    : diagnostic.Location == sheet.Location;
                if (!bInSheet)
                {
                    continue;
                }

                if (diagnostic.Severity == ESeverity.Error)
                {
                    errorCount++;
                }
                else
                {
                    warningCount++;
                }
            }
        }

        private string ReadInputStamp()
        {
            if (!Directory.Exists(InputDirectory))
            {
                return "";
            }

            string[] paths = Directory.GetFiles(InputDirectory, INPUT_PATTERN, SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.Ordinal);
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < paths.Length; index++)
            {
                string fileName = Path.GetFileName(paths[index]);
                if (fileName.StartsWith(LOCK_FILE_PREFIX, StringComparison.Ordinal))
                {
                    continue;
                }

                FileInfo info = new FileInfo(paths[index]);
                builder.Append(fileName).Append('|')
                    .Append(info.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(info.Length.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            return builder.ToString();
        }
    }
}
