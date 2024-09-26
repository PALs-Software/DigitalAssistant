using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAssistant.Server.Data.Migrations.SQLiteMigrations
{
    /// <inheritdoc />
    public partial class AddLlmPropertiesToSetupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AsrProvider",
                table: "Setup",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InterpreterMode",
                table: "Setup",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LlmContextSize",
                table: "Setup",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LlmMode",
                table: "Setup",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LlmModel",
                table: "Setup",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LlmPrecision",
                table: "Setup",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsrProvider",
                table: "Setup");

            migrationBuilder.DropColumn(
                name: "InterpreterMode",
                table: "Setup");

            migrationBuilder.DropColumn(
                name: "LlmContextSize",
                table: "Setup");

            migrationBuilder.DropColumn(
                name: "LlmMode",
                table: "Setup");

            migrationBuilder.DropColumn(
                name: "LlmModel",
                table: "Setup");

            migrationBuilder.DropColumn(
                name: "LlmPrecision",
                table: "Setup");
        }
    }
}
