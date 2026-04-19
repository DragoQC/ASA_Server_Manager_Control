namespace managerwebapp.Data.Entities;

public sealed class RemoteServerEntity : BaseEntity
{
    public required string RemoteUrl { get; set; }
    public required string VpnAddress { get; set; }
    public required string InviteStatus { get; set; }
    public required string ValidationStatus { get; set; }
    public DateTimeOffset? LastSeenAtUtc { get; set; }
    public required string ApiKey { get; set; }
}
