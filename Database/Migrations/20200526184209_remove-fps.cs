using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class removefps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FramesPerSecond",
                table: "Demo");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "FramesPerSecond",
                table: "Demo",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);
        }
    }
}
