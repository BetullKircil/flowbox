using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowBox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourierId",
                table: "Shipments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Couriers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Couriers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CourierId",
                table: "Shipments",
                column: "CourierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Couriers_CourierId",
                table: "Shipments",
                column: "CourierId",
                principalTable: "Couriers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Couriers_CourierId",
                table: "Shipments");

            migrationBuilder.DropTable(
                name: "Couriers");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_CourierId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CourierId",
                table: "Shipments");
        }
    }
}
