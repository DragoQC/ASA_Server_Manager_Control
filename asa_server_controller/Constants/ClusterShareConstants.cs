namespace asa_server_controller.Constants;

public static class ClusterShareConstants
{
    public const string ClusterDirectoryPath = "/opt/asa-control/cluster";
    public const string SmbDirectoryPath = "/opt/asa-control/smb";
    public const string ServerConfigFilePath = SmbDirectoryPath + "/smb.conf";
    public const string ClientConfigFilePath = SmbDirectoryPath + "/client.mount.cifs.conf";
    public const string ApplyServerScriptPath = SmbDirectoryPath + "/apply-smb-server.sh";
    public const string SystemServerConfigFilePath = "/etc/samba/smb.conf";
    public const string SmbServiceName = "smbd";
    public const string ShareName = "arkcluster";
    public const string ClientMountPath = "/opt/asa/cluster";
}
