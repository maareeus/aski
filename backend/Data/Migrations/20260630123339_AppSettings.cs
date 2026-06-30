using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aski.Tickets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BrandName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    LogoData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    LogoContentType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    FaviconData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    FaviconContentType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
