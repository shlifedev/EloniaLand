using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static Elonia.Checks.Checker;

namespace Elonia.Events.Checks;

internal static class EventChecks
{
    private const int WARMUP_COUNT = 10_000;
    private const int PUBLISH_COUNT = 1_000_000;

    public static void RunAll()
    {
        Run(CheckTypeAndInstanceIsolation);
        Run(CheckOrderAndFilter);
        Run(CheckReferenceIdentity);
        Run(CheckMissingSubscriptionsAndNulls);
        Run(CheckRemovalDuringBroadcast);
        Run(CheckRegistrationDuringBroadcast);
        Run(CheckExceptionsAndReentrancy);
        Run(CheckMultiTypeRegistration);
        Run(CheckUnRegisterAll);
        Run(CheckUnRegisterAllDuringBroadcast);
        Run(CheckLifetime);
        Run(MeasureAllocations);
    }

    private static void CheckTypeAndInstanceIsolation()
    {
        EventBus first = new EventBus();
        EventBus second = new EventBus();
        RecordingListener<SampleEvent> firstListener = new RecordingListener<SampleEvent>();
        RecordingListener<SampleEvent> secondListener = new RecordingListener<SampleEvent>();
        RecordingListener<OtherEvent> otherListener = new RecordingListener<OtherEvent>();
        first.Register(firstListener);
        first.Register(otherListener);
        second.Register(secondListener);

        first.Broadcast(new SampleEvent(17));
        Check(firstListener.ReceiveCount == 1 && firstListener.LastMessage.Value == 17,
            "A typed value reaches its matching listener unchanged.");
        Check(secondListener.ReceiveCount == 0 && otherListener.ReceiveCount == 0,
            "Event types and event system instances are isolated.");
        second.Broadcast(new SampleEvent(23));
        first.Broadcast(new OtherEvent(31));
        Check(secondListener.LastMessage.Value == 23 && otherListener.LastMessage.Value == 31,
            "Independent subscriptions remain usable.");
        Check(ReferenceEquals(EventBus.Shared, EventBus.Shared), "Shared returns the same event system.");
    }

    private static void CheckOrderAndFilter()
    {
        EventBus events = new EventBus();
        List<int> order = new List<int>();
        RecordingListener<SampleEvent> first = new RecordingListener<SampleEvent>();
        RecordingListener<SampleEvent> second = new RecordingListener<SampleEvent>();
        first.OnReceivedOrNull = () => order.Add(1);
        second.OnReceivedOrNull = () => order.Add(2);
        events.Register(first);
        events.Register(second);
        events.Register(first);
        events.Broadcast(new SampleEvent(1));
        Check(order.Count == 2 && order[0] == 1 && order[1] == 2,
            "Listeners run once each, in registration order.");

        order.Clear();
        events.Broadcast(new SampleEvent(2), listener => ReferenceEquals(listener, second));
        Check(order.Count == 1 && order[0] == 2, "A filter selects the intended listener.");
        events.UnRegister(first);
        events.UnRegister(first);
        events.Broadcast(new SampleEvent(3));
        Check(first.ReceiveCount == 1 && second.ReceiveCount == 3,
            "Repeated unregistration is harmless and does not remove other listeners.");
    }

    private static void CheckReferenceIdentity()
    {
        EventBus events = new EventBus();
        ValueEqualListener first = new ValueEqualListener();
        ValueEqualListener second = new ValueEqualListener();
        events.Register<SampleEvent>(first);
        events.Register<SampleEvent>(second);
        events.Register<SampleEvent>(first);
        events.Broadcast(new SampleEvent(1));
        Check(first.ReceiveCount == 1 && second.ReceiveCount == 1,
            "Distinct listeners are retained even when Equals considers them equal.");
        events.UnRegister<SampleEvent>(first);
        events.Broadcast(new SampleEvent(2));
        Check(first.ReceiveCount == 1 && second.ReceiveCount == 2,
            "Unregistration uses reference identity too.");
    }

    private static void CheckMissingSubscriptionsAndNulls()
    {
        EventBus events = new EventBus();
        RecordingListener<SampleEvent> listener = new RecordingListener<SampleEvent>();
        events.UnRegister(listener);
        events.Broadcast(new SampleEvent(1), ThrowFromFilter);
        Check(listener.ReceiveCount == 0, "Unknown types can be published or unregistered without callbacks.");
        CheckThrows<ArgumentNullException>(() => events.Register<SampleEvent>(null!), "Null registration is rejected.");
        CheckThrows<ArgumentNullException>(() => events.UnRegister<SampleEvent>(null!), "Null unregistration is rejected.");
        CheckThrows<ArgumentNullException>(() => events.Register<SampleEvent, OtherEvent>(null!),
            "Null multi-type registration is rejected.");
        CheckThrows<ArgumentNullException>(() => events.UnRegisterAll(null!),
            "Null UnRegisterAll is rejected.");
    }

