using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Elonia.DataManager.Runtime;

namespace Elonia.DataManager
{
    public sealed class CommitReport
    {
        public List<string> Written { get; } = new List<string>();
        public List<string> Deleted { get; } = new List<string>();
        public int UnchangedCount { get; set; }
        public bool HasChanges => Written.Count > 0 || Deleted.Count > 0;
    }

    public static class OutputCommitter
    {
        private const string CODE_PATTERN = "*.g.cs";
        private const string DATA_PATTERN = "*" + TableManifest.DATA_EXTENSION;

        private static readonly UTF8Encoding UTF8_WITHOUT_BOM = new UTF8Encoding(false);

        public static CommitReport Commit(GeneratedOutput output, string codeDirectory, string dataDirectory)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            CommitReport report = new CommitReport();
            List<FileChange> changes = new List<FileChange>();
            HashSet<string> keptPaths = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> file in output.CodeFiles)
            {
                PlanWrite(Path.Combine(codeDirectory, file.Key), UTF8_WITHOUT_BOM.GetBytes(file.Value), file.Key, changes, keptPaths, report);
            }

            foreach (KeyValuePair<string, byte[]> file in output.DataFiles)
            {
                PlanWrite(Path.Combine(dataDirectory, file.Key), file.Value, file.Key, changes, keptPaths, report);
            }

            PlanDeletes(codeDirectory, CODE_PATTERN, true, changes, keptPaths);
            PlanDeletes(dataDirectory, DATA_PATTERN, false, changes, keptPaths);
            PlanDeletes(dataDirectory, TableManifest.FILE_NAME, false, changes, keptPaths);
            if (changes.Count == 0)
            {
                return report;
            }

            Directory.CreateDirectory(codeDirectory);
            Directory.CreateDirectory(dataDirectory);
            int appliedCount = 0;
            try
            {
                for (int index = 0; index < changes.Count; index++)
                {
                    FileChange change = changes[index];
                    change.OriginalOrNull = File.Exists(change.Path) ? File.ReadAllBytes(change.Path) : null;
                    appliedCount++;
                    if (change.ContentOrNull == null)
                    {
                        File.Delete(change.Path);
                        report.Deleted.Add(change.Name);
                    }
                    else
                    {
                        File.WriteAllBytes(change.Path, change.ContentOrNull);
                        report.Written.Add(change.Name);
                    }
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Rollback(changes, appliedCount);
                throw new IOException("결과를 쓰지 못해 원래 상태로 되돌렸습니다: " + exception.Message, exception);
            }

            return report;
        }

        private static void PlanWrite(string path, byte[] content, string name, List<FileChange> changes, HashSet<string> keptPaths, CommitReport report)
        {
            keptPaths.Add(Path.GetFullPath(path));
            if (File.Exists(path) && AreEqual(File.ReadAllBytes(path), content))
            {
                report.UnchangedCount++;
                return;
            }

            changes.Add(new FileChange(path, name, content));
        }

        private static void PlanDeletes(string directory, string pattern, bool bRequireMarker, List<FileChange> changes, HashSet<string> keptPaths)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            string[] paths = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.Ordinal);
            for (int index = 0; index < paths.Length; index++)
            {
                if (keptPaths.Contains(Path.GetFullPath(paths[index])))
                {
                    continue;
                }

                if (bRequireMarker && !File.ReadAllText(paths[index]).StartsWith(CodeWriter.GENERATED_MARKER, StringComparison.Ordinal))
                {
                    continue;
                }

                changes.Add(new FileChange(paths[index], Path.GetFileName(paths[index]), null));
            }
        }

        private static void Rollback(List<FileChange> changes, int appliedCount)
        {
            for (int index = appliedCount - 1; index >= 0; index--)
            {
                FileChange change = changes[index];
                try
                {
                    if (change.OriginalOrNull == null)
                    {
                        File.Delete(change.Path);
                    }
                    else
                    {
                        File.WriteAllBytes(change.Path, change.OriginalOrNull);
                    }
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                }
            }
        }

        private static bool AreEqual(byte[] left, byte[] right)
        {
            return left.AsSpan().SequenceEqual(right);
        }

        private sealed class FileChange
        {
            public string Path { get; }
            public string Name { get; }
            public byte[]? ContentOrNull { get; }

            public byte[]? OriginalOrNull { get; set; }

            public FileChange(string path, string name, byte[]? contentOrNull)
            {
                Path = path;
                Name = name;
                ContentOrNull = contentOrNull;
            }
        }
    }
}
