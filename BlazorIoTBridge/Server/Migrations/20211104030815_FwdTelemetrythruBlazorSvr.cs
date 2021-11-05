using Microsoft.EntityFrameworkCore.Migrations;

namespace BlazorIoTBridge.Server.Migrations
{
    public partial class FwdTelemetrythruBlazorSvr : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FwdTelemetrythruBlazorSvr",
                table: "Infos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FwdTelemetrythruBlazorSvr",
                table: "Infos");
        }
    }
}