    private static void CheckRemovalDuringBroadcast()
    {
        EventBus events = new EventBus();
        RecordingListener<SampleEvent> first = new RecordingListener<SampleEvent>();
        RecordingListener<SampleEvent> second = new RecordingListener<SampleEvent>();
        RecordingListener<SampleEvent> third = new RecordingListener<SampleEvent>();
        events.Register(first);
        events.Register(second);
        events.Register(third);

        void RemoveSelfAndNext()
        {
            events.UnRegister(first);
            events.UnRegister(second);
        }

        first.OnReceivedOrNull = RemoveSelfAndNext;
        events.Broadcast(new SampleEvent(1));
        Check(first.ReceiveCount == 1 && second.ReceiveCount == 0 && third.ReceiveCount == 1,
            "Removing self and a future listener skips the removed listener but preserves the rest.");
        events.Broadcast(new SampleEvent(2));
        Check(first.ReceiveCount == 1 && second.ReceiveCount == 0 && third.ReceiveCount == 2,
            "Removal during a callback persists into subsequent broadcasts.");

        EventBus another = new EventBus();
        RecordingListener<SampleEvent> earlier = new RecordingListener<SampleEvent>();
        RecordingListener<SampleEvent> current = new RecordingListener<SampleEvent>();
        RecordingListener<SampleEvent> later = new RecordingListener<SampleEvent>();
        another.Register(earlier);
        another.Register(current);
        another.Register(later);
        current.OnReceivedOrNull = () => another.UnRegister(earlier);
        another.Broadcast(new SampleEvent(1));
        Check(earlier.ReceiveCount == 1 && current.ReceiveCount == 1 && later.ReceiveCount == 1,
            "Removing an already-notified listener does not duplicate or skip callbacks.");
        current.OnReceivedOrNull = () => another.UnRegister(current);
        another.Broadcast(new SampleEvent(2));
        another.Broadcast(new SampleEvent(3));
        Check(earlier.ReceiveCount == 1 && current.ReceiveCount == 2 && later.ReceiveCount == 3,
            "Removing only self preserves other listeners in this and later broadcasts.");
    }

    private static void CheckRegistrationDuringBroadcast()
    {
        EventBus events = new EventBus();
        RecordingListener<SampleEvent> first = new RecordingListener<SampleEvent>();
        RecordingListener<SampleEvent> late = new RecordingListener<SampleEvent>();
        first.OnReceivedOrNull = () => events.Register(late);
        events.Register(first);
        events.Broadcast(new SampleEvent(1));
        Check(late.ReceiveCount == 0, "A newly registered listener does not join the current snapshot.");
        events.Broadcast(new SampleEvent(2));
        Check(first.ReceiveCount == 2 && late.ReceiveCount == 1,
            "A new listener participates starting with the next broadcast.");
    }

    private static void CheckExceptionsAndReentrancy()
    {
        EventBus events = new EventBus();
        RecordingListener<SampleEvent> first = new RecordingListener<SampleEvent>();
        RecordingListener<SampleEvent> second = new RecordingListener<SampleEvent>();
        events.Register(first);
        events.Register(second);
        first.OnReceivedOrNull = ThrowFromListener;
        CheckThrows<ApplicationException>(() => events.Broadcast(new SampleEvent(1)), "Listener errors reach the caller.");
        Check(first.ReceiveCount == 1 && second.ReceiveCount == 0, "A listener error stops the current broadcast.");
        first.OnReceivedOrNull = null;
        events.Broadcast(new SampleEvent(2));
        Check(first.ReceiveCount == 2 && second.ReceiveCount == 1, "Broadcast state recovers after a listener error.");

        CheckThrows<ApplicationException>(() => events.Broadcast(new SampleEvent(3), ThrowFromFilter),
            "Filter errors reach the caller.");
        events.Broadcast(new SampleEvent(4));
        Check(first.ReceiveCount == 3 && second.ReceiveCount == 2, "Broadcast state recovers after a filter error.");

        first.OnReceivedOrNull = () => events.Broadcast(new SampleEvent(5));
        CheckThrows<InvalidOperationException>(() => events.Broadcast(new SampleEvent(5)),
            "Same-type recursive broadcasting is rejected.");
        first.OnReceivedOrNull = null;
        events.Broadcast(new SampleEvent(6));
        Check(first.ReceiveCount == 5 && second.ReceiveCount == 3,
            "A recursive broadcast error does not leave the pipeline processing.");

        RecordingListener<OtherEvent> other = new RecordingListener<OtherEvent>();
        events.Register(other);
        first.OnReceivedOrNull = () => events.Broadcast(new OtherEvent(7));
        events.Broadcast(new SampleEvent(7));
        Check(other.ReceiveCount == 1 && other.LastMessage.Value == 7 && second.ReceiveCount == 4,
            "A callback can synchronously broadcast a different event type.");
    }

