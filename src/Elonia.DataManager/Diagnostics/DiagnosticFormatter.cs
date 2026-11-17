using System.Collections.Generic;
using System.Text;

namespace Elonia.DataManager
{
    public static class DiagnosticFormatter
    {
        public static string Format(IReadOnlyList<Diagnostic> diagnostics)
        {
            StringBuilder builder = new StringBuilder();
            for (int index = 0; index < diagnostics.Count; index++)
            {
                Diagnostic diagnostic = diagnostics[index];
                builder.Append(diagnostic.Severity == ESeverity.Error ? "[오류] " : "[경고] ");
                builder.Append(diagnostic.Location).Append('\n');
                if (diagnostic.ValueOrNull != null)
                {
                    builder.Append("  값: \"").Append(diagnostic.ValueOrNull).Append("\"\n");
                }

                builder.Append("  이유: ").Append(diagnostic.Reason).Append('\n');
                if (diagnostic.FixOrNull != null)
                {
                    builder.Append("  고치는 방법: ").Append(diagnostic.FixOrNull).Append('\n');
                }

                builder.Append('\n');
            }

            return builder.ToString();
        }
    }
}
