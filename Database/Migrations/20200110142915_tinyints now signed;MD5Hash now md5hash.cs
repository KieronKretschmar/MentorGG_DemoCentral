using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class tinyintsnowsignedMD5Hashnowmd5hash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MD5Hash",
                schema: null,
                table: "Demo",
                newName: "Md5hash");

            migrationBuilder.AlterColumn<sbyte>(
                name: "UploadType",
                schema: null,
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint(3) unsigned");

            migrationBuilder.AlterColumn<sbyte>(
                name: "UploadStatus",
                schema: null,
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint(3) unsigned");

            migrationBuilder.AlterColumn<sbyte>(
                name: "Source",
                schema: null,
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint(3) unsigned");

            migrationBuilder.AlterColumn<sbyte>(
                name: "FileStatus",
                schema: null,
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint(3) unsigned");

            migrationBuilder.AlterColumn<sbyte>(
                name: "DemoFileWorkerStatus",
                schema: null,
                table: "Demo",
                type: "tinyint(3)",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint(3) unsigned");

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
            migrationBuilder.RenameColumn(
                name: "Md5hash",
                schema: null,
                table: "Demo",
                newName: "MD5Hash");

            migrationBuilder.AlterColumn<byte>(
                name: "UploadType",
                schema: null,
                table: "Demo",
                type: "tinyint(3) unsigned",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<byte>(
                name: "UploadStatus",
                schema: null,
                table: "Demo",
                type: "tinyint(3) unsigned",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<byte>(
                name: "Source",
                schema: null,
                table: "Demo",
                type: "tinyint(3) unsigned",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<byte>(
                name: "FileStatus",
                schema: null,
                table: "Demo",
                type: "tinyint(3) unsigned",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

            migrationBuilder.AlterColumn<byte>(
                name: "DemoFileWorkerStatus",
                schema: null,
                table: "Demo",
                type: "tinyint(3) unsigned",
                nullable: false,
                oldClrType: typeof(sbyte),
                oldType: "tinyint(3)");

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
