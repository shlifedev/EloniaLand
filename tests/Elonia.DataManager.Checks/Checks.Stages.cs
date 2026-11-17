using System;
using Elonia.DataManager;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckCellText()
    {
        CheckEqual("", CellText.FromValue(null), "An empty cell is an empty text.");
        CheckEqual("5", CellText.FromValue(5.0), "Excel stores 5 as 5.0; it reads as an integer.");
        CheckEqual("-12", CellText.FromValue(-12.0), "A negative integer reads as an integer.");
        CheckEqual("0.1", CellText.FromValue(0.1), "A fraction keeps its shortest exact form.");
        CheckEqual("1.5", CellText.FromValue(1.5), "A fraction uses a period regardless of the computer's language.");
        CheckEqual("true", CellText.FromValue(true), "A boolean reads as true.");
        CheckEqual("2026-01-31", CellText.FromValue(new DateTime(2026, 1, 31)), "A date without a time reads as a date.");
        CheckEqual("2026-01-31 13:30:00", CellText.FromValue(new DateTime(2026, 1, 31, 13, 30, 0)), "A date with a time keeps the time.");
        CheckEqual(" kept ", CellText.FromValue(" kept "), "Text is kept as written.");

        CheckEqual("A", CellOrigin.GetColumnLetters(1), "Column 1 is A.");
        CheckEqual("Z", CellOrigin.GetColumnLetters(26), "Column 26 is Z.");
        CheckEqual("AA", CellOrigin.GetColumnLetters(27), "Column 27 is AA.");
        CheckEqual("Book.xlsx / Item!C7", new CellOrigin("Book.xlsx", "Item", 7, 3).ToString(), "An origin reads like an Excel address.");

        Sheet sheet = Fixture.CreateSheet("S", new string?[] { "a", "b", "c" }, new string?[] { "d" });
        CheckEqual("3,2", $"{sheet.ColumnCount},{sheet.RowCount}", "A sheet is as wide as its longest row.");
        CheckEqual("", sheet.GetCell(1, 2).Text, "A short row is padded with empty cells.");
        CheckEqual("C2", sheet.GetCell(1, 2).Origin.Address, "A padded cell still knows where it is.");
        Sheet transposed = sheet.Transpose();
        CheckEqual("2,3", $"{transposed.ColumnCount},{transposed.RowCount}", "Transposing swaps rows and columns.");
        CheckEqual("b", transposed.GetCell(1, 0).Text, "A transposed cell moves to the swapped position.");
        CheckEqual("B1", transposed.GetCell(1, 0).Origin.Address, "A transposed cell keeps its original address.");
        CheckEqual("D9", transposed.GetCell(3, 8).Origin.Address, "A cell outside a transposed sheet maps back to the original address.");
    }

    private static void CheckSheetRoles()
    {
        CheckEqual(ESheetKind.Table, SheetRole.FromName("Item").Kind, "A plain name is a table.");
        CheckEqual("Item", SheetRole.FromName("Item").TableName, "A table sheet names its table.");
        CheckEqual(ESheetKind.Config, SheetRole.FromName("Physics.Config").Kind, "X.Config is a config sheet.");
        CheckEqual("Physics", SheetRole.FromName("Physics.Config").TableName, "A config sheet names its table.");
        CheckEqual(ESheetKind.ChildRows, SheetRole.FromName("Monster.Boss").Kind, "X.Name is a child rows sheet.");
        CheckEqual(ESheetKind.ColumnExtension, SheetRole.FromName("Monster.Col.Stats").Kind, "X.Col.Name is a column extension sheet.");
        CheckEqual("Monster", SheetRole.FromName("Monster.Col.Stats").TableName, "A column extension names its table.");
        CheckEqual(ESheetKind.Enums, SheetRole.FromName("Enums").Kind, "Enums is an enum sheet.");
        CheckEqual(ESheetKind.Enums, SheetRole.FromName("Battle.Enums").Kind, "X.Enums is an enum sheet.");
        CheckEqual(ESheetKind.Table, SheetRole.FromName("enums").Kind, "The Enums name is case sensitive.");
        CheckEqual(ESheetKind.Ignored, SheetRole.FromName("#Notes").Kind, "A # sheet is ignored.");
        CheckEqual(ESheetKind.Ignored, SheetRole.FromName("~Draft").Kind, "A ~ sheet is ignored.");
        CheckEqual(ESheetKind.Invalid, SheetRole.FromName("A.B.C").Kind, "Three parts without Col are not understood.");
        CheckEqual(ESheetKind.Invalid, SheetRole.FromName("A.Col.B.C").Kind, "Four parts are not understood.");
    }

    private static void CheckTypeExpressions()
    {
        CheckEqual("Value int key=False None", Describe("int"), "A plain type.");
        CheckEqual("Value int key=True None", Describe(" Key< int > "), "Key<...> marks a key; spaces are ignored.");
        CheckEqual("Value float key=False Array", Describe("float[]"), "T[] is an array.");
        CheckEqual("Value string key=False List", Describe("List<string>"), "List<T> is a list.");
        CheckEqual("Enum EKind key=False None", Describe("Enum<EKind>"), "Enum<Name> is an enum.");
        CheckEqual("Enum EKind key=False Array", Describe("Enum<EKind[]>"), "Enum<Name[]> is an enum array.");
        CheckEqual("Enum EKind key=True None", Describe("Key<Enum<EKind>>"), "An enum can be a key.");
        CheckEqual("Reference Item key=False None", Describe("Ref<Item>"), "Ref<Table> is a reference.");
        CheckEqual("Mapping EId key=False None", Describe("Mapping<EId>"), "Mapping<Name> is a mapping.");
        CheckEqual("error", Describe(""), "An empty type is an error.");
        CheckEqual("error", Describe("List<List<int>>"), "Nested lists are an error.");
        CheckEqual("error", Describe("Key<int[]>"), "An array cannot be a key.");
        CheckEqual("error", Describe("Key<Ref<Item>>"), "A reference cannot be a key.");
        CheckEqual("error", Describe("int[][]"), "Nested arrays are an error.");
        CheckEqual("error", Describe("Enum<>"), "An enum needs a name.");
    }

    private static string Describe(string text)
    {
        if (!TypeExpression.TryParse(text, out TypeExpression? expression, out string _))
        {
            return "error";
        }

        return $"{expression!.Kind} {expression.Name} key={expression.IsKey} {expression.Collection}";
    }

    private static void CheckScalarTypes()
    {
        TypeRegistry registry = TypeRegistry.CreateDefault();
        CheckEqual("5", Parse(registry, "int", " 5 "), "An integer ignores surrounding spaces.");
        CheckEqual("-2147483648", Parse(registry, "int", "-2147483648"), "The smallest int is read.");
        CheckEqual("error", Parse(registry, "int", "2147483648"), "An int out of range is an error.");
        CheckEqual("error", Parse(registry, "int", "3.5"), "A fraction is not an int.");
        CheckEqual("error", Parse(registry, "int", "1,000"), "A thousands separator is not accepted.");
        CheckEqual("error", Parse(registry, "uint", "-1"), "A negative number is not a uint.");
        CheckEqual("255", Parse(registry, "byte", "255"), "The largest byte is read.");
        CheckEqual("error", Parse(registry, "byte", "256"), "A byte out of range is an error.");
        CheckEqual("0", Parse(registry, "int", ""), "An empty int cell is 0.");
        CheckEqual("1.5", Parse(registry, "float", "1.5"), "A float is read.");
        CheckEqual("1000", Parse(registry, "double", "1e3"), "A double accepts an exponent.");
        CheckEqual("error", Parse(registry, "float", "NaN"), "NaN is not accepted.");
        CheckEqual("error", Parse(registry, "float", "1,5"), "A comma is not a decimal point.");
        CheckEqual("True", Parse(registry, "bool", "TRUE"), "A bool ignores case.");
        CheckEqual("True", Parse(registry, "bool", "1"), "1 is true.");
        CheckEqual("False", Parse(registry, "bool", "0"), "0 is false.");
        CheckEqual("False", Parse(registry, "bool", ""), "An empty bool cell is false.");
        CheckEqual("error", Parse(registry, "bool", "yes"), "Other words are not bools.");
        CheckEqual(" a ", Parse(registry, "string", " a "), "A string keeps its spaces.");
        CheckEqual("error", Parse(registry, "DateTime", "someday"), "A word is not a date.");
        CheckEqual("error", Parse(registry, "Guid", "1234"), "A short number is not a GUID.");

        registry.FindOrNull("DateTime")!.TryParse("2026-01-31 13:30:00", out object when, out string _);
        CheckEqual(DateTimeKind.Utc, ((DateTime)when).Kind, "A date without a zone is read as UTC, whatever the computer's time zone.");
        CheckEqual(13, ((DateTime)when).Hour, "The hour is kept as written.");

        ColumnType array = registry.FindOrNull("int")!.CreateArrayType();
        CheckEqual("int[]", array.CanonicalName, "An array type is named after its element.");
        Check(array.TryParse("1| 2 |3", out object numbers, out string _) && string.Join(",", (int[])numbers) == "1,2,3", "Array elements are split by | and trimmed.");
        Check(array.TryParse("", out object none, out string _) && ((int[])none).Length == 0, "An empty array cell is an empty array.");
        Check(!array.TryParse("1||3", out object _, out string emptyReason) && emptyReason.Contains("2번째"), "An empty element is an error that names its position.");
        Check(!array.TryParse("1|x", out object _, out string badReason) && badReason.Contains("\"x\""), "A bad element is an error that quotes it.");
        CheckEqual("List<int>", registry.FindOrNull("int")!.CreateListType().CSharpTypeName, "A list type is named after its element.");

        Check(registry.FindOrNull("Int") == null, "Type names are case sensitive.");
        CheckEqual("int", registry.FindSimilarNameOrNull("Int"), "A name that differs only by case is suggested.");
        CheckThrows<ArgumentException>(() => registry.Add(registry.FindOrNull("int")!), "Registering a name twice is rejected.");
    }

    private static string Parse(TypeRegistry registry, string typeName, string text)
    {
        if (!registry.FindOrNull(typeName)!.TryParse(text, out object value, out string _))
        {
            return "error";
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;
    }

    private static void CheckEnumExpressions()
    {
        CheckEqual("3", Evaluate("3"), "A number.");
        CheckEqual("16", Evaluate("0x10"), "A hex number.");
        CheckEqual("5", Evaluate("0b101"), "A binary number.");
        CheckEqual("1000", Evaluate("1_000"), "Digit separators are allowed.");
        CheckEqual("4", Evaluate("1 << 2"), "A shift.");
        CheckEqual("3", Evaluate("Read | Write"), "Earlier members can be combined.");
        CheckEqual("7", Evaluate("1 + 2 * 3"), "Multiplication binds tighter than addition.");
        CheckEqual("9", Evaluate("(1 + 2) * 3"), "Parentheses group.");
        CheckEqual("-2", Evaluate("~Read"), "Bitwise not.");
        CheckEqual("5", Evaluate("1 | 1 << 2"), "A shift binds tighter than or.");
        CheckEqual("-1", Evaluate("-1"), "A negative number.");
        CheckEqual("error", Evaluate("1 <<"), "An unfinished expression is an error.");
        CheckEqual("error", Evaluate("Unknown"), "An unknown member is an error.");
        CheckEqual("error", Evaluate("(1"), "A missing parenthesis is an error.");
        CheckEqual("error", Evaluate("1 2"), "Leftover text is an error.");
        CheckEqual("error", Evaluate("1 << 64"), "A shift past 63 is an error.");
    }

    private static string Evaluate(string text)
    {
        EnumExpression.TryResolveMember resolve = (string name, out long value) =>
        {
            value = name == "Read" ? 1 : 2;
            return name == "Read" || name == "Write";
        };
        return EnumExpression.TryEvaluate(text, resolve, out long result, out string _) ? result.ToString() : "error";
    }
}
