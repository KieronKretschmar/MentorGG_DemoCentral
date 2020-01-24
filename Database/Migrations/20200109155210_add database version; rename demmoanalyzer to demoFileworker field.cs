using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class adddatabaseversionrenamedemmoanalyzertodemoFileworkerfield : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DemoAnalyzerStatus",
                schema: null,
                table: "Demo");

            migrationBuilder.DropColumn(
                name: "DemoAnalyzerVersion",
                schema: null,
                table: "Demo");

            migrationBuilder.AlterColumn<int>(
                name: "MatchId",
                schema: null,
                table: "Demo",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseVersion",
                schema: null,
                table: "Demo",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DemoFileWorkerStatus",
                schema: null,
                table: "Demo",
                type: "tinyint(3) unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "DemoFileWorkerVersion",
                schema: null,
                table: "Demo",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatabaseVersion",
                schema: null,
                table: "Demo");

            migrationBuilder.DropColumn(
                name: "DemoFileWorkerStatus",
                schema: null,
                table: "Demo");

            migrationBuilder.DropColumn(
                name: "DemoFileWorkerVersion",
                schema: null,
                table: "Demo");

            migrationBuilder.AlterColumn<int>(
                name: "MatchId",
                schema: null,
                table: "Demo",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<byte>(
                name: "DemoAnalyzerStatus",
                schema: null,
                table: "Demo",
                type: "tinyint(3) unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "DemoAnalyzerVersion",
                schema: null,
                table: "Demo",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true);
        }
    }
}
