using Microsoft.EntityFrameworkCore.Migrations;

namespace BlazorIoTBridge.Server.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Infos",
                columns: table => new
                {
                    DeviceGuid = table.Column<string>(type: "TEXT", nullable: false),
                    HUB_NAME = table.Column<string>(type: "TEXT", nullable: true),
                    DEVICE_NAME = table.Column<string>(type: "TEXT", nullable: true),
                    SHARED_ACCESS_KEY_NAME = table.Column<string>(type: "TEXT", nullable: true),
                    IOTHUB_DEVICE_CONN_STRING = table.Column<string>(type: "TEXT", nullable: true),
                    IOTHUB_HUB_CONN_STRING = table.Column<string>(type: "TEXT", nullable: true),
                    SERVICE_CONNECTION_STRING = table.Column<string>(type: "TEXT", nullable: true),
                    SYMMETRIC_KEY = table.Column<string>(type: "TEXT", nullable: true),
                    EVENT_HUBS_CONNECTION_STRING = table.Column<string>(type: "TEXT", nullable: true),
                    EVENT_HUBS_COMPATIBILITY_PATH = table.Column<string>(type: "TEXT", nullable: true),
                    EVENT_HUBS_SAS_KEY = table.Column<string>(type: "TEXT", nullable: true),
                    EVENT_HUBS_COMPATIBILITY_ENDPOINT = table.Column<string>(type: "TEXT", nullable: true),
                    Txt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Infos", x => x.DeviceGuid);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Infos");
        }
    }
}
