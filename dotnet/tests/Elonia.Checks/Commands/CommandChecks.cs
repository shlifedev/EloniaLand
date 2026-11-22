using System;
using System.Collections.Generic;
using static Elonia.Checks.Checker;

namespace Elonia.Commands.Checks;

internal static class CommandChecks
{
    private const int COMMAND_COUNT = 1_000_000;

    public static void RunAll()
    {
        Run(CheckRecordAndReplay);
        Run(CheckCopyAndClear);
        Run(CheckTextRoundTrip);
        Run(CheckTextErrors);
        Run(CheckNames);
        Run(CheckWriterNumbers);
        Run(MeasureAllocations);
    }

    private static void CheckRecordAndReplay()
    {
        Counter counter = new Counter();
        CommandRecord<Counter> record = new CommandRecord<Counter>();
        Execute(record, counter, new AddCommand(5));
        Execute(record, counter, new ResetCommand());
        Execute(record, counter, new AddCommand(3));
        Execute(record, counter, new AddCommand(-1));
        Check(record.Count == 4 && counter.Value == 2, "명령 네 개를 실행하고 기록한다.");

        Counter replayed = new Counter();
        for (int index = 0; index < record.Count; index++)
        {
            record.Execute(index, replayed);
        }

        Check(replayed.Value == counter.Value && string.Join(",", replayed.History) == "5,0,3,2", "기록을 다른 대상에 다시 실행하면 같은 순서로 같은 결과가 된다.");
        Check(!record.Execute(1, replayed), "명령의 실행 결과(bool)를 그대로 돌려준다.");
        Check(Throws<ArgumentOutOfRangeException>(() => record.Execute(4, replayed)), "기록 밖의 번호는 거부한다.");
    }

    private static void CheckCopyAndClear()
    {
        CommandRecord<Counter> source = new CommandRecord<Counter>();
        source.Add(new AddCommand(7));
        source.Add(new ResetCommand());
        CommandRecord<Counter> target = new CommandRecord<Counter>();
        source.CopyTo(1, target);
        source.CopyTo(0, target);
        Check(target.Count == 2 && WriteText(target) == "ra7", "CopyTo는 그 번호의 명령을 대상 기록의 끝에 더한다.");

        target.Clear();
        Check(target.Count == 0 && WriteText(target) == "", "Clear는 모든 명령을 지운다.");
        target.Add(new AddCommand(1));
        Check(target.Count == 1 && WriteText(target) == "a1", "지운 뒤에도 다시 기록할 수 있다.");
    }

    private static void CheckTextRoundTrip()
    {
        CommandTextFormat<Counter> format = CreateFormat();
        CommandRecord<Counter> record = format.Parse(" a12 r a-3  ab a10 ");
        Check(record.Count == 5 && WriteText(record) == "a12ra-3aba10", "공백을 건너뛰고 읽은 기록을 같은 글자로 쓴다.");

        Counter counter = new Counter();
        for (int index = 0; index < record.Count; index++)
        {
            record.Execute(index, counter);
        }

        Check(counter.IsAborted && counter.Value == 7, "실패한 명령이 옮긴 위치를 되돌려 다음 명령 타입이 읽는다.");
    }

    private static void CheckTextErrors()
    {
        CommandTextFormat<Counter> format = CreateFormat();
        Check(format.Parse("").Count == 0 && format.Parse("   ").Count == 0, "빈 문자열은 빈 기록이다.");
        Check(Throws<FormatException>(() => format.Parse("a1x")), "모르는 글자는 FormatException이다.");
        Check(Throws<FormatException>(() => format.Parse("a")), "숫자가 빠진 명령은 FormatException이다.");
        Check(Throws<FormatException>(() => format.Parse("a2147483648")), "int 범위를 넘는 숫자는 읽지 않는다.");
        Check(Throws<ArgumentNullException>(() => format.Parse(null!)), "null 문자열은 거부한다.");
    }

    private static void CheckNames()
    {
        CommandTextWriter writer = new CommandTextWriter(2);
        writer.Write("pick_up");
        writer.Write(',');
        writer.Write("Attack2");
        Check(writer.ToString() == "pick_up,Attack2", "이름을 그대로 이어 쓴다.");

        CommandTextReader reader = new CommandTextReader("pick_up,]");
        Check(reader.TryReadName(out string name) && name == "pick_up" && reader.Position == 7, "영문, 숫자, '_'까지 이름으로 읽는다.");
        Check(reader.TryRead(',') && !reader.TryReadName(out _) && reader.Position == 8, "이름 글자가 없으면 읽지 않고 위치를 그대로 둔다.");
    }

