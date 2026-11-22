using System.Collections.Generic;
using Elonia.Data;
using Elonia.Game;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
{
    private static void CheckDialogue(GameContent content, GameTables tables)
    {
        CheckBumpDoesNotTalk(content, tables);
        CheckPromiseDialogue(content, tables);
        CheckDeclineDialogue(content, tables);
        CheckLeaveDialogue(content, tables);
    }

    private static void CheckBumpDoesNotTalk(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "villager");
        EventRecorder recorder = new EventRecorder(play.Session);
        Check(!play.Execute(new MoveCommand(EDirection.Right)) && recorder.BlockedCount == 1, "마을 사람에게 부딪히면 막힌 이동만 알린다.");
        Check(!play.IsTalking, "부딪혀서는 말을 걸지 않는다.");
        play.SelectTile(1, 0);
        play.SelectTile(1, 0);
        play.StepTravel();
        Check(!play.IsTalking, "마을 사람을 왼쪽 클릭해서는 말을 걸지 않는다.");

        play.Session.Map.SetProp(1, 0, tables.Prop.Get("tree"));
        Check(!play.Execute(new MoveCommand(EDirection.Right)) && play.Session.Map.GetPropOrNull(1, 0) != null,
            "주민과 나무가 겹치면 부딪히기로 말 걸기 뒤의 채집도 시작하지 않는다.");
    }

    private static void CheckPromiseDialogue(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "villager");
        Actor villager = play.Session.Map.Actors[1];
        long tick = play.Session.World.Tick;
        Talk(play);
        Check(play.IsTalking && play.DialogueSpeakerOrNull == villager, "메뉴에서 말 걸기를 고르면 마을 사람과 대화를 시작한다.");
        Check(play.DialogueLineOrNull!.Text.Contains("슬라임이 늘어서") && play.DialogueLineOrNull.Choices.Count == 2, "마을 사람이 첫 인사를 하고 대답을 기다린다.");
        Check(play.Session.World.Tick > tick, "말을 거는 데 한 차례를 쓴다.");

        tick = play.Session.World.Tick;
        play.Execute(new ChooseCommand(0));
        Check(play.IsTalking && play.DialogueLineOrNull!.Text.Contains("고마워요"), "돕겠다고 하면 마을 사람이 고마워한다.");
        Check(play.Session.Flags.Get(Dialogues.PROMISED_HELP_FLAG) == 1, "돕겠다는 약속이 스토리 플래그로 남는다.");
        play.Execute(new ChooseCommand(0));
        Check(!play.IsTalking && play.DialogueLineOrNull == null, "마지막 대답을 고르면 대화창이 닫힌다.");
        Check(play.Session.World.Tick == tick, "대답을 고르는 동안에는 시간이 흐르지 않는다.");

        Talk(play);
        Check(play.DialogueLineOrNull!.Text.Contains("든든해요"), "약속한 뒤에 다시 말을 걸면 다른 말을 한다.");
    }

    private static void CheckDeclineDialogue(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "villager");
        Talk(play);
        play.Execute(new ChooseCommand(2));
        Check(play.IsTalking, "없는 번호의 대답은 무시하고 계속 기다린다.");
        play.Execute(new ChooseCommand(1));
        Check(!play.IsTalking && play.Session.Flags.Get(Dialogues.PROMISED_HELP_FLAG) == 0, "거절하면 플래그 없이 대화가 끝난다.");
    }

    private static void CheckLeaveDialogue(GameContent content, GameTables tables)
    {
        PlaySession play = CreatePlay(content, tables, "hero", "villager");
        Talk(play);
        long tick = play.Session.World.Tick;
        Check(play.Execute(new WaitCommand()) && !play.IsTalking, "대화 중에 다른 명령을 내리면 대화를 그만두고 그 명령을 따른다.");
        Check(play.Session.World.Tick > tick, "대화를 그만두고 내린 명령은 시간을 쓴다.");
        play.Execute(new ChooseCommand(0));
        Check(play.Session.Flags.Get(Dialogues.PROMISED_HELP_FLAG) == 0, "대화가 끝난 뒤의 대답 번호는 아무것도 하지 않는다.");
    }

    private static void Talk(PlaySession play)
    {
        List<MenuChoice> choices = new List<MenuChoice>();
        play.CollectMenuChoices(1, 0, choices);
        Check(choices.Count == 2 && choices[0].Label == "마을 사람에게 말 걸기", "마을 사람 칸의 첫 항목은 말 걸기다.");
        play.StartMenuChoice(1, 0, choices[0].InteractionOrNull);
        play.StepTravel();
    }
}
