using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace asa_server_controller.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServerPasswordToRemoteServers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServerPassword",
                table: "RemoteServers",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServerPassword",
                table: "RemoteServers");
        }
    }
}
