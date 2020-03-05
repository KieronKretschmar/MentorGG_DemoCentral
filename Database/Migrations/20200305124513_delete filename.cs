using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class deletefilename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Demo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Demo",
                type: "longtext",
                nullable: true);
        }
    }
}
