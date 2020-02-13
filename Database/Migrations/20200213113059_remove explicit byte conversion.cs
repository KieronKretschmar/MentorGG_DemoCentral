using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class removeexplicitbyteconversion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "UploadType",
                schema: "democentral",
                table: "Demo",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<int>(
                name: "UploadStatus",
                schema: "democentral",
                table: "Demo",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<byte>(
                name: "Source",
                schema: "democentral",
                table: "Demo",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<byte>(
                name: "Quality",
                schema: "democentral",
                table: "Demo",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<byte>(
                name: "Frames",
                schema: "democentral",
                table: "Demo",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<int>(
                name: "FileStatus",
                schema: "democentral",
                table: "Demo",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<int>(
                name: "DemoFileWorkerStatus",
                schema: "democentral",
                table: "Demo",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

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
            migrationBuilder.AlterColumn<sbyte>(
                name: "UploadType",
                schema: "democentral",
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<sbyte>(
                name: "UploadStatus",
                schema: "democentral",
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<sbyte>(
                name: "Source",
                schema: "democentral",
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<sbyte>(
                name: "Quality",
                schema: "democentral",
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<sbyte>(
                name: "Frames",
                schema: "democentral",
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(byte));

            migrationBuilder.AlterColumn<sbyte>(
                name: "FileStatus",
                schema: "democentral",
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<sbyte>(
                name: "DemoFileWorkerStatus",
                schema: "democentral",
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(int));

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
