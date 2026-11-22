using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Data;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
{
    private static void CheckItems(GameContent content, GameContent edited)
    {
        CheckPotionDuringCombat(content);

        GameSession session = CreateMap(content, "@...!");
        Map map = session.Map;
        Actor player = session.Player;
        RuleRecorder recorder = new RuleRecorder(session);
        ThingRow branch = content.Tables.Thing.Get("branch");
        ThingRow potion = content.Tables.Thing.Get("potion");
        ThingRow gold = content.Tables.Thing.Get("gold");
        Thing first = map.AddEntity(new Thing(map.CreateEntityID(), branch, 0, 0, 3));
        Thing second = map.AddEntity(new Thing(map.CreateEntityID(), branch, 1, 0, 2));
        Thing coins = map.AddEntity(new Thing(map.CreateEntityID(), gold, 2, 0, 7));
        Thing healing = map.AddEntity(new Thing(map.CreateEntityID(), potion, 3, 0));
        long tick = session.World.Tick;
        Check(session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT).IsExecuted, "발밑의 재료를 줍는다.");
        Check(map.GetItemAtOrNull(0, 0) == null && map.Things.Count == 4 && player.Gold == 0, "재료를 주우면 그 물건만 맵에서 사라지고 골드는 그대로다.");
        Check(session.World.Tick == tick + ActorAction.DEFAULT_COST / player.Speed, "줍기는 기본 행동 비용을 쓴다.");
        Check(!session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT).IsExecuted, "빈 칸에서는 주울 수 없다.");
        session.Runner.SubmitPlayerAction(MoveAction.RIGHT);
        session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        int units = content.Tables.Item.Get(branch.Item).Amount;
        Check(player.Inventory.Stacks.Count == 1 && player.Inventory.Stacks[0].Def == branch
            && player.Inventory.GetCount(branch) == (first.Count + second.Count) * units, "같은 재료는 하나의 행을 공유하며 수량을 합친다.");
        session.Runner.SubmitPlayerAction(MoveAction.RIGHT);
        session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        int money = coins.Count * content.Tables.Item.Get(gold.Item).Amount;
        Check(player.Gold == money && player.Inventory.GetCount(gold) == 0, "골드는 수량과 행의 Amount를 곱해 즉시 얻는다.");
        player.TakeDamage(player.Hp - 1);
        session.Runner.SubmitPlayerAction(MoveAction.RIGHT);
        session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        int healed = content.Tables.Item.Get(potion.Item).Amount;
        Check(player.Hp == 1 + healed && player.Inventory.GetCount(potion) == 0, "물약은 행에 지정한 만큼 회복한다.");
        session.Runner.SubmitPlayerAction(MoveAction.RIGHT);
        player.Heal(player.MaxHp);
        session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        Check(player.Hp == player.MaxHp && player.Inventory.Stacks.Count == 1 && map.Things.Count == 0,
            "체력이 가득 차도 물약은 소모되며 최대 체력을 넘지 않는다.");
        Check(recorder.Pickups.Count == 5, "각 줍기를 한 번씩 알린다.");
        CheckPickup(recorder.Pickups[0], player, first, EItemEffect.None, 3 * units);
        CheckPickup(recorder.Pickups[1], player, second, EItemEffect.None, 2 * units);
        CheckPickup(recorder.Pickups[2], player, coins, EItemEffect.Gold, money);
        CheckPickup(recorder.Pickups[3], player, healing, EItemEffect.Heal, healed);

        GameSession bundles = CreateMap(edited, "@");
        ThingRow bundle = edited.Tables.Thing.Get("branch");
        bundles.Map.AddEntity(new Thing(2, bundle, 0, 0, 3));
        bundles.Runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        Check(bundles.Player.Inventory.GetCount(bundle) == 6 && bundles.Player.Inventory.GetCount(branch) == 0,
            "재료도 Amount와 수량을 곱하며, 다른 표 묶음의 행은 ID가 같아도 별도 정의다.");
    }

    private static void CheckPotionDuringCombat(GameContent content)
    {
        GameSession session = CreateMap(content, "@s");
        Actor hero = session.Player;
        Actor slime = session.Map.Actors[1];
        ThingRow potion = content.Tables.Thing.Get("potion");
        session.Map.AddEntity(new Thing(session.Map.CreateEntityID(), potion, hero.X, hero.Y));
        int healing = content.Tables.Item.Get(potion.Item).Amount;

        for (int turn = 0; turn < 100 && (hero.MaxHp - hero.Hp < healing || session.Runner.GetTicksUntilReady(slime) > 0); turn++)
        {
            session.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
        }

        int hp = hero.Hp;
        long attacks = slime.ActionCount;
        Check(hero.IsAlive && hero.MaxHp - hp >= healing && session.Runner.GetTicksUntilReady(slime) == 0,
            "다친 영웅 옆의 슬라임이 같은 틱에 반격할 준비가 되었다.");
        Check(session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT).IsExecuted, "전투 중에도 발밑의 물약을 마신다.");
        Check(hero.Hp == hp + healing - slime.Stat(EStat.Attack) && slime.ActionCount == attacks + 1 && slime.Hp == slime.MaxHp,
            "물약을 먼저 마시고 한 번 반격을 받으며 슬라임의 체력은 그대로다.");
    }

    private static void CheckPickup(ItemPickedUp pickup, Actor actor, Thing thing, EItemEffect effect, int amount)
    {
        Check(pickup.Actor == actor && pickup.Thing == thing && pickup.Effect == effect && pickup.Amount == amount,
            "줍기 이벤트가 주운 액터와 물건, 효과, 총량을 전달한다.");
    }

    private static void CheckHarvest(GameContent content)
    {
        GameSession session = CreateMap(content, "w..", "@TT", ".T.");
        Actor hero = session.Player;
        Map map = session.Map;
        GameRunner runner = session.Runner;
        RuleRecorder recorder = new RuleRecorder(session);
        PropRow tree = content.Tables.Prop.Get("tree");
        FloorRow dirt = content.Tables.Floor.Get("dirt");
        map.SetFloor(1, 1, dirt);
        long tick = session.World.Tick;
        foreach ((int x, int y) in new (int, int)[] { (-1, 1), (0, 0), (0, 2), (2, 1), (2, 2), (0, 1) })
        {
            Check(!runner.SubmitPlayerAction(new HarvestAction(x, y)).IsExecuted, $"캐기 대상이 될 수 없는 ({x}, {y})는 거부된다.");
        }

        Check(session.World.Tick == tick && map.Things.Count == 0 && recorder.Harvests.Count == 0, "거부된 채집은 시간, 물건, 이벤트를 바꾸지 않는다.");
        Check(map.GetPropOrNull(2, 1) == tree && map.GetPropOrNull(0, 0) == content.Tables.Prop.Get("wall")
            && map.GetPropOrNull(0, 2) == null, "거부된 칸의 나무, 벽, 빈 땅이 그대로다.");
        HarvestAction chop = new HarvestAction(1, 1);
        for (int turn = 1; turn < HarvestAction.TURNS; turn++)
        {
            Check(runner.SubmitPlayerAction(chop).IsExecuted && chop.Progress == turn && !chop.IsFinished, "한 차례씩 나무를 벤다.");
        }

        Check(map.GetPropOrNull(1, 1) == tree && map.Things.Count == 0 && recorder.Harvests.Count == 0, "마지막 차례 전에는 결과가 없다.");
        Check(runner.SubmitPlayerAction(chop).IsExecuted && chop.IsFinished, "마지막 차례에 나무를 캔다.");
        Check(!runner.SubmitPlayerAction(chop).IsExecuted, "끝난 작업은 다시 실행되지 않는다.");
        Thing? droppedOrNull = map.GetItemAtOrNull(1, 1);
        Check(droppedOrNull != null && droppedOrNull.Def == content.Tables.Thing.Get(tree.DropThing)
            && droppedOrNull.Count == tree.DropAmount && droppedOrNull.ID != hero.ID, "채집 결과는 새 ID와 표가 가리킨 행 및 수량을 가진다.");
        Check(map.GetPropOrNull(1, 1) == null && map.GetFloor(1, 1) == dirt && hero.X == 0 && hero.Y == 1,
            "나무만 사라지고 바닥과 플레이어 위치는 유지된다.");
        Check(session.World.Tick == tick + HarvestAction.TURNS * ActorAction.DEFAULT_COST / hero.Speed, "채집은 차례마다 기본 비용을 쓴다.");
        Check(recorder.Harvests.Count == 1, "채집 이벤트가 한 번 발생한다.");
        PropHarvested harvest = recorder.Harvests[0];
        Check(harvest.Actor == hero && harvest.X == 1 && harvest.Y == 1 && harvest.HarvestedProp == tree
            && harvest.DroppedThing == droppedOrNull, "채집 이벤트가 행과 떨어진 물건, 채집자, 위치를 전달한다.");
        tick = session.World.Tick;
        Check(!runner.SubmitPlayerAction(new HarvestAction(1, 1)).IsExecuted && session.World.Tick == tick
            && map.Things.Count == 1 && recorder.Harvests.Count == 1, "같은 칸을 다시 캐면 추가 효과가 없다.");
        Check(runner.SubmitPlayerAction(MoveAction.RIGHT).IsExecuted && hero.X == 1, "채집한 자리로 걸어갈 수 있다.");

        Thing buried = map.AddEntity(new Thing(map.CreateEntityID(), content.Tables.Thing.Get("gold"), 1, 0));
        map.SetProp(1, 0, tree);
        tick = session.World.Tick;
        Check(!runner.SubmitPlayerAction(new HarvestAction(1, 0)).IsExecuted && map.GetPropOrNull(1, 0) == tree
            && map.GetItemAtOrNull(1, 0) == buried && session.World.Tick == tick && recorder.Harvests.Count == 1,
            "이미 아이템이 놓여 있으면 나무와 아이템을 보존하고 거부한다.");

        GameContent blocked = WorkbookFixture.CreateContent(new Dictionary<(string, string, string), string>
        {
            [("Floor", "dirt", "IsWalkable")] = "false"
        });
        GameSession flooded = CreateMap(blocked, "@T");
        flooded.Map.SetFloor(1, 0, blocked.Tables.Floor.Get("dirt"));
        tick = flooded.World.Tick;
        RuleRecorder floodEvents = new RuleRecorder(flooded);
        Check(!flooded.Runner.SubmitPlayerAction(new HarvestAction(1, 0)).IsExecuted && flooded.World.Tick == tick
            && flooded.Map.GetPropOrNull(1, 0) == blocked.Tables.Prop.Get("tree") && flooded.Map.Things.Count == 0
            && floodEvents.Harvests.Count == 0, "막힌 바닥에서는 수확물을 놓을 수 없어 채집을 거부한다.");
    }

    private static void CheckStepTriggers(GameContent content)
    {
        GameSession session = CreateMap(content, "@^");
        RuleRecorder recorder = new RuleRecorder(session);
        Actor hero = session.Player;
        Thing spikes = session.Map.Things[0];
        int damage = spikes.Row().StepDamage;
        Check(session.Runner.SubmitPlayerAction(MoveAction.RIGHT).IsExecuted && hero.X == 1, "함정 칸에 들어간다.");
        Check(hero.Hp == hero.MaxHp - damage && session.Map.Contains(spikes), "함정이 행의 피해를 입히고 맵에 남는다.");
        Check(recorder.Log.Count == 2 && recorder.Log[0] == $"밟기 {hero.ID}>{spikes.ID}" && recorder.Log[1] == $"피해 {hero.ID} {damage}",
            "밟기 이벤트 다음에 피해를 알린다.");
        Check(recorder.Damages[0].SourceOrNull == null, "함정 피해에는 공격자가 없다.");
        Check(!session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT).IsExecuted, "아이템 행이 없는 함정은 주울 수 없다.");
        session.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
        session.Runner.SubmitPlayerAction(MoveAction.LEFT);
        Check(recorder.Log.Count == 2, "서 있거나 떠날 때에는 함정을 발동하지 않는다.");
        session.Runner.SubmitPlayerAction(MoveAction.RIGHT);
        Check(recorder.Log.Count == 4 && hero.Hp == hero.MaxHp - damage * 2, "다시 밟으면 다시 피해를 입는다.");

        GameSession npcMap = CreateMap(content, "@^s");
        Actor slime = npcMap.Map.Actors[1];
        WaitForNpc(npcMap, slime);
        Check(slime.X == 1 && slime.Hp == slime.MaxHp - damage, "이동하는 NPC에게도 함정이 적용된다.");

        GameSession lethal = CreateMap(content, "@^");
        Actor victim = lethal.Player;
        victim.TakeDamage(victim.Hp - damage - 1);
        Thing first = lethal.Map.Things[0];
        Thing second = lethal.Map.AddEntity(new Thing(lethal.Map.CreateEntityID(), first.Def, 1, 0));
        lethal.Map.AddEntity(new Thing(lethal.Map.CreateEntityID(), first.Def, 1, 0));
        RuleRecorder events = new RuleRecorder(lethal);
        lethal.Runner.SubmitPlayerAction(MoveAction.RIGHT);
        Check(lethal.Runner.IsGameOver && !victim.IsAlive, "치명적인 함정은 플레이어를 죽여 게임을 끝낸다.");
        Check(ZString.Join(",", events.Log) == $"밟기 {victim.ID}>{first.ID},피해 {victim.ID} {damage},밟기 {victim.ID}>{second.ID},피해 {victim.ID} {damage},사망 {victim.ID}",
            "한 칸의 함정은 추가 순서대로 작동하고 사망을 한 번 알린 뒤 나머지 함정은 멈춘다.");
    }
}
