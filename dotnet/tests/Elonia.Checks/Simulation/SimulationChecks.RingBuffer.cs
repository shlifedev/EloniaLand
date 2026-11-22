using System;
using System.Collections.Generic;
using static Elonia.Checks.Checker;

namespace Elonia.Simulation.Checks;

internal static partial class SimulationChecks
{
    private static void CheckRingBuffer()
    {
        CheckThrows<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0), "원형 버퍼의 용량은 0일 수 없다.");
        CheckThrows<ArgumentOutOfRangeException>(() => new RingBuffer<int>(-1), "원형 버퍼의 용량은 음수일 수 없다.");

        RingBuffer<string> buffer = new RingBuffer<string>(1);
        Check(buffer.Capacity == 1 && buffer.Count == 0, "한 칸짜리 원형 버퍼도 처음에는 비어 있다.");
        buffer.Add("처음");
        using (IEnumerator<string> iterator = buffer.GetEnumerator())
        {
            buffer.Add("마지막");
            CheckThrows<InvalidOperationException>(() => iterator.MoveNext(), "덮어쓰기 전에 얻은 열거자로는 순회를 시작할 수 없다.");
            CheckThrows<InvalidOperationException>(() => iterator.Reset(), "덮어쓰기 전에 얻은 열거자는 되감을 수 없다.");
        }
        Check(buffer.Count == 1 && buffer[0] == "마지막", "한 칸짜리 원형 버퍼에는 마지막 값만 남는다.");
    }
}
