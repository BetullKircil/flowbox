using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowBox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Couriers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Couriers_UserId",
                table: "Couriers",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Couriers_Users_UserId",
                table: "Couriers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Couriers_Users_UserId",
                table: "Couriers");

            migrationBuilder.DropIndex(
                name: "IX_Couriers_UserId",
                table: "Couriers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Couriers");
        }
    }
}
