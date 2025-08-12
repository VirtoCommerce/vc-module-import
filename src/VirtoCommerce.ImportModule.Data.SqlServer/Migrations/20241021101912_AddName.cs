using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ImportModule.Data.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ImportRunHistory",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "ImportRunHistory");
        }
    }
}
