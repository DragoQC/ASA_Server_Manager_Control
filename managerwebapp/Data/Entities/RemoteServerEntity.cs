namespace managerwebapp.Data.Entities;

public sealed class RemoteServerEntity : BaseEntity
{
    public required string IpAddress { get; set; }
    public required string ApiKeyHash { get; set; }
}
