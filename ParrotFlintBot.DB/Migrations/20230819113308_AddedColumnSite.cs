using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParrotFlintBot.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddedColumnSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Site",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DateOfLastCrawl",
                value: new DateTime(2023, 8, 19, 11, 33, 8, 617, DateTimeKind.Utc).AddTicks(2039));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Site",
                table: "Projects");

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "DateOfLastCrawl",
                value: new DateTime(2023, 6, 13, 13, 0, 11, 923, DateTimeKind.Utc).AddTicks(9131));
        }
    }
}
