using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aski.ControlPlane.Data.Migrations
{
    /// <inheritdoc />
    public partial class DbContainerHostPort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HostPort",
                table: "DbContainers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostPort",
                table: "DbContainers");
        }
    }
}
