namespace Shared.Shared.Events.EventBus
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event) where T : class;
    }
}
