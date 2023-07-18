using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotFlintBot.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddedProxyListToAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "ProxyList",
                table: "AppSettings",
                type: "text[]",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DateOfLastCrawl", "ProxyList" },
                values: new object[] { new DateTime(2023, 6, 4, 15, 22, 14, 500, DateTimeKind.Utc).AddTicks(2938), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProxyList",
                table: "AppSettings");

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DateOfLastCrawl",
                value: new DateTime(2023, 5, 17, 14, 14, 26, 317, DateTimeKind.Utc).AddTicks(4007));
        }
    }
}
