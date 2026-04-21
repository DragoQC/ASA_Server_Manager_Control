namespace managerwebapp.Models.Settings;

public sealed record LoginMethodAvailability(
    bool PasswordEnabled,
    bool EmailEnabled,
    bool TwoFactorEnabled);
