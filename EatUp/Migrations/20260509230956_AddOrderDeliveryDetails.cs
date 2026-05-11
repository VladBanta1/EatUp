using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EatUp.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDeliveryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryApartment",
                table: "Orders",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryBlock",
                table: "Orders",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryStaircase",
                table: "Orders",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryApartment",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryBlock",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryStaircase",
                table: "Orders");
        }
    }
}
