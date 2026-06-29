using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aski.Tickets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SoftwareVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Software_Name_Version",
                table: "Software");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Software");

            migrationBuilder.AddColumn<int>(
                name: "SoftwareVersionId",
                table: "Tickets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SoftwareVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SoftwareId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    ReleasedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoftwareVersions_Software_SoftwareId",
                        column: x => x.SoftwareId,
                        principalTable: "Software",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SoftwareVersionId",
                table: "Tickets",
                column: "SoftwareVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Software_Name",
                table: "Software",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareVersions_SoftwareId_Version",
                table: "SoftwareVersions",
                columns: new[] { "SoftwareId", "Version" });

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_SoftwareVersions_SoftwareVersionId",
                table: "Tickets",
                column: "SoftwareVersionId",
                principalTable: "SoftwareVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_SoftwareVersions_SoftwareVersionId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "SoftwareVersions");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_SoftwareVersionId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Software_Name",
                table: "Software");

            migrationBuilder.DropColumn(
                name: "SoftwareVersionId",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Software",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Software_Name_Version",
                table: "Software",
                columns: new[] { "Name", "Version" });
        }
    }
}
