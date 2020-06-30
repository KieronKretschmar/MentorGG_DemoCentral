using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class addmatchdataremovalflag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MatchDataRemoved",
                table: "Demo",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MatchDataRemoved",
                table: "Demo");
        }
    }
}
