using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAssistant.Server.Data.Migrations.MSSQLMigrations
{
    /// <inheritdoc />
    public partial class AddGroupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DashboardOrder",
                table: "Devices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Devices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInDashboard",
                table: "Devices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DashboardOrder",
                table: "Clients",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Clients",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowInDashboard",
                table: "Clients",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IconId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShowInDashboard = table.Column<bool>(type: "bit", nullable: false),
                    DashboardOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlternativeNames = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Groups_ServerFiles_IconId",
                        column: x => x.IconId,
                        principalTable: "ServerFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_GroupId",
                table: "Devices",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_GroupId",
                table: "Clients",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_IconId",
                table: "Groups",
                column: "IconId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Groups_GroupId",
                table: "Clients",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Groups_GroupId",
                table: "Devices",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Groups_GroupId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Groups_GroupId",
                table: "Devices");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Devices_GroupId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Clients_GroupId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "DashboardOrder",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ShowInDashboard",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "DashboardOrder",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ShowInDashboard",
                table: "Clients");
        }
    }
}
