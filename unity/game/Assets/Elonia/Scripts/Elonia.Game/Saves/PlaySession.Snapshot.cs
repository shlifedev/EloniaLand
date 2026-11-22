using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Text;
using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed partial class PlaySession
    {
        private const string TOWN_MAP_ID = "town";

        private const int MAX_MAP_CELLS = 512 * 512;
        private const int MAX_MAP_ENTITIES = 8192;
        private const int MAX_STORY_FLAGS = 16384;

        public GameSnapshot CaptureSnapshot()
        {
            Dictionary<string, int> flags = Session.Flags.CaptureValues();
            CheckSizeLimits((long)Session.Map.Width * Session.Map.Height, (long)Session.Map.Actors.Count + Session.Map.Things.Count, flags.Count);
            return new GameSnapshot
            {
                Seed = Seed,
                Tick = Session.World.Tick,
                RandomState = Session.World.Random.State,
                Player = CaptureActor(Session.Player),
                Map = CaptureMap(),
                Flags = flags,
                DialogueSpeakerID = _dialogueSpeakerOrNull?.ID ?? 0,
                DialogueLineIdOrNull = _dialogueLineOrNull?.Id,
                HasUsedCheats = HasUsedCheats
            };
        }

        private MapSnapshot CaptureMap()
        {
            Map map = Session.Map;
            int cellCount = checked(map.Width * map.Height);
            MapSnapshot saved = new MapSnapshot
            {
                Id = TOWN_MAP_ID,
                Width = map.Width,
                Height = map.Height,
                LegendIds = new string[cellCount],
                FloorIds = new string[cellCount],
                PropIds = new string?[cellCount],
                Npcs = new ActorSnapshot[map.Actors.Count - 1],
                Things = new ThingSnapshot[map.Things.Count],
                TurnIndex = Session.Runner.TurnIndex,
                NextEntityID = map.NextEntityID
            };

            for (int index = 0; index < cellCount; index++)
            {
                int x = index % map.Width;
                int y = index / map.Width;
                Cell cell = map.GetCell(x, y);
                saved.LegendIds[index] = Session.GetLegend(x, y).Id;
                saved.FloorIds[index] = cell.Floor.Id;
                saved.PropIds[index] = cell.PropOrNull?.Id;
            }

            int npcIndex = 0;
            for (int index = 0; index < map.Actors.Count; index++)
            {
                Actor actor = map.Actors[index];
                if (actor == Session.Player)
                {
                    saved.PlayerIndex = index;
                }
                else
                {
                    saved.Npcs[npcIndex++] = CaptureActor(actor);
                }
            }

            for (int index = 0; index < map.Things.Count; index++)
            {
                Thing thing = map.Things[index];
                saved.Things[index] = new ThingSnapshot
                {
                    ID = thing.ID,
                    RowId = thing.Def.Id,
                    X = thing.X,
                    Y = thing.Y,
                    Count = thing.Count
                };
            }

            return saved;
        }

        private ActorSnapshot CaptureActor(Actor actor)
        {
            ActorStats stats = actor.GameStats();
            IReadOnlyList<ItemStack> stacks = actor.Inventory.Stacks;
            ActorSnapshot saved = new ActorSnapshot
            {
                ID = actor.ID,
                RowId = actor.Def.Id,
                X = actor.X,
                Y = actor.Y,
                Hp = actor.Hp,
                Gold = actor.Gold,
                ActionCount = actor.ActionCount,
                Energy = actor.Energy,
                IsInvincible = actor.IsInvincible,
                Inventory = new StackSnapshot[stacks.Count],
                Modifiers = new ModifierSnapshot[stats.Modifiers.Count],
                BehaviorNameOrNull = (actor.ControllerOrNull as Brain)?.CurrentBehaviorOrNull?.Name,
                WasAlert = _alertActors.Contains(actor)
            };
            for (int index = 0; index < stacks.Count; index++)
            {
                ItemStack stack = stacks[index];
                saved.Inventory[index] = new StackSnapshot { RowId = stack.Def.Id, Count = stack.Count };
            }

            for (int index = 0; index < stats.Modifiers.Count; index++)
            {
                StatModifier modifier = stats.Modifiers[index];
                saved.Modifiers[index] = new ModifierSnapshot { SourceOrNull = modifier.Source, Stat = modifier.Stat, Amount = modifier.Amount };
            }

            return saved;
        }

        public static bool CanRestore(GameContent content, GameSnapshot snapshot)
        {
            try
            {
                Restore(content, snapshot);
                return true;
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }

        public static PlaySession Restore(GameContent content, GameSnapshot snapshot)
        {
            if (content == null || snapshot == null)
            {
                throw new ArgumentNullException(content == null ? nameof(content) : nameof(snapshot));
            }

            try
            {
                return RestoreSession(content.Tables, snapshot);
            }
            catch (KeyNotFoundException error)
            {
                throw new InvalidDataException($"세이브가 참조하는 게임 데이터를 찾을 수 없습니다. {error.Message}", error);
            }
            catch (Exception error) when (error is ArgumentException || error is OverflowException || error is InvalidOperationException)
            {
                throw new InvalidDataException($"세이브의 월드 상태가 올바르지 않습니다. {error.Message}", error);
            }
        }

        private static PlaySession RestoreSession(GameTables tables, GameSnapshot snapshot)
        {
            if (HasMissingPart(snapshot) || !HasValidLayout(snapshot.Map))
            {
                throw new InvalidDataException("세이브의 지도 크기나 배치가 올바르지 않습니다.");
            }

            MapSnapshot saved = snapshot.Map;
            CheckSizeLimits(saved.LegendIds.Length, (long)saved.Npcs.Length + 1 + saved.Things.Length, snapshot.Flags.Count);
            Map map = new Map(saved.Width, saved.Height);
            TownLegendRow[,] layout = RestoreTerrain(tables, saved, map);
            Actor player = RestoreActor(tables, snapshot.Player, null);
            List<Actor> alertActors = RestoreActors(tables, snapshot, map, player);
            RestoreThings(tables, saved, map);
            map.RestoreNextEntityID(saved.NextEntityID);

            World world = World.Restore(map, tables, snapshot.Tick, snapshot.RandomState, snapshot.Seed);
            GameRunner runner = GameRunner.Restore(world, player, saved.TurnIndex);
            PlaySession play = new PlaySession(new GameSession(runner, layout))
            {
                IsFromSave = true,
                HasUsedCheats = snapshot.HasUsedCheats
            };

            play._alertActors.UnionWith(alertActors);
            foreach (KeyValuePair<string, int> pair in snapshot.Flags)
            {
                play.Session.Flags.Set(pair.Key, pair.Value);
            }

            play.RestoreDialogue(snapshot);
            return play;
        }

        private static bool HasMissingPart(GameSnapshot snapshot)
        {
            MapSnapshot saved = snapshot.Map;
            return saved == null || snapshot.Player == null || snapshot.Flags == null || saved.LegendIds == null ||
                saved.FloorIds == null || saved.PropIds == null || saved.Npcs == null || saved.Things == null;
        }

        private static bool HasValidLayout(MapSnapshot saved)
        {
            int cellCount = saved.LegendIds.Length;
            return saved.Id == TOWN_MAP_ID && saved.Width > 0 && saved.Height > 0 && (long)saved.Width * saved.Height == cellCount &&
                saved.FloorIds.Length == cellCount && saved.PropIds.Length == cellCount &&
                saved.PlayerIndex >= 0 && saved.PlayerIndex <= saved.Npcs.Length;
        }

        private static void CheckSizeLimits(long cells, long entities, int flags)
        {
            if (cells > MAX_MAP_CELLS || entities > MAX_MAP_ENTITIES || flags > MAX_STORY_FLAGS)
            {
                throw new InvalidDataException("현재 세이브 형식에서 지원하는 지역 크기, 개체 수 또는 스토리 플래그 수를 초과했습니다.");
            }
        }

        private static TownLegendRow[,] RestoreTerrain(GameTables tables, MapSnapshot saved, Map map)
        {
            TownLegendRow[,] layout = new TownLegendRow[saved.Height, saved.Width];
            for (int index = 0; index < saved.LegendIds.Length; index++)
            {
                int x = index % saved.Width;
                int y = index / saved.Width;
                layout[y, x] = tables.TownLegend.Get(saved.LegendIds[index]);
                map.SetFloor(x, y, tables.Floor.Get(saved.FloorIds[index]));
                string? propId = saved.PropIds[index];
                if (propId != null)
                {
                    map.SetProp(x, y, tables.Prop.Get(propId));
                }
            }

            return layout;
        }

        private static List<Actor> RestoreActors(GameTables tables, GameSnapshot snapshot, Map map, Actor player)
        {
            MapSnapshot saved = snapshot.Map;
            List<Actor> alertActors = new List<Actor>();
            int npcIndex = 0;
            for (int index = 0; index <= saved.Npcs.Length; index++)
            {
                bool bPlayer = index == saved.PlayerIndex;
                ActorSnapshot actorState = bPlayer ? snapshot.Player : saved.Npcs[npcIndex++];
                Actor actor = bPlayer ? player : RestoreActor(tables, actorState, player);
                map.RestoreEntity(actor);
                if (actorState.WasAlert)
                {
                    alertActors.Add(actor);
                }
            }

            return alertActors;
        }

        private static void RestoreThings(GameTables tables, MapSnapshot saved, Map map)
        {
            foreach (ThingSnapshot thing in saved.Things)
            {
                if (thing == null || thing.ID <= 0 || thing.ID == int.MaxValue)
                {
                    throw new InvalidDataException("세이브에 잘못된 물건 ID가 있습니다.");
                }

                map.RestoreEntity(new Thing(thing.ID, tables.Thing.Get(thing.RowId), thing.X, thing.Y, thing.Count));
            }
        }

        private void RestoreDialogue(GameSnapshot snapshot)
        {
            if (snapshot.DialogueLineIdOrNull == null)
            {
                if (snapshot.DialogueSpeakerID != 0)
                {
                    throw new InvalidDataException("세이브에 대화 내용이 없습니다.");
                }

                return;
            }

            Actor? speakerOrNull = FindActorOrNull(Session.Map, snapshot.DialogueSpeakerID);
            if (speakerOrNull == null || !speakerOrNull.IsAlive || !Dialogues.HasDialogue(speakerOrNull.Row()))
            {
                throw new InvalidDataException("세이브의 대화 상대를 찾을 수 없습니다.");
            }

            ShowLine(speakerOrNull, Dialogues.GetLine(snapshot.DialogueLineIdOrNull));
        }

        private static Actor? FindActorOrNull(Map map, int id)
        {
            for (int index = 0; index < map.Actors.Count; index++)
            {
                if (map.Actors[index].ID == id)
                {
                    return map.Actors[index];
                }
            }

            return null;
        }

        private static Actor RestoreActor(GameTables tables, ActorSnapshot saved, Actor? playerOrNull)
        {
            if (saved == null || saved.ID <= 0 || saved.ID == int.MaxValue || saved.Inventory == null || saved.Modifiers == null)
            {
                throw new InvalidDataException("세이브의 액터 정보가 올바르지 않습니다.");
            }

            ActorRow row = tables.Actor.Get(saved.RowId);
            if ((row.Behavior == EActorBehavior.Player) != (playerOrNull == null))
            {
                throw new InvalidDataException($"세이브의 액터 '{row.Id}'는 플레이어와 NPC 구분이 맞지 않습니다.");
            }

            Brain? brainOrNull = playerOrNull == null ? null : RestoreBrain(row, saved, playerOrNull);
            Actor actor = new Actor(saved.ID, row, RestoreStats(row, saved), saved.X, saved.Y, brainOrNull);
            actor.RestoreState(saved.Hp, saved.Gold, saved.ActionCount, saved.Energy, saved.IsInvincible);
            RestoreInventory(tables, saved, actor);
            return actor;
        }

        private static Brain RestoreBrain(ActorRow row, ActorSnapshot saved, Actor player)
        {
            Brain brain = TownMap.CreateNpcBrain(row, player);
            foreach (IBehavior behavior in brain.Behaviors)
            {
                if (behavior.Name == saved.BehaviorNameOrNull)
                {
                    brain.RestoreCurrentBehavior(behavior);
                    break;
                }
            }

            return brain;
        }

        private static ActorStats RestoreStats(ActorRow row, ActorSnapshot saved)
        {
            ActorStats stats = new ActorStats(row);
            foreach (ModifierSnapshot modifier in saved.Modifiers)
            {
                if (modifier == null || modifier.Amount == 0 || modifier.LegacySourceIndex < 0 || modifier.SourceOrNull == string.Empty)
                {
                    throw new InvalidDataException("세이브의 능력치 수정치가 올바르지 않습니다.");
                }

                string source = modifier.SourceOrNull ?? GetLegacySource(modifier.LegacySourceIndex);
                if (stats.GetModifier(source, modifier.Stat) != 0)
                {
                    throw new InvalidDataException("세이브의 동일한 능력치 수정치가 중복되었습니다.");
                }

                stats.SetModifier(source, modifier.Stat, modifier.Amount);
            }

            return stats;
        }

        private static string GetLegacySource(int sourceIndex)
        {
            return sourceIndex == 0 ? ActorStats.CHEAT_SOURCE : ZString.Concat("legacy.", sourceIndex);
        }

        private static void RestoreInventory(GameTables tables, ActorSnapshot saved, Actor actor)
        {
            foreach (StackSnapshot stack in saved.Inventory)
            {
                if (stack == null)
                {
                    throw new InvalidDataException("세이브에 비어 있는 인벤토리 항목이 있습니다.");
                }

                ThingRow item = tables.Thing.Get(stack.RowId);
                if (actor.Inventory.GetCount(item) != 0)
                {
                    throw new InvalidDataException($"세이브의 인벤토리에 '{stack.RowId}'가 중복되었습니다.");
                }

                actor.Inventory.Add(item, stack.Count);
            }
        }
    }
}
