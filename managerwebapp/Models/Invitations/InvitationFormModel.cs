namespace managerwebapp.Models.Invitations;

public sealed class InvitationFormModel
{
    public string? ClusterId { get; set; }
    public string? RemoteUrl { get; set; }
    public string? VpnAddress { get; set; }
    public string? ApiKey { get; set; }
}
