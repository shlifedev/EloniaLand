using System;
using Elonia.Data;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
{
    private static void CheckBehaviors(GameContent content)
    {
        GameSession field = CreateMap(content, "@..m.....");
        Actor rat = field.Map.Actors[1];
        Brain brain = (Brain)rat.ControllerOrNull!;
        WaitForNpc(field, rat);
        Check(rat.X == 4 && rat.Y == 0 && brain.CurrentBehaviorOrNull is FleeBehavior flee
            && flee.Threat == field.Player && flee.SenseRange == rat.Row().SenseRange && brain.LastActionOrNull == MoveAction.RIGHT, "쥐가 감지 범위 안에서 멀어지는 방향으로 도망간다.");
        WaitForNpc(field, rat);
        Check(rat.X == 5 && brain.CurrentBehaviorOrNull is FleeBehavior, "거리와 감지 범위가 같아도 도망간다.");
        WaitForNpc(field, rat);
        Check(brain.CurrentBehaviorOrNull is WanderBehavior, "범위 밖에 나가면 배회를 재개한다.");

        CheckApproachingRat(content);

        GameSession wall = CreateMap(content, ".m.", "...", ".@.");
        Actor wallRat = wall.Map.Actors[1];
        WaitForNpc(wall, wallRat);
        Check(wallRat.X == 0 && wallRat.Y == 0, "등 뒤가 벽이면 벽을 따라 옆으로 빠진다.");
        GameSession corner = CreateMap(content, "m..", "...", "..@");
        Actor cornerRat = corner.Map.Actors[1];
        WaitForNpc(corner, cornerRat);
        Check(cornerRat.X == 0 && cornerRat.Y == 0 && ((Brain)cornerRat.ControllerOrNull!).LastActionOrNull == WaitAction.DEFAULT,
            "구석에서는 제자리에서 대기로 차례를 쓴다.");

        GameSession shared = TownMap.Create(content, 42, new string[] { "@..", ".v.", "..." });
        Actor wanderingVillager = shared.Map.Actors[1];
        CheckEqual(3, shared.World.Random.NextInt(4), "첫 방향을 월드에서 직접 뽑는다.");
        WaitForNpc(shared, wanderingVillager);
        Check(wanderingVillager.X == 1 && wanderingVillager.Y == 2, "주민의 배회가 월드의 다음 방향을 소비한다.");
        CheckEqual(0, shared.World.Random.NextInt(4), "배회 직후 월드에서 세 번째 방향을 뽑는다.");

        GameSession first = CreateMap(content, "@......v..");
        GameSession second = CreateMap(content, "@......v..");
        Actor villager = first.Map.Actors[1];
        Actor other = second.Map.Actors[1];
        bool bMoved = false;
        bool bPathsMatch = true;
        for (int index = 0; index < 500; index++)
        {
            WaitForNpc(first, villager);
            WaitForNpc(second, other);
            bPathsMatch &= first.Map.IsInside(villager.X, villager.Y) && villager.X == other.X && villager.Y == other.Y;
            bMoved |= villager.X != 7;
        }

        Check(bPathsMatch, "배회 경로는 맵 안에 머물고 같은 시드로 재현된다.");
        Check(bMoved && villager.ActionCount == 500 && first.World.Tick == second.World.Tick, "배회하는 주민이 매 차례 행동한다.");

        GameSession distant = CreateMap(content, "@......s");
        Actor farSlime = distant.Map.Actors[1];
        WaitForNpc(distant, farSlime);
        Check(((Brain)farSlime.ControllerOrNull!).CurrentBehaviorOrNull is WanderBehavior, "감지 범위 밖에서는 추격하지 않는다.");
        GameSession quiet = CreateMap(content, "@.......");
        Brain onlyFlee = new Brain(new FleeBehavior(quiet.Player, rat.Row().SenseRange));
        Brain onlyChase = new Brain(new ChaseBehavior(quiet.Player, farSlime.Row().SenseRange));
        Actor quietRat = quiet.Map.AddEntity(new Actor(2, rat.Def, new ActorStats(rat.Row()), 6, 0, onlyFlee));
        Actor quietSlime = quiet.Map.AddEntity(new Actor(3, farSlime.Def, new ActorStats(farSlime.Row()), 7, 0, onlyChase));
        WaitForNpc(quiet, quietSlime);
        Check(quietRat.ActionCount > 0 && quietRat.X == 6 && onlyFlee.CurrentBehaviorOrNull == null
            && quietSlime.X == 7 && onlyChase.CurrentBehaviorOrNull == null, "다른 행동 방식이 없으면 감지 범위 밖의 쥐와 슬라임은 제자리에 머문다.");
        GameSession blocked = CreateMap(content, "@ws");
        Actor trapped = blocked.Map.Actors[1];
        WaitForNpc(blocked, trapped);
        Check(trapped.X == 2 && ((Brain)trapped.ControllerOrNull!).LastActionOrNull == WaitAction.DEFAULT,
            "가까워질 수 있는 모든 칸이 막혀 있으면 추격 중에도 제자리에서 기다린다.");

        GameSession detour = CreateMap(content, "@...", "....");
        ActorRow slimeRow = content.Tables.Actor.Get("slime");
        ActorRow villagerRow = content.Tables.Actor.Get("villager");
        Actor chaser = detour.Map.AddEntity(new Actor(3, slimeRow, new ActorStats(slimeRow), 3, 1,
            new Brain(new ChaseBehavior(detour.Player, 5))));
        detour.Map.AddEntity(new Actor(4, villagerRow, new ActorStats(villagerRow), 2, 0, new WaitingController()));
        WaitForNpc(detour, chaser);
        Check(chaser.X == 2 && chaser.Y == 1, "가장 가까운 칸을 액터가 막으면 다른 인접 칸으로 접근한다.");
    }

    private static void CheckApproachingRat(GameContent content)
    {
        GameSession field = CreateMap(content,
            "................", "................", "...............m", "................",
            "................", ".@..............", "................", "................");
        Actor player = field.Player;
        Actor rat = field.Map.Actors[1];
        Brain brain = (Brain)rat.ControllerOrNull!;
        int range = rat.Row().SenseRange;
        bool bWandered = false;
        bool bInterrupted = false;
        bool bEveryChoiceMatches = true;
        for (int index = 0; index < 200; index++)
        {
            int x = rat.X;
            int y = rat.Y;
            long before = rat.ActionCount;
            ActorAction step;
            if (player.X != x)
            {
                step = player.X < x ? MoveAction.RIGHT : MoveAction.LEFT;
            }
            else
            {
                step = player.Y < y ? MoveAction.DOWN : MoveAction.UP;
            }

            if (!field.Runner.SubmitPlayerAction(step).IsExecuted)
            {
                field.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
            }

            if (before == rat.ActionCount)
            {
                continue;
            }

            int dx = x - player.X;
            int dy = y - player.Y;
            bool bInRange = dx * dx + dy * dy <= range * range;
            if (bInRange)
            {
                bEveryChoiceMatches &= brain.CurrentBehaviorOrNull is FleeBehavior;
                bInterrupted |= bWandered;
            }
            else
            {
                bEveryChoiceMatches &= brain.CurrentBehaviorOrNull is WanderBehavior;
                bWandered = true;
            }
        }

        Check(bWandered && bInterrupted, "실제로 접근하면 쥐가 배회를 중단하고 도망간다.");
        Check(bEveryChoiceMatches, "쥐가 행동한 모든 차례에 두 좌표의 거리와 감지 범위가 도주 여부를 결정한다.");
    }

    private sealed class WaitingController : IActorController
    {
        public ActorAction ChooseAction(ActionContext context)
        {
            return WaitAction.DEFAULT;
        }
    }
}
