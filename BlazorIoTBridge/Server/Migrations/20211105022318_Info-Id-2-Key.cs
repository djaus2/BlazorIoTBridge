using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BlazorIoTBridge.Server.Migrations
{
    public partial class InfoId2Key : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Infos",
                table: "Infos");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceGuid",
                table: "Infos",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Infos",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Infos",
                table: "Infos",
                column: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Infos",
                table: "Infos");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Infos");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceGuid",
                table: "Infos",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Infos",
                table: "Infos",
                column: "DeviceGuid");
        }
    }
}
