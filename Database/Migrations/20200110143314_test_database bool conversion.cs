using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class test_databaseboolconversion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "SOQUEUE",
                schema: null,
                table: "InQueue",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<bool>(
                name: "DFWQUEUE",
                schema: null,
                table: "InQueue",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<bool>(
                name: "DDQUEUE",
                schema: null,
                table: "InQueue",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<int>(
                name: "MatchId",
                schema: null,
                table: "Demo",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "SOQUEUE",
                schema: null,
                table: "InQueue",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(bool));

            migrationBuilder.AlterColumn<short>(
                name: "DFWQUEUE",
                schema: null,
                table: "InQueue",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(bool));

            migrationBuilder.AlterColumn<short>(
                name: "DDQUEUE",
                schema: null,
                table: "InQueue",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(bool));

            migrationBuilder.AlterColumn<int>(
                name: "MatchId",
                schema: null,
                table: "Demo",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }
    }
}
