using System.Collections.Generic;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
{
    private static void CheckRuleInteractions(GameContent content)
    {
        PlaySession play = new PlaySession(CreateMap(content, "@s..Twv$"));
        GameSession session = play.Session;
        Actor hero = session.Player;
        Actor slime = session.Map.Actors[1];
        Actor rat = CreateMap(content, "@m").Map.Actors[1];
        Check(slime.Row().IsHostile && slime.IsThreat(), "적대적이고 공격할 수 있는 액터는 위협이다.");
        Check(rat.Row().IsHostile && !rat.IsThreat(), "적대적이어도 공격력이 없으면 위협이 아니다.");
        Check(!hero.Row().IsHostile && !hero.IsThreat(), "비적대적이면 공격력이 있어도 위협이 아니다.");
        List<MenuChoice> choices = new List<MenuChoice>();
        foreach (int x in new int[] { 0, 2, 5, -1 })
        {
            play.CollectMenuChoices(x, 0, choices);
            Check(choices.Count == 0 || (choices.Count == 1 && choices[0].InteractionOrNull == null),
                "자기 자신, 빈 칸, 벽, 맵 밖에서는 규칙이 일을 제안하지 않는다.");
        }

        play.CollectMenuChoices(6, 0, choices);
        Check(choices.Count == 2 && choices[0].InteractionOrNull?.Action is TalkAction, "주민에게는 말 걸기만 제안한다.");

        play.CollectMenuChoices(4, 0, choices);
        Interaction harvest = choices[0].InteractionOrNull!.Value;
        Check(harvest.Action is HarvestAction && harvest.TargetX == 4 && harvest.TargetY == 0 && harvest.Reach == 1
            && !harvest.IsInReach(hero) && !session.Runner.SubmitPlayerAction(harvest.Action).IsExecuted,
            "먼 나무를 캐는 일은 목록에 보이지만 옆 칸에 도착해야 실행된다.");
        play.CollectMenuChoices(7, 0, choices);
        Interaction pickup = choices[0].InteractionOrNull!.Value;
        Check(pickup.Action is PickUpAction && pickup.Reach == 0 && pickup.TargetX == 7 && pickup.TargetY == 0,
            "아이템은 그 칸에서 주울 수 있다.");

        session.Map.AddEntity(new Thing(session.Map.CreateEntityID(), content.Tables.Thing.Get("gold"), 1, 0));
        session.Map.SetProp(1, 0, content.Tables.Prop.Get("tree"));
        play.CollectMenuChoices(1, 0, choices);
        Check(choices.Count == 4 && choices[0].InteractionOrNull?.Action is AttackAction
            && choices[1].InteractionOrNull?.Action is HarvestAction && choices[2].InteractionOrNull?.Action is PickUpAction,
            "같은 칸의 규칙은 공격, 채집, 줍기 순서로 일을 제안한다.");
        Interaction attack = choices[0].InteractionOrNull!.Value;
        Check(attack.TargetActorOrNull == slime && attack.Reach == AttackAction.REACH && attack.IsInReach(hero), "옆 슬라임을 가리키는 공격을 얻는다.");
        Check(play.Execute(new MoveCommand(EDirection.Right)) && slime.Hp == slime.MaxHp - hero.Stat(EStat.Attack) && hero.X == 0,
            "부딪히면 첫 번째 인접 행동인 공격을 실행한다.");
        session.Map.RemoveProp(1, 0);
        for (int hit = 0; hit < slime.MaxHp && slime.IsAlive; hit++)
        {
            session.Runner.SubmitPlayerAction(attack.Action);
        }

        play.CollectMenuChoices(1, 0, choices);
        Check(!slime.IsAlive && choices.Count == 2 && choices[0].InteractionOrNull?.Action is PickUpAction,
            "적이 죽으면 공격은 없어지고 그 밑의 아이템만 제안한다.");
        Check(play.Execute(new MoveCommand(EDirection.Right)) && hero.X == 1 && session.Map.GetItemAtOrNull(1, 0) != null,
            "아이템 칸에 부딪혀도 자동으로 줍지 않는다.");
        CheckInteractByActionId(content);
        CheckPickUpAtTargetTile(content);
    }

    private static void CheckInteractByActionId(GameContent content)
    {
        PlaySession play = CreatePlay(content, content.Tables, "hero", "slime");
        GameSession session = play.Session;
        Actor slime = session.Map.Actors[1];
        session.Map.SetProp(1, 0, content.Tables.Prop.Get("tree"));
        session.Player.IsInvincible = true;
        Check(play.Execute(new InteractCommand(1, 0, "harvest")) && session.Map.GetPropOrNull(1, 0) == null && slime.Hp == slime.MaxHp,
            "행동 ID로 고른 칸의 일은 순서와 상관없이 그 종류의 일(채집)이다.");
        Check(!play.Execute(new InteractCommand(1, 0, "harvest")), "그 칸에 없는 종류의 일은 하지 않는다.");
    }

    private static void CheckPickUpAtTargetTile(GameContent content)
    {
        PlaySession play = CreatePlay(content, content.Tables, "hero", "gold");
        Map map = play.Session.Map;
        Thing underfoot = map.AddEntity(new Thing(map.CreateEntityID(), content.Tables.Thing.Get("branch"), 0, 0));
        Thing? target = map.GetItemAtOrNull(1, 0);
        long tick = play.Session.World.Tick;

        Check(!play.Execute(new InteractCommand(1, 0, "pick_up")), "다른 칸을 지정한 줍기는 거부된다.");
        Check(!play.Replay(PlayerCommandText.Parse("[1,0,pick_up]"), 0), "재현한 줍기 명령도 지정한 칸까지 닿아야 한다.");
        Check(map.GetItemAtOrNull(0, 0) == underfoot && target != null && map.GetItemAtOrNull(1, 0) == target,
            "거부된 줍기는 발밑과 목표 칸의 아이템을 모두 남겨 둔다.");
        Check(play.Session.World.Tick == tick && play.GetCommandText() == "[1,0,pick_up][1,0,pick_up]",
            "두 번의 시도가 입력 기록에 남고 시간은 흐르지 않는다.");

        play.Execute(new MoveCommand(EDirection.Right));
        Check(play.Execute(new InteractCommand(1, 0, "pick_up")) && map.GetItemAtOrNull(1, 0) == null
            && map.GetItemAtOrNull(0, 0) == underfoot && play.Session.Player.Gold > 0, "목표 칸에 도착한 뒤에는 그 칸의 골드만 줍는다.");
    }
}
