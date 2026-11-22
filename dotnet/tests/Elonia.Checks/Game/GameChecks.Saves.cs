using System;
using System.IO;
using Elonia.Data;
using Elonia.Simulation;
using MemoryPack;
using static Elonia.Checks.Checker;

namespace Elonia.Game.Checks;

internal static partial class GameChecks
{
    private static void CheckSaves(GameContent content)
    {
        string directory = Path.Combine(Path.GetTempPath(), "EloniaChecks", Guid.NewGuid().ToString("N"));
        SaveSlotStore store = CreateStore(directory, content);
        try
        {
            CheckSlotsAndRecovery(store, directory, content);
            CheckSavedTown(store, content);
            CheckSavedDialogue(store, content);
            CheckSavedHarvest(store, content);
            CheckReleasedSave(content);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static SaveSlotStore CreateStore(string directory, GameContent content)
    {
        return new SaveSlotStore(directory, snapshot => PlaySession.CanRestore(content, snapshot));
    }

    private static void CheckSlotsAndRecovery(SaveSlotStore store, string directory, GameContent content)
    {
        PlaySession play = PlaySession.CreateTown(content, 501);
        ESaveSlot[] slots = { ESaveSlot.Quick, ESaveSlot.Manual1, ESaveSlot.Manual2, ESaveSlot.Manual3 };
        long[] ticks = new long[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            Check(!store.Exists(slots[i]), $"슬롯 {slots[i]}은 처음에 비어 있다.");
            ticks[i] = play.Session.World.Tick;
            store.Write(slots[i], play.CaptureSnapshot());
            play.Execute(new WaitCommand());
        }

        store.Write(ESaveSlot.Quick, play.CaptureSnapshot());
        SaveSlotStore reopened = CreateStore(directory, content);
        SaveSlotInfo info = reopened.ReadInfo(ESaveSlot.Quick);
        CheckEqual(play.Session.World.Tick, info.Tick, "빠른저장은 전용 슬롯을 갱신한다.");
        Check(info.SavedAtUtc.Kind == DateTimeKind.Utc && info.PlayerHp == play.Session.Player.Hp, "슬롯 목록에 UTC 시각과 체력이 있다.");
        CheckEqual(ticks[0], reopened.Read(ESaveSlot.Quick, true).Tick, "빠른저장 전의 정상본이 백업으로 남는다.");
        for (int i = 1; i < slots.Length; i++)
        {
            CheckEqual(ticks[i], reopened.Read(slots[i]).Tick, $"빠른저장은 수동 슬롯 {i}의 시점을 보존한다.");
        }

        string quickPath = Path.Combine(directory, "quick.sav");
        byte[] valid = File.ReadAllBytes(quickPath);
        byte[] corrupt = (byte[])valid.Clone();
        corrupt[corrupt.Length - 1] ^= 1;
        File.WriteAllBytes(quickPath, corrupt);
        CheckThrows<InvalidDataException>(() => reopened.Read(ESaveSlot.Quick), "내용의 한 바이트가 바뀌어도 오류로 처리한다.");
        reopened.Write(ESaveSlot.Quick, play.CaptureSnapshot());
        CheckEqual(ticks[0], reopened.Read(ESaveSlot.Quick, true).Tick, "손상된 본파일을 교체해도 이전 정상 백업이 남는다.");

        corrupt = (byte[])valid.Clone();
        corrupt[4] = 99;
        File.WriteAllBytes(quickPath, corrupt);
        CheckThrows<NotSupportedException>(() => reopened.Read(ESaveSlot.Quick), "지원하지 않는 형식을 구분한다.");
        CheckThrows<NotSupportedException>(() => reopened.Write(ESaveSlot.Quick, play.CaptureSnapshot()), "미래 버전의 저장 파일을 덮지 않는다.");
        PlaySession recovered = PlaySession.Restore(content, reopened.Read(ESaveSlot.Quick, true));
        CheckThrows<NotSupportedException>(() => reopened.Write(ESaveSlot.Quick, recovered.CaptureSnapshot()), "백업으로 복구한 뒤에도 미래 버전 원본은 보존한다.");
        Check(File.ReadAllBytes(quickPath).AsSpan().SequenceEqual(corrupt), "실패한 쓰기는 슬롯 원본을 보존한다.");

        GameSnapshot retired = play.CaptureSnapshot();
        retired.Map.FloorIds[0] = "retired_floor";
        reopened.Write(ESaveSlot.Manual2, retired);
        CheckThrows<InvalidDataException>(() => PlaySession.Restore(content, reopened.Read(ESaveSlot.Manual2)), "형식이 맞아도 필요한 지형이 없으면 복원하지 않는다.");
        reopened.Write(ESaveSlot.Manual2, play.CaptureSnapshot());
        CheckEqual(ticks[2], reopened.Read(ESaveSlot.Manual2, true).Tick, "복원할 수 없는 본파일이 마지막 정상 백업을 밀어내지 않는다.");

        using FileStream held = File.Open(Path.Combine(directory, "slot1.lock"), FileMode.Open, FileAccess.Write, FileShare.None);
        CheckThrows<IOException>(() => reopened.Write(ESaveSlot.Manual1, play.CaptureSnapshot()), "동시 쓰기는 안전하게 거절한다.");
        CheckEqual(ticks[1], reopened.Read(ESaveSlot.Manual1).Tick, "실패한 덮어쓰기 뒤에도 기존 슬롯을 읽는다.");
    }

    private static void CheckSavedTown(SaveSlotStore store, GameContent content)
    {
        PlaySession original = PlaySession.CreateTown(content, 501);
        Actor player = original.Session.Player;
        player.IsInvincible = true;
        player.AddGold(43);
        player.SetStatModifier(ActorStats.CHEAT_SOURCE, EStat.Attack, 2);
        player.SetStatModifier("ring", EStat.Speed, 3);
        original.MarkCheatUsed();
        for (int i = 0; i < 8; i++)
        {
            original.Execute(new WaitCommand());
        }

        store.Write(ESaveSlot.Manual1, original.CaptureSnapshot());
        GameSnapshot saved = store.Read(ESaveSlot.Manual1);
        PlaySession loaded = PlaySession.Restore(content, saved);
        Check(loaded.IsFromSave && loaded.HasUsedCheats, "복원된 판과 치트 사용 기록을 구별한다.");
        Check(loaded.Session.Player.Gold == 43 && loaded.Session.Player.Stat(EStat.Attack) == player.Stat(EStat.Attack),
            "골드와 능력치 수정치를 보존한다.");
        CheckEqual(original.Session.World.Tick, loaded.Session.World.Tick, "복원 자체는 시간을 진행시키지 않는다.");
        for (int i = 0; i < 25; i++)
        {
            original.Execute(new WaitCommand());
            loaded.Execute(new WaitCommand());
        }

        byte[] before = MemoryPackSerializer.Serialize(original.CaptureSnapshot());
        byte[] after = MemoryPackSerializer.Serialize(loaded.CaptureSnapshot());
        Check(before.AsSpan().SequenceEqual(after), "같은 25번의 입력 후 NPC, 난수, 에너지, 다음 ID를 포함한 전체 상태가 같다.");
        loaded.Session.Player.RemoveStatModifiers(ActorStats.CHEAT_SOURCE);
        CheckEqual(player.Stat(EStat.Attack) - 2, loaded.Session.Player.Stat(EStat.Attack), "불러온 뒤 같은 출처의 기존 수정치를 해제할 수 있다.");

        loaded.Session.Player.RemoveStatModifiers("ring");
        Check(loaded.Session.Player.Speed == player.Speed - 3 && loaded.Session.Player.GameStats().Modifiers.Count == 0,
            "치트가 아닌 출처도 불러온 뒤 같은 이름으로 뗄 수 있다.");

        saved.Map.LegendIds[0] = "missing_after_update";
        CheckThrows<InvalidDataException>(() => PlaySession.Restore(content, saved), "사라진 표 ID를 조용히 다른 지형으로 바꾸지 않는다.");
    }

    private static void CheckSavedDialogue(SaveSlotStore store, GameContent content)
    {
        PlaySession play = CreatePlay(content, content.Tables, "hero", "villager");
        Talk(play);
        play.Execute(new ChooseCommand(0));
        store.Write(ESaveSlot.Manual2, play.CaptureSnapshot());
        PlaySession loaded = PlaySession.Restore(content, store.Read(ESaveSlot.Manual2));
        Check(loaded.DialogueSpeakerOrNull == loaded.Session.Map.Actors[1] && loaded.DialogueLineOrNull == play.DialogueLineOrNull,
            "선택한 대사와 상대가 새 세션에 이어진다.");
        CheckEqual(1, loaded.Session.Flags.Get(Dialogues.PROMISED_HELP_FLAG), "틱이 흐르지 않은 대답도 저장한다.");
        loaded.Execute(new ChooseCommand(0));
        Talk(loaded);
        Check(loaded.DialogueLineOrNull!.Text.Contains("든든해요"), "불러온 뒤 약속을 기억한 주민과 다시 대화한다.");
    }

    private static void CheckSavedHarvest(SaveSlotStore store, GameContent content)
    {
        PlaySession play = CreatePlay(content, content.Tables, "hero", "tree", "grass");
        play.Execute(new MoveCommand(EDirection.Right));
        play.Execute(new MoveCommand(EDirection.Right));
        play.Execute(new PickUpCommand());
        int nextId = play.Session.Map.NextEntityID;
        play.SelectTile(2, 0);
        play.SelectTile(2, 0);
        store.Write(ESaveSlot.Manual3, play.CaptureSnapshot());
        PlaySession loaded = PlaySession.Restore(content, store.Read(ESaveSlot.Manual3));
        Check(loaded.Session.Map.GetPropOrNull(1, 0) == null && loaded.Session.GetLegend(1, 0).Id == "tree",
            "나무를 벤 칸과 원래 범례를 함께 복원한다.");
        Check(loaded.Session.Player.Inventory.Stacks.Count == 1 && loaded.Session.Player.Inventory.Stacks[0].Count == content.Tables.Prop.Get("tree").DropAmount,
            "주운 재료는 인벤토리에 남는다.");
        CheckEqual(nextId, loaded.Session.Map.CreateEntityID(), "마지막으로 주운 재료의 개체 ID를 다시 발급하지 않는다.");
        Check(loaded.Session.Player.X == 1 && !loaded.IsTraveling && loaded.TravelPathOrNull == null,
            "자동 이동 중에는 마지막으로 도착한 칸에서 수동 입력을 기다린다.");
    }

    private static void CheckReleasedSave(GameContent content)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "SaveFixtures", "v1");
        GameSnapshot saved = CreateStore(directory, content).Read(ESaveSlot.Manual1);
        PlaySession loaded = PlaySession.Restore(content, saved);
        Check(loaded.Seed == 1 && loaded.Session.Map.Width == 2 && loaded.Session.Flags.Get(Dialogues.PROMISED_HELP_FLAG) == 1 && loaded.IsTalking,
            "v1 저장 파일을 읽어 같은 마을과 약속, 열린 대사를 복원한다.");
        long tick = loaded.Session.World.Tick;
        loaded.Execute(new WaitCommand());
        Check(loaded.Session.World.Tick > tick, "v1 세이브에서 다음 차례를 진행한다.");

        Actor cheated = PlaySession.Restore(content, CreateStore(directory, content).Read(ESaveSlot.Manual2)).Session.Player;
        CheckEqual(2, cheated.GameStats().GetModifier(ActorStats.CHEAT_SOURCE, EStat.Attack), "v1 세이브의 치트 수정치를 치트 출처로 복원한다.");
        cheated.RemoveStatModifiers(ActorStats.CHEAT_SOURCE);
        CheckEqual(cheated.Row().Attack, cheated.Stat(EStat.Attack), "v1 세이브에서 복원한 치트 수정치를 뗄 수 있다.");
    }
}
