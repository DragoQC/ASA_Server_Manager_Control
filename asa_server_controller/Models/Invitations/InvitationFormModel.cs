namespace asa_server_controller.Models.Invitations;

public sealed class InvitationFormModel
{
    public string? ClusterId { get; set; }
    public string? VpnAddress { get; set; }
    public string? Port { get; set; }
    public string? ExposedGamePort { get; set; }
}
