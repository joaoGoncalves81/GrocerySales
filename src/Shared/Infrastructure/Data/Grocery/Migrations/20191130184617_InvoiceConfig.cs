using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Data.Grocery.Migrations
{
    public partial class InvoiceConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CatalogAttribute1",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CatalogAttribute2",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CatalogAttribute3",
                table: "OrderItems");

            migrationBuilder.AddColumn<string>(
                name: "CustomizeName",
                table: "OrderItems",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoiceConfig",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Week = table.Column<int>(nullable: false),
                    WeekState = table.Column<int>(nullable: false),
                    MaxValue = table.Column<decimal>(nullable: false),
                    NrInvoices = table.Column<int>(nullable: false),
                    LastRunDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceConfig", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceConfig");

            migrationBuilder.DropColumn(
                name: "CustomizeName",
                table: "OrderItems");

            migrationBuilder.AddColumn<int>(
                name: "CatalogAttribute1",
                table: "OrderItems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogAttribute2",
                table: "OrderItems",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CatalogAttribute3",
                table: "OrderItems",
                nullable: true);
        }
    }
}
