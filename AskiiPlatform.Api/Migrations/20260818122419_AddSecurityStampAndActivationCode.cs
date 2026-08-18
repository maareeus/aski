using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AskiiPlatform.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityStampAndActivationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivationCodeExpiresUtc",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationCodeHash",
                table: "Users",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // Le righe esistenti riceverebbero la stringa vuota, che il
            // controllo di revoca tratta come token non valido: senza questo
            // riempimento un aggiornamento in produzione disconnetterebbe
            // ogni utente e nessuno riuscirebbe più ad autenticarsi.
            migrationBuilder.Sql(
                "UPDATE Users SET SecurityStamp = lower(hex(randomblob(16))) " +
                "WHERE SecurityStamp IS NULL OR SecurityStamp = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivationCodeExpiresUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActivationCodeHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");
        }
    }
}
