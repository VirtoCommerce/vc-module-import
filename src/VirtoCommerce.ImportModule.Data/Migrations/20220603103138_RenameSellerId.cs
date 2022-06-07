using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ImportModule.Data.Migrations
{
    public partial class RenameSellerId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellerId",
                table: "ImportRunHistory",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "SellerName",
                table: "ImportProfile",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "SellerId",
                table: "ImportProfile",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ImportProfile_SellerId_Name",
                table: "ImportProfile",
                newName: "IX_ImportProfile_UserId_Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ImportRunHistory",
                newName: "SellerId");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "ImportProfile",
                newName: "SellerName");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ImportProfile",
                newName: "SellerId");

            migrationBuilder.RenameIndex(
                name: "IX_ImportProfile_UserId_Name",
                table: "ImportProfile",
                newName: "IX_ImportProfile_SellerId_Name");
        }
    }
}
