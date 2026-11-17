using System.Collections.Generic;
using Elonia.DataManager;

namespace Elonia.Checks;

internal static class Fixture
{
    public const string WORKBOOK_NAME = "Fixture.xlsx";
    public const string GUID_TEXT = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

    public static TableBuildOptions CreateOptions()
    {
        TableBuildOptions options = new TableBuildOptions();
        options.Namespace = "Elonia.Checks.Generated";
        options.TablesClassName = "FixtureTables";
        return options;
    }

    public static Workbook CreateWorkbook()
    {
        List<Sheet> sheets = new List<Sheet>
        {
            CreateSheet("Enums",
                new string?[] { "byte", null, "Flags<int>", null, "#메모", null, "short", null },
                new string?[] { "EKind", null, "EAccess", null, "무시", "EPlain", "ESmall", null },
                new string?[] { "아이템 분류", null, "접근 권한 <여러 개>", null, null, "값 열이 없는 enum", null, null },
                new string?[] { "None", null, "None", "0", "아무 글", "A", "Low", "-1" },
                new string?[] { "Potion", null, "Read", "1 << 0", null, "B", "High", null },
                new string?[] { "Gold", "10", "Write", "1 << 1", null, "C", null, null },
                new string?[] { null, null, "All", "Read | Write", null, null, null, null }),
            CreateSheet("Item",
                new string?[] { "Key<string>", "string", "int", "Enum<EKind>" },
                new string?[] { "Id", "Name", "Price", "Kind" },
                new string?[] { "아이템 ID", "이름", "가격", null },
                new string?[] { "potion", "Potion", "5", "Potion" },
                new string?[] { "gold", "Gold", null, "Gold" },
                new string?[] { "branch", "Branch", "2", null }),
            CreateSheet("Monster",
                new string?[] { "Key<int>", "Mapping<EMonsterId>", "string", "Key<string>", "Ref<Item>", "int", "int", "int" },
                new string?[] { "Id", "Code", "Name", "Slug", "Drop", "Level", "Level", "Level" },
                new string?[] { "번호", "코드에서 쓸 이름", "이름", "주소용 이름", "떨어뜨리는 아이템", "레벨별 값", null, null },
                new string?[] { "1001", "Slime", "슬라임", "slime", "potion", "1", "2", "3" },
                new string?[] { "1002", "Bat", "박쥐", "bat", "~", "4", null, "6" },
                new string?[] { "~1003", "Ghost", "아직 안 쓰는 행", "ghost", "~", null, null, null },
                new string?[] { null, null, null, null, null, null, null, null }),
            CreateSheet("Monster.Boss",
                new string?[] { "string", "Key<int>", "Mapping<EMonsterId>", "Ref<Item>", "Key<string>", "int", "int", "int" },
                new string?[] { "Name", "Id", "Code", "Drop", "Slug", "Level", "Level", "Level" },
                new string?[] { null, null, null, null, null, null, null, null },
                new string?[] { "드래곤", "2001", "Dragon", "gold", "dragon", "9", "9", "9" }),
            CreateSheet("Monster.Col.Stats",
                new string?[] { "Key<int>", "int", "float" },
                new string?[] { "Id", "Hp", "Speed" },
                new string?[] { null, "체력", "속도" },
                new string?[] { "01001", "6", "0.5" },
                new string?[] { "2001", "99", "1.25" }),
            CreateSheet("Physics.Config",
                new string?[] { "float", "Gravity", "중력", "9.8" },
                new string?[] { "int", "MaxSpeed", "최고 속도", "30" },
                new string?[] { "bool", "IsEnabled", null, "true" }),
            CreateSheet("AllTypes",
                new string?[]
                {
                    "Key<int>", "uint", "long", "ulong", "short", "ushort", "byte", "float", "double", "decimal", "bool", "string", "DateTime", "Guid",
                    "int[]", "List<string>", "float[]", "List<int>", "Enum<EKind[]>", "Enum<EAccess>", "Enum<ESmall>", "Key<Enum<EPlain>>", "~임시"
                },
                new string?[]
                {
                    "Id", "U", "L", "UL", "S", "US", "B", "F", "D", "M", "Flag", "Text", "When", "G",
                    "Numbers", "Tags", "Rates", "Counts", "Kinds", "Access", "Small", "Plain", "Draft"
                },
                new string?[] { "모든 타입" },
                new string?[]
                {
                    "1", "4000000000", "-9000000000000000000", "18000000000000000000", "-32768", "65535", "255", "1.5", "0.1", "79228162514264337593543950335", "TRUE", " 앞뒤 공백 ", "2026-01-31 13:30:00", GUID_TEXT,
                    "1|2|3", "a| b |c", "0.5|1.5", "7", "Potion|Gold", "Read, Write", "Low", "A", "읽지 않는 열"
                },
                new string?[] { "2", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, "B", null }),
            CreateSheet("Empty",
                new string?[] { "int", "string" },
                new string?[] { "Value", "Note" }),
            CreateSheet("#Notes", new string?[] { "이 시트는 읽지 않는다" }),
            CreateSheet("~Draft", new string?[] { "이 시트도 읽지 않는다" }),
        };
        return new Workbook(WORKBOOK_NAME, sheets);
    }

    public static Sheet CreateSheet(string name, params string?[][] rows)
    {
        return new Sheet(WORKBOOK_NAME, name, rows);
    }
}
