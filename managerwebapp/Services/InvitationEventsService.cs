namespace managerwebapp.Services;

public sealed class InvitationEventsService
{
    public event Func<Task>? Changed;

    public Task NotifyChangedAsync()
    {
        if (Changed is null)
        {
            return Task.CompletedTask;
        }

        IEnumerable<Func<Task>> handlers = Changed
            .GetInvocationList()
            .Cast<Func<Task>>();

        return Task.WhenAll(handlers.Select(handler => handler()));
    }
}