    private static void CheckWriterNumbers()
    {
        CommandTextWriter writer = new CommandTextWriter(1);
        writer.Write(0);
        writer.Write(',');
        writer.Write(-45);
        writer.Write(',');
        writer.Write(int.MinValue);
        writer.Write(',');
        writer.Write(int.MaxValue);
        Check(writer.ToString() == "0,-45,-2147483648,2147483647", "정수를 10진수로 쓰고 버퍼가 모자라면 늘린다.");

        CommandTextReader reader = new CommandTextReader("-2147483648");
        Check(reader.TryReadInt(out int value) && value == int.MinValue && reader.IsEnd, "int의 최솟값을 읽는다.");
    }

    private static void MeasureAllocations()
    {
        Counter counter = new Counter(bKeepHistory: false);
        CommandRecord<Counter> record = new CommandRecord<Counter>(COMMAND_COUNT);
        for (int index = 0; index < 2; index++)
        {
            AddAlternating(record, index);
        }

        for (int index = 0; index < record.Count; index++)
        {
            record.Execute(index, counter);
        }

        record.Clear();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < COMMAND_COUNT; index++)
        {
            AddAlternating(record, index);
            record.Execute(index, counter);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Check(allocatedBytes == 0, $"데운 뒤 명령의 기록과 실행은 할당하지 않는다. 할당: {allocatedBytes} B");
        Console.WriteLine($"명령 기록·실행 할당: {allocatedBytes} B / {COMMAND_COUNT}회");
    }

    private static void AddAlternating(CommandRecord<Counter> record, int index)
    {
        if (index % 2 == 0)
        {
            record.Add(new AddCommand(1));
        }
        else
        {
            record.Add(new ResetCommand());
        }
    }

    private static void Execute<TCommand>(CommandRecord<Counter> record, Counter counter, in TCommand command) where TCommand : struct, ICommand<Counter>
    {
        record.Add(command);
        command.Execute(counter);
    }

    private static CommandTextFormat<Counter> CreateFormat()
    {
        return new CommandTextFormat<Counter>()
            .Add<AddCommand>()
            .Add<ResetCommand>()
            .Add<AbortCommand>();
    }

    private static string WriteText(CommandRecord<Counter> record)
    {
        CommandTextWriter writer = new CommandTextWriter();
        record.WriteTo(writer);
        return writer.ToString();
    }

    private static bool Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return true;
        }

        return false;
    }

    private sealed class Counter
    {
        private readonly bool _bKeepHistory;

        public int Value { get; set; }
        public bool IsAborted { get; set; }
        public List<int> History { get; } = new List<int>();

        public Counter(bool bKeepHistory = true)
        {
            _bKeepHistory = bKeepHistory;
        }

        public void Record()
        {
            if (_bKeepHistory)
            {
                History.Add(Value);
            }
        }
    }

    private readonly struct AddCommand : ICommand<Counter>, ICommandReader<AddCommand>
    {
        public int Amount { get; }

        public AddCommand(int amount)
        {
            Amount = amount;
        }

        public bool Execute(Counter context)
        {
            context.Value += Amount;
            context.Record();
            return true;
        }

        public void Write(CommandTextWriter writer)
        {
            writer.Write('a');
            writer.Write(Amount);
        }

        public bool TryRead(CommandTextReader reader, out AddCommand command)
        {
            command = default;
            if (!reader.TryRead('a') || !reader.TryReadInt(out int amount))
            {
                return false;
            }

            command = new AddCommand(amount);
            return true;
        }
    }

    private readonly struct ResetCommand : ICommand<Counter>, ICommandReader<ResetCommand>
    {
        public bool Execute(Counter context)
        {
            context.Value = 0;
            context.Record();
            return false;
        }

        public void Write(CommandTextWriter writer)
        {
            writer.Write('r');
        }

        public bool TryRead(CommandTextReader reader, out ResetCommand command)
        {
            command = default;
            return reader.TryRead('r');
        }
    }

    private readonly struct AbortCommand : ICommand<Counter>, ICommandReader<AbortCommand>
    {
        public bool Execute(Counter context)
        {
            context.IsAborted = true;
            return true;
        }

        public void Write(CommandTextWriter writer)
        {
            writer.Write('a');
            writer.Write('b');
        }

        public bool TryRead(CommandTextReader reader, out AbortCommand command)
        {
            command = default;
            return reader.TryRead('a') && reader.TryRead('b');
        }
    }
}
