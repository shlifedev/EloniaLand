using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Cysharp.Text;
using Elonia.Simulation;
using ImGuiNET;

namespace Elonia.Playtest;

internal sealed partial class Debugger
{
    private void DrawTimeTab()
    {
        World world = _session.World;
        DrawLabelValue("현재 틱", ZString.Concat(world.Tick));
        DrawLabelValue("직전 입력 이후", ZString.Format("{0}틱 지남, NPC 행동 {1}회",
            world.Tick - _history.LastPlayerActionTick, _history.NpcActionCountSincePlayerAction));
        DrawWrappedMuted("틱마다 속도만큼 에너지가 찹니다. 에너지가 100 이상이면 행동하고, 행동 비용(기본 100)만큼 줄어듭니다. 남은 에너지는 이월됩니다. 같은 틱에 여럿이 준비되면 맵에 추가된 순서대로 행동합니다.");

        ImGui.SeparatorText("다음 차례 예측");
        BuildForecast();
        ImGuiTableFlags flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ScrollY;
        if (ImGui.BeginTable("forecast", 5, flags, new Vector2(0.0f, -1.0f)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("액터");
            ImGui.TableSetupColumn("남은 틱");
            ImGui.TableSetupColumn("에너지");
            ImGui.TableSetupColumn("속도");
            ImGui.TableSetupColumn("행동 방식");
            ImGui.TableHeadersRow();

            for (int index = 0; index < _forecastRows.Count; index++)
            {
                ForecastRow row = _forecastRows[index];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawSelectableActor(row.Actor);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(row.TicksUntilReady == 0 ? "준비됨" : ZString.Concat(row.TicksUntilReady));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ZString.Concat(row.Actor.Energy));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ZString.Concat(row.Actor.Type.Speed));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(GetCurrentBehaviorName(row.Actor));
            }

            ImGui.EndTable();
        }
    }

    private void BuildForecast()
    {
        _forecastRows.Clear();
        IReadOnlyList<Actor> actors = _session.Map.Actors;
        for (int index = 0; index < actors.Count; index++)
        {
            Actor actor = actors[index];
            if (actor.IsAlive)
            {
                _forecastRows.Add(new ForecastRow(actor, index, GetTicksUntilReady(actor)));
            }
        }

        _forecastRows.Sort(CompareForecastRows);
    }

    private static long GetTicksUntilReady(Actor actor)
    {
        long missingEnergy = GameRunner.READY_ENERGY - actor.Energy;
        if (missingEnergy <= 0)
        {
            return 0;
        }

        int speed = actor.Type.Speed;
        return (missingEnergy + speed - 1) / speed;
    }

    private static int CompareForecastRows(ForecastRow left, ForecastRow right)
    {
        int byTicks = left.TicksUntilReady.CompareTo(right.TicksUntilReady);
        return byTicks != 0 ? byTicks : left.Order.CompareTo(right.Order);
    }

    private void DrawActorTab()
    {
        ImGui.Checkbox("플레이어와 가장 가까운 NPC 자동 선택", ref _isFollowingNearest);
        DrawWrappedMuted("맵에서 액터를 클릭해도 선택됩니다(자동 선택은 꺼집니다).");

        ImGuiTableFlags flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ScrollY;
        if (ImGui.BeginTable("actors", 4, flags, new Vector2(0.0f, 150.0f)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("액터");
            ImGui.TableSetupColumn("위치");
            ImGui.TableSetupColumn("체력");
            ImGui.TableSetupColumn("행동 방식");
            ImGui.TableHeadersRow();

            IReadOnlyList<Actor> actors = _session.Map.Actors;
            for (int index = 0; index < actors.Count; index++)
            {
                Actor actor = actors[index];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawSelectableActor(actor);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ZString.Format("({0}, {1})", actor.X, actor.Y));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(actor.IsAlive ? ZString.Concat(actor.Hp, "/", actor.Type.MaxHp) : "사망");
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(GetCurrentBehaviorName(actor));
            }

            ImGui.EndTable();
        }

        if (_selectedActorOrNull == null)
        {
            ImGui.SeparatorText("선택한 액터 없음");
            return;
        }

        DrawInspector(_selectedActorOrNull);
    }

    private void DrawInspector(Actor actor)
    {
        ImGui.SeparatorText(DebugNames.GetActorName(actor));
        if (ImGui.BeginTable("inspector", 2, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("항목", ImGuiTableColumnFlags.WidthFixed, 70.0f);
            ImGui.TableSetupColumn("값");
            DrawRow("상태", actor.IsAlive ? "생존" : "사망");
            DrawRow("위치", ZString.Format("({0}, {1})", actor.X, actor.Y));
            DrawBarRow("체력", (float)actor.Hp / actor.Type.MaxHp, ZString.Concat(actor.Hp, " / ", actor.Type.MaxHp));
            DrawBarRow("에너지", Math.Min(1.0f, (float)actor.Energy / GameRunner.READY_ENERGY),
                ZString.Concat(actor.Energy, " / ", GameRunner.READY_ENERGY));
            DrawRow("속도", ZString.Format("{0} (틱당 에너지)", actor.Type.Speed));
            DrawRow("공격력", ZString.Concat(actor.Type.AttackPower));
            DrawRow("행동 횟수", ZString.Concat(actor.ActionCount));
            DrawRow("골드", ZString.Concat(actor.Gold));
            DrawRow("조종", GetControllerName(actor));
            ImGui.EndTable();
        }

        if (actor.ControllerOrNull is Brain brain)
        {
            DrawBrain(actor, brain);
        }
    }

    private void DrawBrain(Actor actor, Brain brain)
    {
        ImGui.SeparatorText("두뇌 (Brain)");
        DrawWrappedMuted("위에서부터 확인해 처음으로 나설 수 있는 행동 방식이 행동을 고릅니다. 상태는 이 액터의 마지막 차례 기준이고, 거리는 지금 기준입니다.");

        bool bHasTakenTurn = brain.LastActionOrNull != null;
        int currentIndex = brain.CurrentBehaviorOrNull == null ? brain.Behaviors.Count : IndexOf(brain.Behaviors, brain.CurrentBehaviorOrNull);
        if (ImGui.BeginTable("brain", 4, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("순위", ImGuiTableColumnFlags.WidthFixed, 28.0f);
            ImGui.TableSetupColumn("행동 방식", ImGuiTableColumnFlags.WidthFixed, 56.0f);
            ImGui.TableSetupColumn("상태", ImGuiTableColumnFlags.WidthFixed, 64.0f);
            ImGui.TableSetupColumn("조건");
            ImGui.TableHeadersRow();

            for (int index = 0; index < brain.Behaviors.Count; index++)
            {
                IBehavior behavior = brain.Behaviors[index];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ZString.Concat(index + 1));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(DebugNames.GetBehaviorName(behavior));
                ImGui.TableNextColumn();
                if (!bHasTakenTurn)
                {
                    DrawMuted("차례 전");
                }
                else if (index < currentIndex)
                {
                    DrawMuted("조건 안 맞음");
                }
                else if (index == currentIndex)
                {
                    DrawColored("선택됨", GOOD_TEXT);
                }
                else
                {
                    DrawMuted("확인 안 함");
                }

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(GetBehaviorCondition(actor, behavior));
            }

            ImGui.EndTable();
        }

        DrawLabelValue("마지막 행동", brain.LastActionOrNull == null ? "없음" : DebugNames.GetActionName(brain.LastActionOrNull));
    }

    private static string GetBehaviorCondition(Actor actor, IBehavior behavior)
    {
        switch (behavior)
        {
            case FleeBehavior flee:
                return GetRangeCondition(actor, flee.Threat, flee.SenseRange);
            case ChaseBehavior chase:
                return GetRangeCondition(actor, chase.Target, chase.SenseRange);
            case AttackBehavior attack:
                return ZString.Concat(DebugNames.GetActorName(attack.Target), AttackAction.IsInReach(actor, attack.Target) ? " 닿음" : " 안 닿음");
            case WanderBehavior _:
                return "항상";
            default:
                return "";
        }
    }

    private static string GetRangeCondition(Actor actor, Actor target, int senseRange)
    {
        float deltaX = target.X - actor.X;
        float deltaY = target.Y - actor.Y;
        float distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
        return ZString.Format("{0} 거리 {1:0.0} / 범위 {2}", DebugNames.GetActorName(target), distance, senseRange);
    }

    private string GetControllerName(Actor actor)
    {
        if (actor == _session.Player)
        {
            return "플레이어 입력";
        }

        IActorController? controllerOrNull = actor.ControllerOrNull;
        if (controllerOrNull is Brain)
        {
            return "두뇌 (Brain)";
        }

        return controllerOrNull == null ? "없음" : controllerOrNull.GetType().Name;
    }

    private void DrawHistoryTab()
    {
        ImGui.Checkbox("선택한 액터만", ref _isHistoryFilteredBySelection);
        ImGui.SameLine();
        if (ImGui.Button("지우기"))
        {
            _history.Clear();
        }

        DrawWrappedMuted(ZString.Format("실행된 행동을 최근 것부터 보여 줍니다(최대 {0}개 표시). 거부된 행동은 시간을 쓰지 않으므로 남지 않습니다.", MAX_HISTORY_ROW_COUNT));

        ImGuiTableFlags flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ScrollY;
        if (ImGui.BeginTable("history", 4, flags, new Vector2(0.0f, -1.0f)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("틱", ImGuiTableColumnFlags.WidthFixed, 44.0f);
            ImGui.TableSetupColumn("액터", ImGuiTableColumnFlags.WidthFixed, 90.0f);
            ImGui.TableSetupColumn("행동");
            ImGui.TableSetupColumn("비용", ImGuiTableColumnFlags.WidthFixed, 32.0f);
            ImGui.TableHeadersRow();

            IReadOnlyList<TurnHistory.Entry> entries = _history.Entries;
            int rowCount = 0;
            for (int index = entries.Count - 1; index >= 0 && rowCount < MAX_HISTORY_ROW_COUNT; index--)
            {
                TurnHistory.Entry entry = entries[index];
                if (_isHistoryFilteredBySelection && entry.Actor != _selectedActorOrNull)
                {
                    continue;
                }

                rowCount++;
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ZString.Concat(entry.Tick));
                ImGui.TableNextColumn();
                DrawColored(DebugNames.GetActorName(entry.Actor), entry.Actor == _session.Player ? PLAYER_TEXT : MUTED_TEXT);
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(DebugNames.GetActionName(entry.Action));
                ImGui.TableNextColumn();
                ImGui.TextUnformatted(ZString.Concat(entry.Cost));
            }

            ImGui.EndTable();
        }
    }

    private void DrawReproTab()
    {
        DrawWrappedMuted("같은 시드와 같은 입력이면 항상 같은 판이 됩니다. 아래 명령줄로 실행하면 기록된 입력을 순서대로 넣은 뒤 키보드로 이어서 플레이할 수 있습니다. R로 재시작하면 새 시드로 기록을 다시 시작합니다.");
        DrawLabelValue("시드", ZString.Concat(_seed));
        DrawLabelValue("입력 수", ZString.Concat(_commandSymbols.Count));

        ImGui.SeparatorText("입력 기록");
        DrawWrapped(_commandSymbols.Count == 0 ? "(아직 없음)" : GetCommandText());
        DrawWrappedMuted("w a s d 이동(적이 있는 쪽이면 공격), . 대기, e 줍기");

        ImGui.SeparatorText("재현 명령줄");
        string commandLine = GetReproCommandLine();
        DrawWrapped(commandLine);
        if (ImGui.Button("명령줄 복사"))
        {
            ImGui.SetClipboardText(commandLine);
            _reproStatusOrNull = "클립보드에 복사했습니다.";
        }

        ImGui.SameLine();
        if (ImGui.Button("repro.txt로 저장"))
        {
            string path = Path.GetFullPath(REPRO_FILE_NAME);
            File.WriteAllText(path, commandLine + Environment.NewLine);
            _reproStatusOrNull = ZString.Concat("저장했습니다: ", path);
        }

        if (_reproStatusOrNull != null)
        {
            DrawWrappedMuted(_reproStatusOrNull);
        }
    }

    private string GetCommandText()
    {
        if (_commandTextOrNull == null)
        {
            using (Utf16ValueStringBuilder builder = ZString.CreateStringBuilder())
            {
                for (int index = 0; index < _commandSymbols.Count; index++)
                {
                    builder.Append(_commandSymbols[index]);
                }

                _commandTextOrNull = builder.ToString();
            }
        }

        return _commandTextOrNull;
    }

    private string GetReproCommandLine()
    {
        if (_commandSymbols.Count == 0)
        {
            return ZString.Format("dotnet run --project src/Elonia.Playtest -- --seed {0} --debug", _seed);
        }

        return ZString.Format("dotnet run --project src/Elonia.Playtest -- --seed {0} --demo \"{1}\" --debug", _seed, GetCommandText());
    }

    private void DrawDisplayTab()
    {
        ImGui.SeparatorText("맵 위 표시");
        ImGui.Checkbox("행동 방식 이름 (머리 위)", ref _isBehaviorLabelVisible);
        ImGui.Checkbox("에너지 막대 (발밑, 가득 차면 하늘색)", ref _isEnergyBarVisible);
        ImGui.Checkbox("선택한 액터의 감지 범위", ref _isSenseRangeVisible);
        ImGui.Checkbox("모든 액터의 감지 범위", ref _isEverySenseRangeVisible);
        ImGui.Checkbox("선택한 액터와 대상 연결선", ref _isTargetLineVisible);
        ImGui.Checkbox("마우스 아래 칸 정보", ref _isTileInfoVisible);

        ImGui.SeparatorText("색");
        DrawColored("공격", ToVector4(ATTACK_COLOR));
        ImGui.SameLine();
        DrawColored("추격", ToVector4(CHASE_COLOR));
        ImGui.SameLine();
        DrawColored("도망", ToVector4(FLEE_COLOR));
        ImGui.SameLine();
        DrawColored("배회", ToVector4(WANDER_COLOR));
    }
}
