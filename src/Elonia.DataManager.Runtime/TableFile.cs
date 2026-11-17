using System;
using MemoryPack;

namespace Elonia.DataManager.Runtime
{
    public static class TableFile
    {
        public static TRow[] ReadRows<TRow>(TableManifest manifest, string tableName, Func<string, byte[]> readFile)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            if (readFile == null)
            {
                throw new ArgumentNullException(nameof(readFile));
            }

            TableManifestEntry entry = manifest.GetTable(tableName);
            string fileName = TableManifest.GetDataFileName(tableName);
            byte[]? bytesOrNull = readFile(fileName);
            if (bytesOrNull == null)
            {
                throw new TableLoadException($"The table file '{fileName}' could not be read.");
            }

            string fileHash = TableHash.Compute(bytesOrNull);
            if (fileHash != entry.FileHash)
            {
                throw new TableLoadException(
                    $"The table file '{fileName}' does not match the manifest (file {fileHash}, manifest {entry.FileHash}). The manifest and the data files are from different builds.");
            }

            TRow[]? rowsOrNull;
            try
            {
                rowsOrNull = MemoryPackSerializer.Deserialize<TRow[]>(bytesOrNull);
            }
            catch (MemoryPackSerializationException exception)
            {
                throw new TableLoadException($"The table file '{fileName}' could not be decoded.", exception);
            }

            if (rowsOrNull == null || rowsOrNull.Length != entry.RowCount)
            {
                throw new TableLoadException($"The table file '{fileName}' does not hold the {entry.RowCount} rows the manifest lists.");
            }

            return rowsOrNull;
        }
    }
}
