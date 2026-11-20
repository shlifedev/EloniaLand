using System.Collections.Generic;
using Elonia.Game;
using Elonia.Simulation;

namespace Elonia.Checks;

internal static partial class Program
{
    private static void CheckRuleInteractions(GameContent content)
    {
        PlaySession play = new PlaySession(CreateMap(content, "@s..Twv$"));
        GameSession session = play.Session;
        Actor hero = session.Player;
        Actor slime = session.Map.Actors[1];
        Check(slime.Row().IsHostile && slime.Row().IsThreat(), "적대적이고 공격할 수 있는 행은 위협이다.");
        Check(content.Tables.Actor.Get("rat").IsHostile && !content.Tables.Actor.Get("rat").IsThreat(), "적대적이어도 공격력이 없으면 위협이 아니다.");
        Check(!hero.Row().IsHostile && !hero.Row().IsThreat(), "비적대적이면 공격력이 있어도 위협이 아니다.");
        List<MenuChoice> choices = new List<MenuChoice>();
        foreach (int x in new int[] { 0, 2, 5, 6, -1 })
        {
            play.CollectMenuChoices(x, 0, choices);
            Check(choices.Count == 0 || (choices.Count == 1 && choices[0].InteractionOrNull == null),
                "자기 자신, 빈 칸, 벽, 주민, 맵 밖에서는 규칙이 일을 제안하지 않는다.");
        }

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
        Check(play.Execute(EPlayerCommand.MoveRight).IsExecuted && slime.Hp == slime.Def.MaxHp - hero.Row().Attack && hero.X == 0,
            "부딪히면 첫 번째 인접 행동인 공격을 실행한다.");
        session.Map.RemoveProp(1, 0);
        for (int hit = 0; hit < slime.Def.MaxHp && slime.IsAlive; hit++)
        {
            session.Runner.SubmitPlayerAction(attack.Action);
        }

        play.CollectMenuChoices(1, 0, choices);
        Check(!slime.IsAlive && choices.Count == 2 && choices[0].InteractionOrNull?.Action is PickUpAction,
            "적이 죽으면 공격은 없어지고 그 밑의 아이템만 제안한다.");
        Check(play.Execute(EPlayerCommand.MoveRight).IsExecuted && hero.X == 1 && session.Map.GetItemAtOrNull(1, 0) != null,
            "아이템 칸에 부딪혀도 자동으로 줍지 않는다.");
    }
}
