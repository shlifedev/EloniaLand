using System;
using System.Collections.Generic;
using System.IO;
using Elonia.DataManager;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckFixtureBuilds()
    {
        TableBuildResult result = BuildFixture();
        Check(result.IsSuccess, "The fixture builds without errors." + Environment.NewLine + DiagnosticFormatter.Format(result.Diagnostics.Items));
        CheckEqual(0, result.Diagnostics.ErrorCount, "The fixture has no errors.");
        CheckEqual(0, result.Diagnostics.WarningCount, "The fixture has no warnings.");
        CheckEqual("AllTypes,Empty,Item,Monster,Physics", JoinTableNames(result.Tables), "Tables are listed in name order; ignored and merged sheets add no tables.");
        CheckEqual("EAccess,EKind,EMonsterId,EPlain,ESmall", JoinEnumNames(result.Enums), "Enums come from the Enums sheet and from Mapping columns.");

        GeneratedOutput output = result.OutputOrNull!;
        CheckEqual("AllTypes.bytes,Empty.bytes,Item.bytes,Monster.bytes,Physics.bytes,tables.manifest", string.Join(",", output.DataFiles.Keys),
            "One data file per table plus the manifest.");
        Check(output.CodeFiles.ContainsKey("ItemRow.g.cs") && output.CodeFiles.ContainsKey("ItemTable.g.cs")
            && output.CodeFiles.ContainsKey("Enums.g.cs") && output.CodeFiles.ContainsKey("FixtureTables.g.cs"), "Row, table, enum and bundle code is generated.");
    }

    private static void CheckFixtureContents()
    {
        TableBuildResult result = BuildFixture();
        TableData monster = FindTable(result, "Monster");
        CheckEqual("Id,Name,Slug,Drop,Level,Hp,Speed", JoinFields(monster.Schema), "A Mapping column is left out and extension columns are appended.");
        CheckEqual(3, monster.Rows.Count, "The ignored row and the blank row are skipped, and the child sheet's row is appended.");
        CheckEqual("int[]", monster.Schema.Columns[4].Type.CanonicalName, "Columns with the same field name merge into an array.");
        CheckEqual("1,2,3", string.Join(",", (int[])monster.Rows[0].Values[4]), "Merged columns keep their order.");
        CheckEqual("4,6", string.Join(",", (int[])monster.Rows[1].Values[4]), "A blank cell among merged columns is skipped.");
        CheckEqual(6, (int)monster.Rows[0].Values[5], "An extension column is joined by key value (01001 is 1001).");
        CheckEqual(0, (int)monster.Rows[1].Values[5], "A row the extension sheet does not list gets empty values.");
        CheckEqual(2001, (int)monster.Rows[2].Values[0], "A child sheet's columns are matched by field name, not by position.");
        CheckEqual("드래곤", (string)monster.Rows[2].Values[1], "A child sheet's reordered columns land in the right fields.");
        Check(!((RefValue)monster.Rows[1].Values[3]).HasValue, "A '~' reference points to nothing.");
        CheckEqual("potion", (string)((RefValue)monster.Rows[0].Values[3]).KeyOrNull!, "A reference keeps the target key.");

        EnumDefinition mapping = FindEnum(result, "EMonsterId");
        CheckEqual("Slime=1001,Bat=1002,Dragon=2001", JoinMembers(mapping), "A Mapping column builds an enum from the keys, including child rows and skipping ignored rows.");
        CheckEqual("None=0,Read=1,Write=2,All=3", JoinMembers(FindEnum(result, "EAccess")), "Enum value expressions are evaluated.");
        CheckEqual("None=0,Potion=1,Gold=10", JoinMembers(FindEnum(result, "EKind")), "A member without a value follows the previous one.");
        CheckEqual("Low=-1,High=0", JoinMembers(FindEnum(result, "ESmall")), "A negative value is followed by the next integer.");
        Check(FindEnum(result, "EAccess").IsFlags && !FindEnum(result, "EKind").IsFlags, "Flags<...> marks a flags enum.");

        TableData physics = FindTable(result, "Physics");
        CheckEqual("Gravity,MaxSpeed,IsEnabled", JoinFields(physics.Schema), "A Config sheet is read with rows and columns swapped.");
        CheckEqual(1, physics.Rows.Count, "A Config sheet with one value column makes one row.");
        CheckEqual(30, (int)physics.Rows[0].Values[1], "A Config sheet's values are read.");
        CheckEqual(0, FindTable(result, "Empty").Rows.Count, "A table may have no rows.");
        CheckEqual(22, FindTable(result, "AllTypes").Schema.Columns.Count, "A column whose header starts with ~ is ignored.");
    }

    private static void CheckGeneratedCodeIsCurrent()
    {
        TableBuildResult result = BuildFixture();
        if (!result.IsSuccess)
        {
            return;
        }

        string directory = GetFixtureCodeDirectory();
        foreach (KeyValuePair<string, string> file in result.OutputOrNull!.CodeFiles)
        {
            string path = Path.Combine(directory, file.Key);
            bool bCurrent = File.Exists(path) && IsSameCode(File.ReadAllText(path), file.Value);
            Check(bCurrent, $"The generated fixture file {file.Key} is current. Run with --update-fixture after changing a generator or the fixture.");
        }

        int generatedFileCount = Directory.Exists(directory) ? Directory.GetFiles(directory, "*.g.cs").Length : 0;
        CheckEqual(result.OutputOrNull.CodeFiles.Count, generatedFileCount, "The fixture folder holds no stale generated files.");
    }

    private static void CheckBuildIsDeterministic()
    {
        GeneratedOutput first = BuildFixture().OutputOrNull!;
        GeneratedOutput second = BuildFixture().OutputOrNull!;
        bool bSame = first.CodeFiles.Count == second.CodeFiles.Count && first.DataFiles.Count == second.DataFiles.Count;
        foreach (KeyValuePair<string, string> file in first.CodeFiles)
        {
            bSame &= second.CodeFiles.TryGetValue(file.Key, out string? other) && other == file.Value;
        }

        foreach (KeyValuePair<string, byte[]> file in first.DataFiles)
        {
            bSame &= second.DataFiles.TryGetValue(file.Key, out byte[]? other) && other.AsSpan().SequenceEqual(file.Value);
        }

        Check(bSame, "The same input produces byte-identical output.");
    }

    private static bool IsSameCode(string left, string right)
    {
        return RemoveCommentLines(left) == RemoveCommentLines(right);
    }

    private static string RemoveCommentLines(string code)
    {
        string[] lines = code.Replace("\r", "").Split('\n');
        List<string> kept = new List<string>();
        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].Trim();
            if (trimmed.Length > 0 && !trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                kept.Add(lines[index].TrimEnd());
            }
        }

        return string.Join("\n", kept);
    }

    private static TableData FindTable(TableBuildResult result, string name)
    {
        for (int index = 0; index < result.Tables.Count; index++)
        {
            if (result.Tables[index].Schema.Name == name)
            {
                return result.Tables[index];
            }
        }

        throw new InvalidOperationException($"The build has no table '{name}'.");
    }

    private static EnumDefinition FindEnum(TableBuildResult result, string name)
    {
        for (int index = 0; index < result.Enums.Count; index++)
        {
            if (result.Enums[index].Name == name)
            {
                return result.Enums[index];
            }
        }

        throw new InvalidOperationException($"The build has no enum '{name}'.");
    }

    private static string JoinTableNames(IReadOnlyList<TableData> tables)
    {
        List<string> names = new List<string>();
        for (int index = 0; index < tables.Count; index++)
        {
            names.Add(tables[index].Schema.Name);
        }

        return string.Join(",", names);
    }

    private static string JoinEnumNames(IReadOnlyList<EnumDefinition> enums)
    {
        List<string> names = new List<string>();
        for (int index = 0; index < enums.Count; index++)
        {
            names.Add(enums[index].Name);
        }

        return string.Join(",", names);
    }

    private static string JoinFields(TableSchema schema)
    {
        List<string> fields = new List<string>();
        for (int index = 0; index < schema.Columns.Count; index++)
        {
            fields.Add(schema.Columns[index].Field);
        }

        return string.Join(",", fields);
    }

    private static string JoinMembers(EnumDefinition definition)
    {
        List<string> members = new List<string>();
        for (int index = 0; index < definition.Members.Count; index++)
        {
            members.Add(definition.Members[index].Name + "=" + definition.Members[index].Value);
        }

        return string.Join(",", members);
    }
}
