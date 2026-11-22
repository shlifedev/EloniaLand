using System;

namespace Elonia.Events.Checks;

internal readonly record struct SampleEvent(int Value);
internal readonly record struct OtherEvent(int Value);
internal readonly record struct UnsubscribedEvent(int Value);

internal class RecordingListener<TMessage> : IEventListener<TMessage> where TMessage : struct
{
    public int ReceiveCount { get; private set; }
    public TMessage LastMessage { get; private set; }
    public Action? OnReceivedOrNull { get; set; }

    public void OnEvent(TMessage message)
    {
        ReceiveCount++;
        LastMessage = message;
        OnReceivedOrNull?.Invoke();
    }
}

internal sealed class ValueEqualListener : RecordingListener<SampleEvent>
{
    public override bool Equals(object? otherOrNull)
    {
        return otherOrNull is ValueEqualListener;
    }

    public override int GetHashCode()
    {
        return 0;
    }
}

internal sealed class MultiListener :
    IEventListener<SampleEvent>, IEventListener<OtherEvent>,
    IEventListener<int>, IEventListener<long>, IEventListener<short>, IEventListener<byte>,
    IEventListener<sbyte>, IEventListener<ushort>, IEventListener<uint>, IEventListener<ulong>,
    IEventListener<float>, IEventListener<double>
{
    public int ReceiveCount { get; private set; }

    public void OnEvent(SampleEvent message)
    {
        ReceiveCount++;
    }

    public void OnEvent(OtherEvent message)
    {
        ReceiveCount++;
    }

    public void OnEvent(int message)
    {
        ReceiveCount++;
    }

    public void OnEvent(long message)
    {
        ReceiveCount++;
    }

    public void OnEvent(short message)
    {
        ReceiveCount++;
    }

    public void OnEvent(byte message)
    {
        ReceiveCount++;
    }

    public void OnEvent(sbyte message)
    {
        ReceiveCount++;
    }

    public void OnEvent(ushort message)
    {
        ReceiveCount++;
    }

    public void OnEvent(uint message)
    {
        ReceiveCount++;
    }

    public void OnEvent(ulong message)
    {
        ReceiveCount++;
    }

    public void OnEvent(float message)
    {
        ReceiveCount++;
    }

    public void OnEvent(double message)
    {
        ReceiveCount++;
    }
}
