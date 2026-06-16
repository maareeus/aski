using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aski.ControlPlane.Data.Migrations
{
    /// <inheritdoc />
    public partial class StripeMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTestMode",
                table: "StripeSettings");

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "StripeSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "StripeSettings");

            migrationBuilder.AddColumn<bool>(
                name: "IsTestMode",
                table: "StripeSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
