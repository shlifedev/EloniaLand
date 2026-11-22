using System;
using System.IO;
using System.Security.Cryptography;
using Cysharp.Text;
using MemoryPack;

namespace Elonia.Game
{
    public enum ESaveSlot
    {
        Quick,
        Manual1,
        Manual2,
        Manual3
    }

    public readonly struct SaveSlotInfo
    {
        public DateTime SavedAtUtc { get; }
        public long Tick { get; }
        public int PlayerHp { get; }

        internal SaveSlotInfo(DateTime savedAtUtc, long tick, int playerHp)
        {
            SavedAtUtc = savedAtUtc;
            Tick = tick;
            PlayerHp = playerHp;
        }
    }

    public sealed class SaveSlotStore
    {
        private const uint MAGIC = 0x534E4C45;
        private const int FORMAT_VERSION = 1;
        private const int HASH_SIZE = 32;
        private const int MAX_PAYLOAD_BYTES = 64 * 1024 * 1024;
        private readonly string _directory;
        private readonly Predicate<GameSnapshot> _canRestore;

        public SaveSlotStore(string directory, Predicate<GameSnapshot> canRestore)
        {
            _directory = Path.GetFullPath(directory);
            _canRestore = canRestore ?? throw new ArgumentNullException(nameof(canRestore));
        }

        public bool Exists(ESaveSlot slot, bool bBackup = false)
        {
            return File.Exists(GetPath(slot, bBackup));
        }

        public SaveSlotInfo ReadInfo(ESaveSlot slot, bool bBackup = false)
        {
            using BinaryReader reader = Open(GetPath(slot, bBackup));
            return ReadHeader(reader, out _);
        }

        public GameSnapshot Read(ESaveSlot slot, bool bBackup = false)
        {
            using BinaryReader reader = Open(GetPath(slot, bBackup));
            SaveSlotInfo info = ReadHeader(reader, out int length);
            byte[] hash = reader.ReadBytes(HASH_SIZE);
            byte[] payload = reader.ReadBytes(length);
            using SHA256 sha = SHA256.Create();
            if (!sha.ComputeHash(payload).AsSpan().SequenceEqual(hash))
            {
                throw new InvalidDataException("저장 파일의 내용이 손상되었습니다.");
            }

            GameSnapshot? snapshot = null;
            int consumed;
            try
            {
                consumed = MemoryPackSerializer.Deserialize<GameSnapshot>(payload, ref snapshot);
            }
            catch (Exception exception) when (exception is MemoryPackSerializationException || exception is ArgumentException ||
                exception is OverflowException)
            {
                throw new InvalidDataException("저장 데이터를 읽을 수 없습니다.", exception);
            }

            if (snapshot == null || snapshot.Player == null || consumed != length || snapshot.Tick != info.Tick || snapshot.PlayerHp != info.PlayerHp)
            {
                throw new InvalidDataException("저장 파일의 슬롯 정보가 게임 데이터와 일치하지 않습니다.");
            }

            return snapshot;
        }

        public void Write(ESaveSlot slot, GameSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            string path = GetPath(slot, false);
            byte[] payload = MemoryPackSerializer.Serialize(snapshot);
            if (payload.Length > MAX_PAYLOAD_BYTES)
            {
                throw new InvalidDataException("저장 데이터가 슬롯의 최대 크기를 초과했습니다.");
            }

            Directory.CreateDirectory(_directory);
            using FileStream slotLock = new FileStream(Path.ChangeExtension(path, ".lock"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
            string temporary = Path.ChangeExtension(path, ".tmp");
            try
            {
                using (FileStream stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                using (BinaryWriter writer = new BinaryWriter(stream))
                using (SHA256 sha = SHA256.Create())
                {
                    writer.Write(MAGIC);
                    writer.Write(FORMAT_VERSION);
                    writer.Write(DateTime.UtcNow.Ticks);
                    writer.Write(snapshot.Tick);
                    writer.Write(snapshot.PlayerHp);
                    writer.Write(payload.Length);
                    writer.Write(sha.ComputeHash(payload));
                    writer.Write(payload);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(path))
                {
                    string? backupPathOrNull = CanBackUp(slot) ? GetPath(slot, true) : null;
                    File.Replace(temporary, path, backupPathOrNull);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        private bool CanBackUp(ESaveSlot slot)
        {
            try
            {
                return _canRestore(Read(slot));
            }
            catch (InvalidDataException)
            {
                return false;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        private static BinaryReader Open(string path)
        {
            return new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));
        }

        private static SaveSlotInfo ReadHeader(BinaryReader reader, out int length)
        {
            if (reader.ReadUInt32() != MAGIC)
            {
                throw new InvalidDataException("Elonia 저장 파일의 표식을 찾을 수 없습니다.");
            }

            int version = reader.ReadInt32();
            if (version != FORMAT_VERSION)
            {
                throw new NotSupportedException($"지원하지 않는 세이브 형식입니다 (버전 {version}).");
            }

            long utcTicks = reader.ReadInt64();
            long tick = reader.ReadInt64();
            int hp = reader.ReadInt32();
            length = reader.ReadInt32();
            if (utcTicks < 0 || utcTicks > DateTime.MaxValue.Ticks || tick < 0 || hp < 0 ||
                length <= 0 || length > MAX_PAYLOAD_BYTES || reader.BaseStream.Length - reader.BaseStream.Position != HASH_SIZE + length)
            {
                throw new InvalidDataException("저장 파일의 길이 또는 슬롯 정보가 올바르지 않습니다.");
            }

            return new SaveSlotInfo(new DateTime(utcTicks, DateTimeKind.Utc), tick, hp);
        }

        private string GetPath(ESaveSlot slot, bool bBackup)
        {
            string name = slot switch
            {
                ESaveSlot.Quick => "quick",
                ESaveSlot.Manual1 => "slot1",
                ESaveSlot.Manual2 => "slot2",
                ESaveSlot.Manual3 => "slot3",
                _ => throw new ArgumentOutOfRangeException(nameof(slot))
            };
            return Path.Combine(_directory, ZString.Concat(name, bBackup ? ".bak" : ".sav"));
        }
    }
}
