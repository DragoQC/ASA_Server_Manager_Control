namespace managerwebapp.Services;

public sealed class WireGuardInstallService(IServiceScopeFactory serviceScopeFactory)
{
    private readonly object _sync = new();
    private Task? _currentTask;

    public bool IsInstalling { get; private set; }
    public string? LastMessage { get; private set; }
    public bool LastRunFailed { get; private set; }

    public Task StartInstallAsync()
    {
        lock (_sync)
        {
            if (IsInstalling)
            {
                return Task.CompletedTask;
            }

            IsInstalling = true;
            LastMessage = "WireGuard install started.";
            LastRunFailed = false;
            _currentTask = RunInstallAsync();
            return Task.CompletedTask;
        }
    }

    public async Task WaitForCompletionAsync()
    {
        Task? currentTask;

        lock (_sync)
        {
            currentTask = _currentTask;
        }

        if (currentTask is not null)
        {
            await currentTask;
        }
    }

    private async Task RunInstallAsync()
    {
        try
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            SudoService sudoService = scope.ServiceProvider.GetRequiredService<SudoService>();
            LastMessage = await sudoService.InstallWireGuardAsync();
            LastRunFailed = false;
        }
        catch (Exception exception)
        {
            LastMessage = exception.Message;
            LastRunFailed = true;
        }
        finally
        {
            lock (_sync)
            {
                IsInstalling = false;
                _currentTask = null;
            }
        }
    }
}
