using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotFlintBot.DB.Migrations
{
    /// <inheritdoc />
    public partial class RemovedProxyList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProxyList",
                table: "AppSettings");

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DateOfLastCrawl",
                value: new DateTime(2023, 6, 13, 13, 0, 11, 923, DateTimeKind.Utc).AddTicks(9131));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
    }
}
