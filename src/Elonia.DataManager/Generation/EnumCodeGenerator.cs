using System.Collections.Generic;
using System.Globalization;

namespace Elonia.DataManager
{
    public static class EnumCodeGenerator
    {
        public const string FILE_NAME = "Enums.g.cs";

        public static string Generate(IReadOnlyList<EnumDefinition> enums, string namespaceName)
        {
            CodeWriter writer = new CodeWriter();
            writer.FileHeader("Enums 시트와 Mapping<...> 열");
            writer.Line("using System;");
            writer.Line();
            writer.Line("namespace " + namespaceName);
            writer.Open();
            for (int index = 0; index < enums.Count; index++)
            {
                if (index > 0)
                {
                    writer.Line();
                }

                EnumDefinition definition = enums[index];
                writer.Summary(definition.Comment);
                if (definition.IsFlags)
                {
                    writer.Line("[Flags]");
                }

                writer.Line("public enum " + definition.Name + " : " + EnumUnderlying.GetCSharpName(definition.Underlying));
                writer.Open();
                for (int memberIndex = 0; memberIndex < definition.Members.Count; memberIndex++)
                {
                    EnumMember member = definition.Members[memberIndex];
                    writer.Line(member.Name + " = " + member.Value.ToString(CultureInfo.InvariantCulture) + ",");
                }

                writer.Close();
            }

            writer.Close();
            return writer.ToString();
        }
    }
}
