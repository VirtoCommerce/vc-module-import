using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ImportModule.Data.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class AddCursor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE ""ImportRunHistory"" ADD COLUMN IF NOT EXISTS ""Cursor"" character varying(2048) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cursor",
                table: "ImportRunHistory");
        }
    }
}
