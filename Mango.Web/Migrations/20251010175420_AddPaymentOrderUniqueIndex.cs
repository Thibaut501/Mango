using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mango.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentOrderUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderHeaderId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderHeaderId",
                table: "Payments",
                column: "OrderHeaderId",
                unique: true,
                filter: "[OrderHeaderId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderHeaderId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "OrderHeaderId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Payments");
        }
    }
}
