namespace Elonia.Events
{
    public interface IEventListener
    {
    }

    public interface IEventListener<in TMessage> : IEventListener
    {
        void OnEvent(TMessage message);
    }
}
