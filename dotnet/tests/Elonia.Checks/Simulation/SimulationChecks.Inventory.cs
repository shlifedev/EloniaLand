using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Simulation.Checks;

internal static partial class SimulationChecks
{
    private static void CheckInventory()
    {
        Inventory inventory = new Inventory();
        TestThingDef first = new TestThingDef("branch", true);
        TestThingDef second = new TestThingDef("branch", true);
        CheckEqual(0, inventory.Stacks.Count, "새 인벤토리는 비어 있다.");
        CheckEqual(0, inventory.GetCount(first), "없는 물건의 수량은 0이다.");
        inventory.Add(first, 3);
        inventory.Add(first, 2);
        inventory.Add(second, 4);
        CheckEqual(2, inventory.Stacks.Count, "ID가 같아도 다른 정의는 별도 스택에 들어간다.");
        Check(inventory.Stacks[0].Def == first && inventory.Stacks[1].Def == second, "스택은 얻은 순서대로 정의를 보관한다.");
        CheckEqual(5, inventory.Stacks[0].Count, "같은 정의로 추가한 수량은 합쳐진다.");
        CheckEqual(5, inventory.GetCount(first), "합친 수량을 정의로 조회한다.");
        CheckEqual(4, inventory.GetCount(second), "다른 정의의 수량은 별도로 조회한다.");
    }
}
