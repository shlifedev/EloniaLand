namespace Elonia.Events
{
    internal interface IEventPipeline
    {
        void RemoveListener(IEventListener listener);
    }
}
