using System;

namespace Elonia.DataManager.Runtime
{
    public sealed class TableLoadException : Exception
    {
        public TableLoadException(string message)
            : base(message)
        {
        }

        public TableLoadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
