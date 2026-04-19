using System.ComponentModel.DataAnnotations;

namespace managerwebapp.Models.CurseForge;

public sealed class CurseForgeApiSettingsModel
{
    [StringLength(512)]
    public string ApiKey { get; set; } = string.Empty;
}
