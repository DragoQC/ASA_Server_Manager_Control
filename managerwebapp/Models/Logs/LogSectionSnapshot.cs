namespace managerwebapp.Models.Logs;

public sealed record LogSectionSnapshot(
    string Title,
    string Description,
    string Content,
    bool IsAvailable);
