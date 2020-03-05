using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class renamefilepathcolumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "Demo",
                newName: "BlobUrl");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BlobUrl",
                table: "Demo",
                newName: "FilePath");
        }
    }
}
