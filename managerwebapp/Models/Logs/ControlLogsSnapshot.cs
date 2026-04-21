namespace managerwebapp.Models.Logs;

public sealed record ControlLogsSnapshot(
    LogSectionSnapshot StatusSection,
    LogSectionSnapshot JournalSection,
    DateTimeOffset LoadedAtUtc);
