using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ImportModule.Data.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCursor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ImportRunHistory') AND name = 'Cursor')
    ALTER TABLE [ImportRunHistory] ADD [Cursor] nvarchar(2048) NULL
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
