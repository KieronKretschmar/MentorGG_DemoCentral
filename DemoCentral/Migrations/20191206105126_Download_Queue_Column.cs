using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DemoCentral.Migrations
{
    public partial class Download_Queue_Column : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DDQUEUE",
                schema: "democentral",
                table: "InQueue",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "MatchId",
                schema: "democentral",
                table: "Demo",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DDQUEUE",
                schema: "democentral",
                table: "InQueue");

            migrationBuilder.AlterColumn<int>(
                name: "MatchId",
                schema: "democentral",
                table: "Demo",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }
    }
}
