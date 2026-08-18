using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskiiPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMfaToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TFA_Availables",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TFA_Availables",
                table: "Users");
        }
    }
}
