using System;
using System.Buffers;
using MemoryPack;

namespace Elonia.DataManager
{
    public static class TableEncoder
    {
        public static byte[] Encode(TableData table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            TableSchema schema = table.Schema;
            int memberCount = 0;
            for (int index = 0; index < schema.Columns.Count; index++)
            {
                memberCount += schema.Columns[index].Type is RefType ? 2 : 1;
            }

            ArrayBufferWriter<byte> buffer = new ArrayBufferWriter<byte>();
            using (MemoryPackWriterOptionalState state = MemoryPackWriterOptionalStatePool.Rent(MemoryPackSerializerOptions.Default))
            {
                MemoryPackWriter<ArrayBufferWriter<byte>> writer = new MemoryPackWriter<ArrayBufferWriter<byte>>(ref buffer, state);
                writer.WriteCollectionHeader(table.Rows.Count);
                for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
                {
                    object[] values = table.Rows[rowIndex].Values;
                    writer.WriteObjectHeader((byte)memberCount);
                    for (int columnIndex = 0; columnIndex < schema.Columns.Count; columnIndex++)
                    {
                        schema.Columns[columnIndex].Type.Write(ref writer, values[columnIndex]);
                    }
                }

                writer.Flush();
            }

            return buffer.WrittenSpan.ToArray();
        }
    }
}
