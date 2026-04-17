namespace managerwebapp.Constants;

public static class VpnConstants
{
    public const string WgPath = "/usr/bin/wg";
    public const string WgQuickPath = "/usr/bin/wg-quick";
    public const string VpnConfigFilePath = "/opt/asa-control/vpn/wg0.conf";
    public const string WireGuardServiceName = "wg-quick@wg0";
}
