using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Data.Grocery.Migrations
{
    public partial class InvoiceConfig2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeekState",
                table: "InvoiceConfig");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "InvoiceConfig",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "InvoiceConfig");

            migrationBuilder.AddColumn<int>(
                name: "WeekState",
                table: "InvoiceConfig",
                nullable: false,
                defaultValue: 0);
        }
    }
}
