using System;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckInteractions()
    {
        Map map = new Map(4, 1);
        Actor player = map.AddEntity(TestActors.Create(1, 0, 0, 10));
        Actor target = map.AddEntity(TestActors.Create(2, 1, 0, 10));
        InteractionPlan plan = new InteractionPlan(new TestRule(0), new TestRule(1), new TestRule(1));
        CheckThrows<ArgumentNullException>(() => plan.Collect(null!, player, 0, 0), "상호작용을 모으려면 맵이 필요하다.");
        CheckThrows<ArgumentNullException>(() => plan.Collect(map, null!, 0, 0), "상호작용을 모으려면 액터가 필요하다.");
        plan.Collect(map, player, 1, 0);
        CheckEqual(3, plan.Items.Count, "같은 칸에서 규칙마다 일을 추가할 수 있다.");
        Check(plan.TryGetFirst(0, out Interaction onTile) && onTile.TargetX == 1 && onTile.TargetY == 0,
            "거리 0의 일은 지정한 칸을 가리킨다.");
        Check(plan.TryGetFirst(1, out Interaction adjacent) && adjacent.Action == plan.Items[1].Action,
            "입력 순서대로 같은 거리의 첫 번째 일을 고른다.");
        Check(adjacent.TargetActorOrNull == target && adjacent.IsInReach(player), "옆 칸의 액터가 대상이면 닿는다.");
        Check(!plan.TryGetFirst(2, out _), "등록되지 않은 거리는 찾을 수 없다.");
        Check(plan.TryCollectBump(map, player, 1, 0, out Interaction bump) && bump.Action == adjacent.Action,
            "부딪히면 거리 1의 첫 번째 일을 고른다.");
        map.MoveActor(target, 2, 0);
        Check(adjacent.TargetX == 2 && !adjacent.IsInReach(player), "액터가 움직이면 대상 칸과 거리가 바뀐다.");
        Check(!plan.TryCollectBump(map, player, 1, 0, out _) && plan.Items.Count == 1, "거리 0의 일만 있으면 부딪히기로 할 일이 없다.");
        plan.Collect(map, player, -1, 0);
        CheckEqual(0, plan.Items.Count, "맵 밖을 조회하면 앞선 목록이 비워진다.");
    }

    private sealed class TestRule : IInteractionRule
    {
        private readonly int _reach;
        private readonly CostAction _action = new CostAction(100);

        public TestRule(int reach)
        {
            _reach = reach;
        }

        public void Collect(InteractionPlan plan, Map map, Actor actor, int x, int y)
        {
            if (_reach == 0)
            {
                plan.Add(_action, x, y, _reach);
            }
            else if (map.GetLivingActorAtOrNull(x, y) is Actor target)
            {
                plan.Add(_action, target, _reach);
            }
        }
    }
}
