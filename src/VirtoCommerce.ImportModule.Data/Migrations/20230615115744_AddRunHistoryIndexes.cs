using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtoCommerce.ImportModule.Data.Migrations
{
    public partial class AddRunHistoryIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ImportRunHistory_JobId",
                table: "ImportRunHistory",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRunHistory_ProfileId",
                table: "ImportRunHistory",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportRunHistory_UserId",
                table: "ImportRunHistory",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportRunHistory_JobId",
                table: "ImportRunHistory");

            migrationBuilder.DropIndex(
                name: "IX_ImportRunHistory_ProfileId",
                table: "ImportRunHistory");

            migrationBuilder.DropIndex(
                name: "IX_ImportRunHistory_UserId",
                table: "ImportRunHistory");
        }
    }
}
