using Microsoft.EntityFrameworkCore.Migrations;

namespace BlazorIoTBridge.Server.Migrations
{
    public partial class InfoId2Keypart2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceGuid",
                table: "Infos");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceGuid",
                table: "Infos",
                type: "TEXT",
                nullable: true);
        }
    }
}
