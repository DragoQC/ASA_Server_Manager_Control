namespace managerwebapp.Models.Logs;

public sealed record ControlLogsSnapshot(
    LogSectionSnapshot StatusSection,
    LogSectionSnapshot WireGuardStatusSection,
    LogSectionSnapshot JournalSection,
    DateTimeOffset LoadedAtUtc);
