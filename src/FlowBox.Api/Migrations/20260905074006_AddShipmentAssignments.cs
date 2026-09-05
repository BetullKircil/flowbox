using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowBox.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Couriers_CourierId",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_CourierId",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CourierId",
                table: "Shipments");

            migrationBuilder.CreateTable(
                name: "ShipmentAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourierId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentAssignments_Couriers_CourierId",
                        column: x => x.CourierId,
                        principalTable: "Couriers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentAssignments_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAssignments_CourierId",
                table: "ShipmentAssignments",
                column: "CourierId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAssignments_ShipmentId",
                table: "ShipmentAssignments",
                column: "ShipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentAssignments");

            migrationBuilder.AddColumn<Guid>(
                name: "CourierId",
                table: "Shipments",
                type: "uuid",
                nullable: true);

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
    }
}
