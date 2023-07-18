using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ParrotFlintBot.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddedAppSettingsAndNewColumnsForProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorUid",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectPid",
                table: "Projects");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Projects",
                type: "integer",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AddColumn<string>(
                name: "CreatorSlug",
                table: "Projects",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastUpdateTitle",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "PrevUpdatesCount",
                table: "Projects",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "ProjectSlug",
                table: "Projects",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateOfLastCrawl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Id", "DateOfLastCrawl" },
                values: new object[] { 1, new DateTime(2023, 5, 17, 14, 14, 26, 317, DateTimeKind.Utc).AddTicks(4007) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropColumn(
                name: "CreatorSlug",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LastUpdateTitle",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PrevUpdatesCount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectSlug",
                table: "Projects");

            migrationBuilder.AlterColumn<short>(
                name: "Status",
                table: "Projects",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<long>(
                name: "CreatorUid",
                table: "Projects",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ProjectPid",
                table: "Projects",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
