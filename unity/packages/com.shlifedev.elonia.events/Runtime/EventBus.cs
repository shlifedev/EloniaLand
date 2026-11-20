using System;
using System.Collections.Generic;

namespace Elonia.Events
{
    public partial class EventBus
    {
        private readonly Dictionary<Type, IEventPipeline> _pipelines = new Dictionary<Type, IEventPipeline>();

        public static EventBus Shared { get; } = new EventBus();

        public void Register<TMessage>(IEventListener<TMessage> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            EventPipeline<TMessage>? pipelineOrNull = GetPipelineOrNull<TMessage>();
            if (pipelineOrNull == null)
            {
                pipelineOrNull = new EventPipeline<TMessage>();
                _pipelines.Add(typeof(TMessage), pipelineOrNull);
            }

            pipelineOrNull.AddListener(listener);
        }

        public void UnRegister<TMessage>(IEventListener<TMessage> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            EventPipeline<TMessage>? pipelineOrNull = GetPipelineOrNull<TMessage>();
            if (pipelineOrNull != null)
            {
                pipelineOrNull.RemoveListener(listener);
            }
        }

        public void Broadcast<TMessage>(TMessage message, Func<IEventListener<TMessage>, bool>? filterOrNull = null)
        {
            EventPipeline<TMessage>? pipelineOrNull = GetPipelineOrNull<TMessage>();
            if (pipelineOrNull == null)
            {
                return;
            }

            if (pipelineOrNull.IsProcessing)
            {
                throw new InvalidOperationException($"Cannot broadcast {typeof(TMessage).Name} while that event type is being delivered.");
            }

            pipelineOrNull.Broadcast(message, filterOrNull);
        }

        private EventPipeline<TMessage>? GetPipelineOrNull<TMessage>()
        {
            IEventPipeline? pipelineOrNull;
            if (_pipelines.TryGetValue(typeof(TMessage), out pipelineOrNull))
            {
                return (EventPipeline<TMessage>)pipelineOrNull;
            }

            return null;
        }

        private static IEventListener<TMessage> ValidateListener<TMessage>(IEventListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (listener is IEventListener<TMessage> typedListener)
            {
                return typedListener;
            }

            throw new ArgumentException($"The listener must implement IEventListener<{typeof(TMessage).Name}>.", nameof(listener));
        }
    }
}
