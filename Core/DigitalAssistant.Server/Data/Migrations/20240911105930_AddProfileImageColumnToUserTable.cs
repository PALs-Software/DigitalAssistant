using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalAssistant.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileImageColumnToUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProfileImageId",
                table: "DbUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DbUsers_ProfileImageId",
                table: "DbUsers",
                column: "ProfileImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_DbUsers_ServerFiles_ProfileImageId",
                table: "DbUsers",
                column: "ProfileImageId",
                principalTable: "ServerFiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DbUsers_ServerFiles_ProfileImageId",
                table: "DbUsers");

            migrationBuilder.DropIndex(
                name: "IX_DbUsers_ProfileImageId",
                table: "DbUsers");

            migrationBuilder.DropColumn(
                name: "ProfileImageId",
                table: "DbUsers");
        }
    }
}