    private static void CheckMultiTypeRegistration()
    {
        EventBus events = new EventBus();
        RecordingListener<SampleEvent> partial = new RecordingListener<SampleEvent>();
        CheckThrows<ArgumentException>(() => events.Register<SampleEvent, OtherEvent>(partial),
            "A multi-type listener must implement every requested interface.");
        events.Broadcast(new SampleEvent(1));
        Check(partial.ReceiveCount == 0, "Failed multi-type registration does not register earlier types.");

        MultiListener listener = new MultiListener();
        events.Register<SampleEvent, OtherEvent>(listener);
        events.Broadcast(new SampleEvent(3));
        events.Broadcast(new OtherEvent(3));
        events.UnRegisterAll(listener);
        events.Broadcast(new SampleEvent(4));
        events.Broadcast(new OtherEvent(4));
        Check(listener.ReceiveCount == 2, "Two-type registration and UnRegisterAll cover both types.");

        EventBus allTypes = new EventBus();
        allTypes.Register<SampleEvent, OtherEvent, int, long, short, byte, sbyte, ushort, uint, ulong, float, double>(listener);
        BroadcastAllTypes(allTypes);
        Check(listener.ReceiveCount == 14, "The twelve-type overload registers every requested type.");
        allTypes.UnRegisterAll(listener);
        BroadcastAllTypes(allTypes);
        Check(listener.ReceiveCount == 14, "UnRegisterAll removes all twelve registered types.");

        CheckThrows<ArgumentException>(
            () => allTypes.Register<SampleEvent, OtherEvent, int, long, short, byte, sbyte, ushort, uint, ulong, float, UnsubscribedEvent>(listener),
            "The twelve-type overload rejects an unsupported final interface.");
        BroadcastAllTypes(allTypes);
        Check(listener.ReceiveCount == 14, "Failed twelve-type registration is atomic.");
    }

    private static void CheckUnRegisterAll()
    {
        EventBus events = new EventBus();
        MultiListener removed = new MultiListener();
        MultiListener kept = new MultiListener();
        RecordingListener<SampleEvent> keptSingle = new RecordingListener<SampleEvent>();
        events.Register<SampleEvent, OtherEvent>(removed);
        events.Register<SampleEvent, OtherEvent>(kept);
        events.Register(keptSingle);

        events.UnRegisterAll(removed);
        events.Broadcast(new SampleEvent(1));
        events.Broadcast(new OtherEvent(1));
        Check(removed.ReceiveCount == 0, "UnRegisterAll removes every type the listener subscribed to.");
        Check(kept.ReceiveCount == 2 && keptSingle.ReceiveCount == 1,
            "UnRegisterAll leaves other listeners subscribed to the same types.");

        events.UnRegisterAll(removed);
        events.UnRegisterAll(new RecordingListener<UnsubscribedEvent>());
        new EventBus().UnRegisterAll(kept);
        events.Broadcast(new SampleEvent(2));
        Check(kept.ReceiveCount == 3 && keptSingle.ReceiveCount == 2,
            "UnRegisterAll for an absent listener or on an empty event system does nothing.");

        EventBus another = new EventBus();
        another.Register<SampleEvent>(removed);
        events.UnRegisterAll(kept);
        another.Broadcast(new SampleEvent(3));
        Check(removed.ReceiveCount == 1, "UnRegisterAll affects only its own event system.");
    }

