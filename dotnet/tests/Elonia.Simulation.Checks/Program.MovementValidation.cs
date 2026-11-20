using System;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckMovementValidation()
    {
        Map map = new Map(4, 1);
        Actor actor = TestActors.Create(1, -1, 0, 10);
        actor.MoveTo(0, 0);
        map.AddEntity(actor);
        Actor neighbor = map.AddEntity(TestActors.Create(2, 2, 0, 10));
        map.SetProp(3, 0, new TestPropDef("Wall", false));

        actor.MoveTo(1, 0);
        Check(actor.X == 1 && actor.Y == 0 && map.GetCell(0, 0).CanEnter
            && map.GetLivingActorAtOrNull(1, 0) == actor, "이동한 개체의 좌표와 점유 칸이 일치한다.");

        Map other = new Map(4, 1);
        CheckThrows<ArgumentException>(() => actor.MoveTo(2, 0), "다른 액터를 덮어쓸 수 없다.");
        CheckThrows<ArgumentException>(() => map.MoveActor(actor, 3, 0), "막힌 칸으로 갈 수 없다.");
        CheckThrows<ArgumentException>(() => map.MoveActor(actor, -1, 0), "맵 밖으로 이동할 수 없다.");
        CheckThrows<ArgumentException>(() => other.MoveActor(actor, 0, 0), "다른 맵의 액터를 이동시킬 수 없다.");
        CheckThrows<ArgumentException>(() => other.AddEntity(actor), "개체는 동시에 두 맵에 등록될 수 없다.");
        Check(actor.X == 1 && map.GetLivingActorAtOrNull(1, 0) == actor && map.GetLivingActorAtOrNull(2, 0) == neighbor
            && other.Actors.Count == 0 && other.GetCell(0, 0).CanEnter, "거부된 이동은 두 맵의 점유 상태를 보존한다.");

        Thing item = map.AddEntity(new Thing(3, TestItems.GOLD, 0, 0));
        CheckThrows<InvalidOperationException>(() => item.MoveTo(1, 0), "등록된 물건의 좌표만 바꿀 수 없다.");
        Check(item.X == 0 && map.GetItemAtOrNull(0, 0) == item, "거부된 이동은 물건의 좌표와 칸을 보존한다.");
        map.RemoveThing(item);
        item.MoveTo(1, 0);
        other.AddEntity(item);
        Check(!map.Contains(item) && map.GetItemAtOrNull(0, 0) == null
            && other.Contains(item) && other.GetItemAtOrNull(1, 0) == item, "물건을 치운 뒤 다른 맵에 다시 배치할 수 있다.");

        neighbor.TakeDamage(neighbor.Hp);
        actor.MoveTo(2, 0);
        CheckThrows<InvalidOperationException>(() => neighbor.Heal(1), "죽은 액터를 회복시켜 한 칸에 겹칠 수 없다.");
        Check(!neighbor.IsAlive && map.GetLivingActorAtOrNull(2, 0) == actor, "이미 다른 액터가 들어간 칸의 점유 상태가 유지된다.");
    }
}
