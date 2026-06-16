using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aski.ControlPlane.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProjectDbCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DbPassword",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DbUser",
                table: "Projects",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DbPassword",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DbUser",
                table: "Projects");
        }
    }
}
