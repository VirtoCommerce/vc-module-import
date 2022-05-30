using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ImportModule.Data.Infrastructure.Migrations
{
    public partial class AddImport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportProfile",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    DataImporterType = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    SellerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SellerName = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportProfile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportRunHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SellerId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProfileId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    JobId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Executed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Finished = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    ProcessedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorsCount = table.Column<int>(type: "int", nullable: false),
                    Errors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportRunHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportProfile_SellerId_Name",
                table: "ImportProfile",
                columns: new[] { "SellerId", "Name" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportProfile");

            migrationBuilder.DropTable(
                name: "ImportRunHistory");
        }
    }
}
