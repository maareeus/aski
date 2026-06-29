using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aski.Tickets.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class VersionReleaseNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReleaseNotes",
                table: "SoftwareVersions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReleaseNotes",
                table: "SoftwareVersions");
        }
    }
}
