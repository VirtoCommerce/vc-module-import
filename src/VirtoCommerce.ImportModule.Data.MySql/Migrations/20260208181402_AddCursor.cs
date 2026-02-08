using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ImportModule.Data.MySql.Migrations
{
    /// <inheritdoc />
    public partial class AddCursor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET @columnExists = (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ImportRunHistory' AND COLUMN_NAME = 'Cursor' AND TABLE_SCHEMA = DATABASE());
SET @sql = IF(@columnExists = 0,
    'ALTER TABLE `ImportRunHistory` ADD COLUMN `Cursor` varchar(2048) CHARACTER SET utf8mb4 NULL',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
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