    private static void CheckUnRegisterAllDuringBroadcast()
    {
        EventBus events = new EventBus();
        RecordingListener<SampleEvent> first = new RecordingListener<SampleEvent>();
        MultiListener second = new MultiListener();
        RecordingListener<SampleEvent> third = new RecordingListener<SampleEvent>();
        RecordingListener<OtherEvent> other = new RecordingListener<OtherEvent>();
        events.Register(first);
        events.Register<SampleEvent, OtherEvent>(second);
        events.Register(third);
        events.Register(other);

        first.OnReceivedOrNull = () => events.UnRegisterAll(second);
        events.Broadcast(new SampleEvent(1));
        Check(first.ReceiveCount == 1 && second.ReceiveCount == 0 && third.ReceiveCount == 1,
            "UnRegisterAll during a broadcast skips the removed listener and keeps the rest.");
        events.Broadcast(new OtherEvent(1));
        Check(second.ReceiveCount == 0 && other.ReceiveCount == 1,
            "UnRegisterAll during a broadcast also removes the listener's other types.");

        first.OnReceivedOrNull = () => events.UnRegisterAll(first);
        events.Broadcast(new SampleEvent(2));
        events.Broadcast(new SampleEvent(3));
        Check(first.ReceiveCount == 2 && third.ReceiveCount == 3,
            "A listener can UnRegisterAll itself during its own callback.");
    }

    private static void BroadcastAllTypes(EventBus events)
    {
        events.Broadcast(new SampleEvent(1));
        events.Broadcast(new OtherEvent(1));
        events.Broadcast(1);
        events.Broadcast(1L);
        events.Broadcast((short)1);
        events.Broadcast((byte)1);
        events.Broadcast((sbyte)1);
        events.Broadcast((ushort)1);
        events.Broadcast(1U);
        events.Broadcast(1UL);
        events.Broadcast(1.0f);
        events.Broadcast(1.0);
    }

    private static void CheckLifetime()
    {
        WeakReference[] abandoned = CreateAbandonedEventBus();
        EventBus retainedEvents = new EventBus();
        WeakReference removedListener = CreateRemovedListener(retainedEvents);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Check(!abandoned[0].IsAlive && !abandoned[1].IsAlive,
            "An abandoned event system and its listener are not retained by static caches.");
        Check(!removedListener.IsAlive, "Unregistration and snapshot cleanup release the listener.");
        GC.KeepAlive(retainedEvents);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] CreateAbandonedEventBus()
    {
        EventBus events = new EventBus();
        RecordingListener<SampleEvent> listener = new RecordingListener<SampleEvent>();
        events.Register(listener);
        events.Broadcast(new SampleEvent(1));
        return new WeakReference[] { new WeakReference(events), new WeakReference(listener) };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateRemovedListener(EventBus events)
    {
        RecordingListener<SampleEvent> listener = new RecordingListener<SampleEvent>();
        events.Register(listener);
        events.Broadcast(new SampleEvent(1));
        events.UnRegister(listener);
        return new WeakReference(listener);
    }

    private static void MeasureAllocations()
    {
        EventBus events = new EventBus();
        RecordingListener<SampleEvent> listener = new RecordingListener<SampleEvent>();
        events.Register(listener);
        Func<IEventListener<SampleEvent>, bool> filter = static receiver => true;
        for (int index = 0; index < WARMUP_COUNT; index++)
        {
            events.Broadcast(new SampleEvent(index));
            events.Broadcast(new SampleEvent(index), filter);
            events.Broadcast(new UnsubscribedEvent(index));
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < PUBLISH_COUNT; index++)
        {
            events.Broadcast(new SampleEvent(index));
        }
        long normalBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < PUBLISH_COUNT; index++)
        {
            events.Broadcast(new SampleEvent(index), filter);
        }
        long filteredBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        EventBus emptyEvents = new EventBus();
        before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < PUBLISH_COUNT; index++)
        {
            emptyEvents.Broadcast(new UnsubscribedEvent(index));
        }
        long unsubscribedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Check(listener.ReceiveCount == 2 * (WARMUP_COUNT + PUBLISH_COUNT)
            && listener.LastMessage.Value == PUBLISH_COUNT - 1,
            "Allocation measurements deliver every subscribed event.");
        Console.WriteLine($"Allocations after warmup ({PUBLISH_COUNT:N0} broadcasts each):");
        Console.WriteLine($"  Struct event: {normalBytes:N0} B; cached filter: {filteredBytes:N0} B; unsubscribed type: {unsubscribedBytes:N0} B.");
        Console.WriteLine("  Setup, subscription growth, and user callback allocations are outside this measurement.");
    }

    private static void ThrowFromListener()
    {
        throw new ApplicationException("Expected listener failure.");
    }

    private static bool ThrowFromFilter(IEventListener<SampleEvent> listener)
    {
        throw new ApplicationException("Expected filter failure.");
    }
}
