using System;
using System.Collections.Generic;

namespace Elonia.Events
{
    internal sealed class EventPipeline<TMessage> : IEventPipeline
    {
        private readonly List<IEventListener<TMessage>> _listeners = new List<IEventListener<TMessage>>();
        private readonly List<IEventListener<TMessage>> _broadcastBuffer = new List<IEventListener<TMessage>>();

        public bool IsProcessing { get; private set; }

        public void AddListener(IEventListener<TMessage> listener)
        {
            if (FindListenerIndex(listener) < 0)
            {
                _listeners.Add(listener);
            }
        }

        public void RemoveListener(IEventListener<TMessage> listener)
        {
            int index = FindListenerIndex(listener);
            if (index >= 0)
            {
                _listeners.RemoveAt(index);
            }
        }

        public void Broadcast(TMessage message, Func<IEventListener<TMessage>, bool>? filterOrNull)
        {
            IsProcessing = true;
            try
            {
                _broadcastBuffer.AddRange(_listeners);
                for (int index = 0; index < _broadcastBuffer.Count; index++)
                {
                    IEventListener<TMessage> listener = _broadcastBuffer[index];
                    if (FindListenerIndex(listener) < 0)
                    {
                        continue;
                    }

                    if (filterOrNull != null && !filterOrNull(listener))
                    {
                        continue;
                    }

                    listener.OnEvent(message);
                }
            }
            finally
            {
                _broadcastBuffer.Clear();
                IsProcessing = false;
            }
        }

        private int FindListenerIndex(IEventListener<TMessage> listener)
        {
            for (int index = 0; index < _listeners.Count; index++)
            {
                if (ReferenceEquals(_listeners[index], listener))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
