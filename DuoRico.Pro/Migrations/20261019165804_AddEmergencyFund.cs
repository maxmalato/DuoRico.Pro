using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuoRico.Pro.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencyFund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmergencyFunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoupleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyFunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmergencyFunds_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyFunds_CoupleId",
                table: "EmergencyFunds",
                column: "CoupleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyFunds_UpdatedByUserId",
                table: "EmergencyFunds",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyFunds");
        }
    }
}
