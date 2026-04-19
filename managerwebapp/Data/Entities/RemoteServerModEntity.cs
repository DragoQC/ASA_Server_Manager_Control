namespace managerwebapp.Data.Entities;

public sealed class RemoteServerModEntity : BaseEntity
{
    public int RemoteServerId { get; set; }
    public int ModEntityId { get; set; }
}
