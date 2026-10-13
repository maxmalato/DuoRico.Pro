using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DuoRico.Pro.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId_Year_Month",
                table: "Transactions",
                columns: new[] { "UserId", "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CoupleId",
                table: "AspNetUsers",
                column: "CoupleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_UserId_Year_Month",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CoupleId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");
        }
    }
}
