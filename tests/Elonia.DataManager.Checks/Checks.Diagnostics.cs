using System;
using System.Collections.Generic;
using Elonia.DataManager;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckDiagnostics()
    {
        ExpectError("Fixture.xlsx / T!A1", "모르는 타입", "'int'",
            Table("T", "Int", "Value", null, "1"));
        ExpectError("Fixture.xlsx / T!A1", "모르는 타입", "쓸 수 있는 타입",
            Table("T", "integer", "Value", null, "1"));
        ExpectError("Fixture.xlsx / T!A1", "이해하지 못했습니다", null,
            Table("T", "List<List<int>>", "Value", null, "1"));
        ExpectError("Fixture.xlsx / T!A1", "타입이 비어 있습니다", null,
            Table("T", null, "Value", null, "1"));
        ExpectError("Fixture.xlsx / T!A2", "필드명이 비어 있습니다", null,
            Table("T", "int", null, null, "1"));
        ExpectError("Fixture.xlsx / T!A2", "필드명으로 쓸 수 없습니다", null,
            Table("T", "int", "1abc", null, "1"));
        ExpectError("Fixture.xlsx / T!A2", "필드명으로 쓸 수 없습니다", null,
            Table("T", "int", "class", null, "1"));
        ExpectError("Fixture.xlsx / T!A1", "Key로 쓸 수 없는 타입", null,
            Table("T", "Key<float>", "Id", null, "1"));
        ExpectError("Fixture.xlsx / T!B2", "이미 있습니다", null,
            Fixture.CreateSheet("T", new string?[] { "int", "string" }, new string?[] { "Value", "Value" }, new string?[] { null }, new string?[] { "1", "a" }));
        ExpectError("Fixture.xlsx / T!B2", "다른 이름과 겹칩니다", null,
            Fixture.CreateSheet("T", new string?[] { "int", "int" }, new string?[] { "Value", "value" }, new string?[] { null }, new string?[] { "1", "2" }));
        ExpectError("Fixture.xlsx / T", "읽을 열이 없습니다", null,
            Fixture.CreateSheet("T", new string?[] { "#int" }, new string?[] { "Value" }));

        ExpectError("Fixture.xlsx / T!A4", "정수가 아닙니다", "예: 10",
            Table("T", "int", "Value", null, "abc"));
        ExpectError("Fixture.xlsx / T!A5", "정수가 아닙니다", null,
            Table("T", "int", "Value", null, "1", "3.5"));
        ExpectError("Fixture.xlsx / T!A4", "int 범위", null,
            Table("T", "int", "Value", null, "99999999999"));
        ExpectError("Fixture.xlsx / T!A4", "참·거짓", null,
            Table("T", "bool", "Value", null, "yes"));
        ExpectError("Fixture.xlsx / T!A4", "2번째 요소가 비어", null,
            Table("T", "int[]", "Value", null, "1||3"));
        ExpectError("Fixture.xlsx / T!A4", "2번째 요소 \"x\"", null,
            Table("T", "int[]", "Value", null, "1|x"));

        ExpectError("Fixture.xlsx / T!A5", "A4 칸과 같습니다", null,
            Table("T", "Key<int>", "Id", null, "1", "01"));
        ExpectError("Fixture.xlsx / T!A6", "A4 칸과 같습니다", null,
            Table("T", "Key<string>", "Id", null, "a", "b", "a"));

        ExpectError("Fixture.xlsx / T!A1", "enum이 없습니다", "Enums 시트",
            Table("T", "Enum<ENope>", "Value", null, "A"));
        ExpectError("Fixture.xlsx / T!A4", "쓸 수 있는 값: None, Fire", null,
            Enums("Enums", null, "EKind", "None", "Fire"), Table("T", "Enum<EKind>", "Value", null, "Water"));
        ExpectError("Fixture.xlsx / T!A5", "0인 멤버가 없어서", null,
            Fixture.CreateSheet("Enums", new string?[] { null, null }, new string?[] { "EKind", null }, new string?[] { null }, new string?[] { "Fire", "1" }),
            Fixture.CreateSheet("T", new string?[] { "Enum<EKind>", "int" }, new string?[] { "Value", "N" }, new string?[] { null }, new string?[] { "Fire", "1" }, new string?[] { null, "2" }));
        ExpectError("Fixture.xlsx / Enums!B4", "값을 계산하지 못했습니다", null,
            Fixture.CreateSheet("Enums", new string?[] { null, null }, new string?[] { "EKind", null }, new string?[] { null }, new string?[] { "A", "1 <<" }));
        ExpectError("Fixture.xlsx / Enums!B4", "byte에 들어가지 않습니다", null,
            Fixture.CreateSheet("Enums", new string?[] { "byte", null }, new string?[] { "EKind", null }, new string?[] { null }, new string?[] { "A", "300" }));
        ExpectError("Fixture.xlsx / Enums!A5", "같은 이름의 멤버", null,
            Enums("Enums", null, "EKind", "A", "A"));
        ExpectError("Fixture.xlsx / Enums!A1", "정수 타입을 이해하지 못했습니다", null,
            Enums("Enums", "float", "EKind", "A"));
        ExpectError("Fixture.xlsx / Enums!A2", "멤버가 하나도 없습니다", null,
            Enums("Enums", null, "EKind"));
        ExpectError("Fixture.xlsx / More.Enums!A2", "이미 'Fixture.xlsx / Enums!A2'에 있습니다", null,
            Enums("Enums", null, "EKind", "A"), Enums("More.Enums", null, "EKind", "B"), Table("T", "int", "Value", null, "1"));

        Sheet item = Table("Item", "Key<string>", "Id", null, "potion");
        ExpectError("Fixture.xlsx / T!A4", "표 'Item'에 이 Key를 가진 행이 없습니다", "~",
            item, Table("T", "Ref<Item>", "Drop", null, "nope"));
        ExpectError("Fixture.xlsx / T!A5", "아무것도 가리키지 않으려면 '~'", null,
            item, Fixture.CreateSheet("T", new string?[] { "Ref<Item>", "int" }, new string?[] { "Drop", "N" }, new string?[] { null }, new string?[] { "potion", "1" }, new string?[] { null, "2" }));
        ExpectError("Fixture.xlsx / T!A1", "표 'Ghost'이(가) 없거나", null,
            Table("T", "Ref<Ghost>", "Drop", null, "~"));
        ExpectError("Fixture.xlsx / T!A1", "Key<...> 열이 없습니다", null,
            Table("NoKey", "int", "Value", null, "1"), Table("T", "Ref<NoKey>", "Drop", null, "~"));

        ExpectError("Fixture.xlsx / A.B.C", "시트 이름을 이해하지 못했습니다", null,
            Table("A.B.C", "int", "Value", null, "1"));
        ExpectError("Fixture.xlsx / 1Bad", "표 이름으로 쓸 수 없습니다", null,
            Table("1Bad", "int", "Value", null, "1"));
        ExpectError("Fixture.xlsx / T.Config", "이미 'Fixture.xlsx / T'에 있습니다", null,
            Table("T", "int", "Value", null, "1"), Table("T.Config", "int", "Value", null, "1"));
        ExpectError("Fixture.xlsx / Ghost.More", "표 'Ghost'이(가) 없습니다", null,
            Table("T", "int", "Value", null, "1"), Table("Ghost.More", "int", "Value", null, "1"));
        ExpectError("Fixture.xlsx / T.More", "자식 시트에 'int Value' 열이 없습니다", "'Fixture.xlsx / T' 시트와 똑같이",
            Table("T", "int", "Value", null, "1"), Table("T.More", "string", "Value", null, "a"));
        ExpectError("Fixture.xlsx / T.Col.X!A4", "표 'T'에 이 Key를 가진 행이 없습니다", null,
            Table("T", "Key<int>", "Id", null, "1"),
            Fixture.CreateSheet("T.Col.X", new string?[] { "Key<int>", "int" }, new string?[] { "Id", "Hp" }, new string?[] { null }, new string?[] { "9", "5" }));
        ExpectError("Fixture.xlsx / T.Col.X!A5", "같은 Key가 두 번", null,
            Table("T", "Key<int>", "Id", null, "1"),
            Fixture.CreateSheet("T.Col.X", new string?[] { "Key<int>", "int" }, new string?[] { "Id", "Hp" }, new string?[] { null }, new string?[] { "1", "5" }, new string?[] { "01", "6" }));
        ExpectError("Fixture.xlsx / T.Col.X!A2", "Key 열이 표 'T'의 Key 열", null,
            Table("T", "Key<int>", "Id", null, "1"),
            Fixture.CreateSheet("T.Col.X", new string?[] { "Key<string>", "int" }, new string?[] { "Id", "Hp" }, new string?[] { null }, new string?[] { "1", "5" }));

        ExpectError("Fixture.xlsx / T!A1", "정수 Key가 있는 표에서만", null,
            Fixture.CreateSheet("T", new string?[] { "Mapping<EId>", "Key<string>" }, new string?[] { "Code", "Id" }, new string?[] { null }, new string?[] { "A", "a" }));
        ExpectError("Fixture.xlsx / T!B5", "같은 이름의 멤버", null,
            Fixture.CreateSheet("T", new string?[] { "Key<int>", "Mapping<EId>" }, new string?[] { "Id", "Code" }, new string?[] { null }, new string?[] { "1", "A" }, new string?[] { "2", "A" }));
        ExpectError("Fixture.xlsx / Item", "생성될 이름 'ItemRow'", null,
            Enums("Enums", null, "ItemRow", "A"), Table("Item", "int", "Value", null, "1"));

        ExpectError("입력", "표가 하나도 없습니다", null, Fixture.CreateSheet("#Only", new string?[] { "메모" }));
    }

    private static void CheckEveryErrorIsReported()
    {
        TableBuildResult result = BuildSheets(
            Fixture.CreateSheet("A", new string?[] { "Key<int>", "int", "bool" }, new string?[] { "Id", "N", "Flag" }, new string?[] { null },
                new string?[] { "1", "x", "true" }, new string?[] { "1", "2", "maybe" }),
            Table("B", "Strange", "Value", null, "1"));
        CheckEqual(4, result.Diagnostics.ErrorCount, "Every problem is reported in one run: a bad int, a bad bool, a duplicate key and an unknown type.");
        Check(!result.IsSuccess, "A build with errors produces no output.");

        TableBuildResult warned = BuildSheets(
            Fixture.CreateSheet("A", new string?[] { "Key<int>", "string", null }, new string?[] { "Id", "Name", null }, new string?[] { null },
                new string?[] { "1", "a", "떠도는 메모" }, new string?[] { null, "b", null }));
        CheckEqual(0, warned.Diagnostics.ErrorCount, "Warnings are not errors.");
        CheckEqual(2, warned.Diagnostics.WarningCount, "A value under no header and a row without a key are both warned about.");
        Check(warned.IsSuccess, "A build with only warnings still produces output.");
        CheckEqual(1, warned.Tables[0].Rows.Count, "A row whose key is blank is skipped.");
        CheckEqual("Fixture.xlsx / A!C4", warned.Diagnostics.Items[0].Location, "The stray value is located by its cell.");
        CheckEqual("Fixture.xlsx / A!A5", warned.Diagnostics.Items[1].Location, "The skipped row is located by its key cell.");

        string text = DiagnosticFormatter.Format(result.Diagnostics.Items);
        Check(text.Contains("[오류] Fixture.xlsx / A!B4") && text.Contains("값: \"x\"") && text.Contains("이유: ") && text.Contains("고치는 방법: "),
            "A formatted error shows the location, the value, the reason and the fix.");
    }

    private static Sheet Table(string name, params string?[] cells)
    {
        string?[][] rows = new string?[cells.Length][];
        for (int index = 0; index < cells.Length; index++)
        {
            rows[index] = new string?[] { cells[index] };
        }

        return Fixture.CreateSheet(name, rows);
    }

    private static Sheet Enums(string sheetName, string? underlying, string enumName, params string[] members)
    {
        List<string?[]> rows = new List<string?[]> { new string?[] { underlying }, new string?[] { enumName }, new string?[] { null } };
        for (int index = 0; index < members.Length; index++)
        {
            rows.Add(new string?[] { members[index] });
        }

        return Fixture.CreateSheet(sheetName, rows.ToArray());
    }

    private static TableBuildResult BuildSheets(params Sheet[] sheets)
    {
        return TableBuild.Run(new MemoryWorkbookSource(new Workbook(Fixture.WORKBOOK_NAME, sheets)), Fixture.CreateOptions());
    }

    private static void ExpectError(string location, string reasonPart, string? fixPartOrNull, params Sheet[] sheets)
    {
        TableBuildResult result = BuildSheets(sheets);
        bool bFound = false;
        for (int index = 0; index < result.Diagnostics.Items.Count; index++)
        {
            Diagnostic diagnostic = result.Diagnostics.Items[index];
            bool bFixMatches = fixPartOrNull == null || (diagnostic.FixOrNull != null && diagnostic.FixOrNull.Contains(fixPartOrNull));
            bFound |= diagnostic.Severity == ESeverity.Error && diagnostic.Location == location && diagnostic.Reason.Contains(reasonPart) && bFixMatches;
        }

        Check(bFound && !result.IsSuccess, $"An error at '{location}' mentions '{reasonPart}'." + (bFound ? "" : Environment.NewLine + DiagnosticFormatter.Format(result.Diagnostics.Items)));
    }
}
