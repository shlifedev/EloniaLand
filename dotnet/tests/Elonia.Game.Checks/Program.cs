using System;
using System.IO;
using Cysharp.Text;
using Elonia.Data;
using Elonia.Game;
using Elonia.Simulation;

namespace Elonia.Checks;

internal static partial class Program
{
    private static int _checkCount;

    public static void Main()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Tables");
        GameTables tables = GameTables.Load(directory);
        GameContent original = new GameContent(tables);
        GameSession town = TownMap.Create(original, new Random(1));
        Check(town.Map.Actors[0] == town.Player && town.Player.Def == original.Tables.Actor.Get("hero"), "마을에서 플레이어가 먼저 생성된다.");
        CheckThingsAfterActors(town);
        CheckHarvestAndDoor(original, tables);
        CheckHouseLegend(original, tables);
        CheckUnknownSymbol(original, tables);

        int healing = tables.Item.Get("potion").Amount == 8 ? 9 : 8;
        GameContent edited = WorkbookFixture.CreateContent(tables, healing);
        CheckSwappedItems(original, edited, town);
        CheckPotionAction(edited, tables, healing);
        CheckPlaySession(original, tables);
        MeasureRuleActionAllocations(original, tables);
        CheckCombat(original);
        CheckBehaviors(original);
        CheckItems(original, edited);
        CheckHarvest(original);
        CheckStepTriggers(original);
        CheckRuleInteractions(original);
        CheckRuleValidation(original);
        Console.WriteLine($"게임 검사 {_checkCount}개 통과");
    }

    private static void CheckThingsAfterActors(GameSession town)
    {
        int lastActorId = int.MinValue;
        foreach (Actor actor in town.Map.Actors)
        {
            lastActorId = Math.Max(lastActorId, actor.ID);
        }

        foreach (Thing thing in town.Map.Things)
        {
            Check(thing.ID > lastActorId, $"물건 {thing.ID}는 모든 액터 뒤에 ID를 받는다.");
        }
    }

    private static void CheckHarvestAndDoor(GameContent content, GameTables tables)
    {
        string row = ZString.Concat(tables.TownLegend.Get("hero").Symbol, tables.TownLegend.Get("door").Symbol,
            tables.TownLegend.Get("tree").Symbol);
        GameSession session = TownMap.Create(content, new Random(1), new string[] { row });
        Check(session.Map.GetFloor(1, 0) == content.Tables.Floor.Get("wood_floor") && session.Map.GetPropOrNull(1, 0) == content.Tables.Prop.Get("door"),
            "나무 바닥 위에 문이 놓인다.");
        Check(session.Runner.SubmitPlayerAction(MoveAction.RIGHT).IsExecuted, "문을 통과할 수 있다.");

        FloorRow dirt = content.Tables.Floor.Get("dirt");
        session.Map.SetFloor(2, 0, dirt);
        Check(session.Runner.SubmitPlayerAction(new HarvestAction(2, 0)).IsExecuted, "문 옆의 나무를 캔다.");
        Check(session.Map.GetFloor(2, 0) == dirt && session.Map.GetPropOrNull(2, 0) == null, "나무가 사라지고 흙바닥이 남는다.");
        Thing? droppedOrNull = session.Map.GetItemAtOrNull(2, 0);
        PropRow tree = tables.Prop.Get("tree");
        Check(droppedOrNull != null && droppedOrNull.Def == content.Tables.Thing.Get(tree.DropThing.Key) && droppedOrNull.Count == tree.DropAmount,
            "수확물의 종류와 수량이 Prop 표와 일치한다.");
    }

    private static void CheckHouseLegend(GameContent content, GameTables tables)
    {
        string wood = new string(tables.TownLegend.Get("wood_house").Symbol[0], 3);
        string stone = new string(tables.TownLegend.Get("stone_house").Symbol[0], 4);
        string middle = ZString.Concat(wood, tables.TownLegend.Get("grass").Symbol, stone);
        string top = ZString.Concat(wood, tables.TownLegend.Get("hero").Symbol, stone);
        GameSession session = TownMap.Create(content, new Random(1), new string[] { top, middle, middle });
        int houseCellCount = 0;
        for (int y = 0; y < session.Map.Height; y++)
        {
            for (int x = 0; x < session.Map.Width; x++)
            {
                if (session.GetLegend(x, y).HouseStyle != EHouseStyle.None)
                {
                    houseCellCount++;
                }
            }
        }

        Check(houseCellCount == 21, "나무집과 돌집의 모든 칸에 집 외형이 남는다.");
        Check(session.GetLegend(0, 0).HouseStyle == EHouseStyle.Wood, "나무집 칸의 범례가 남는다.");
        Check(session.GetLegend(7, 2).HouseStyle == EHouseStyle.Stone, "돌집 칸의 범례가 남는다.");
        Check(session.GetLegend(3, 0) == tables.TownLegend.Get("hero"), "플레이어 시작 칸의 범례가 남는다.");
    }

    private static void CheckUnknownSymbol(GameContent content, GameTables tables)
    {
        char unknown = FindUnknownSymbol(tables.TownLegend);
        string grass = tables.TownLegend.Get("grass").Symbol;
        string top = ZString.Concat(tables.TownLegend.Get("hero").Symbol, grass, grass);
        string bottom = ZString.Concat(grass, grass, unknown);
        try
        {
            TownMap.Create(content, new Random(1), new string[] { top, bottom });
        }
        catch (InvalidDataException exception) when (exception.Message.Contains($"'{unknown}'", StringComparison.Ordinal)
            && exception.Message.Contains("(2, 1)", StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException("등록되지 않은 기호와 좌표를 함께 보고해야 한다.");
    }

    private static char FindUnknownSymbol(TownLegendTable legend)
    {
        for (int code = '!'; code <= char.MaxValue; code++)
        {
            char symbol = (char)code;
            if (!legend.ContainsBySymbol(symbol.ToString()))
            {
                return symbol;
            }
        }

        throw new InvalidOperationException("미등록 기호 검사에 사용할 빈 기호를 찾을 수 없습니다.");
    }

    private static void CheckSwappedItems(GameContent original, GameContent edited, GameSession originalTown)
    {
        GameSession editedTown = TownMap.Create(edited, new Random(1));
        ThingRow potion = original.Tables.Thing.Get("potion");
        ThingRow gold = original.Tables.Thing.Get("gold");
        int swappedCount = 0;
        foreach (Thing thing in originalTown.Map.Things)
        {
            if (thing.Def != potion && thing.Def != gold)
            {
                continue;
            }

            ThingRow expected = edited.Tables.Thing.Get(thing.Def == potion ? "gold" : "potion");
            Check(editedTown.Map.GetItemAtOrNull(thing.X, thing.Y)?.Def == expected, $"범례를 교환한 뒤 ({thing.X}, {thing.Y})에 다른 아이템이 배치된다.");
            swappedCount++;
        }

        Check(swappedCount > 0, "마을에서 범례 교환을 확인할 물약이나 골드를 찾았다.");
    }

    private static void CheckPotionAction(GameContent edited, GameTables original, int healing)
    {
        string row = ZString.Concat(original.TownLegend.Get("hero").Symbol, original.TownLegend.Get("spike_trap").Symbol,
            original.TownLegend.Get("gold").Symbol);
        GameSession session = TownMap.Create(edited, new Random(1), new string[] { row });
        Check(session.Player.Def.MaxHp == healing + 5, "플레이어의 최대 체력이 복사본의 Actor 표와 일치한다.");
        Check(session.Runner.SubmitPlayerAction(MoveAction.RIGHT).IsExecuted, "함정 칸에 진입한다.");
        int injuredHp = session.Player.Hp;
        Check(injuredHp == session.Player.Def.MaxHp - healing - 1, "가시 함정이 복사본의 StepDamage만큼 피해를 준다.");
        Check(!session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT).IsExecuted, "가시 함정은 주울 수 없다.");
        Check(session.Runner.SubmitPlayerAction(MoveAction.RIGHT).IsExecuted, "물약 칸에 진입한다.");
        Check(session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT).IsExecuted, "물약을 줍는다.");
        Check(session.Player.Hp == injuredHp + healing, $"물약을 주우면 복사본에 지정한 체력 {healing}을 회복한다.");
    }

    private static void Check(bool bCondition, string message)
    {
        _checkCount++;
        if (!bCondition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
