using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class Modeluseenumsaddbyteconversionfordbstorage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "democentral",
                table: "Demo",
                keyColumn: "MatchId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "democentral",
                table: "Demo",
                keyColumn: "MatchId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "democentral",
                table: "Demo",
                keyColumn: "MatchId",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "democentral",
                table: "InQueue");

            migrationBuilder.DropColumn(
                name: "UploadType",
                schema: "democentral",
                table: "InQueue");

            migrationBuilder.AlterColumn<long>(
                name: "UploaderId",
                schema: "democentral",
                table: "InQueue",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint(20)");

            migrationBuilder.AlterColumn<short>(
                name: "DDQUEUE",
                schema: "democentral",
                table: "InQueue",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<byte>(
                name: "UploadStatus",
                schema: "democentral",
                table: "Demo",
                type: "tinyint(3) unsigned",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint unsigned");

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
            migrationBuilder.AlterColumn<long>(
                name: "UploaderId",
                schema: "democentral",
                table: "InQueue",
                type: "bigint(20)",
                nullable: false,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<bool>(
                name: "DDQUEUE",
                schema: "democentral",
                table: "InQueue",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(short));

            migrationBuilder.AddColumn<byte>(
                name: "Source",
                schema: "democentral",
                table: "InQueue",
                type: "tinyint(3) unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "UploadType",
                schema: "democentral",
                table: "InQueue",
                type: "tinyint(3) unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<byte>(
                name: "UploadStatus",
                schema: "democentral",
                table: "Demo",
                type: "tinyint unsigned",
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint(3) unsigned");

            migrationBuilder.AlterColumn<int>(
                name: "MatchId",
                schema: "democentral",
                table: "Demo",
                type: "int(11)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int(11)")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.InsertData(
                schema: "democentral",
                table: "Demo",
                columns: new[] { "MatchId", "DemoAnalyzerStatus", "DemoAnalyzerVersion", "DownloadUrl", "Event", "FileName", "FilePath", "FileStatus", "MatchDate", "MD5Hash", "Source", "UploadDate", "UploadStatus", "UploadType", "UploaderId" },
                values: new object[] { 1, (byte)0, "", null, null, null, null, (byte)0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, (byte)1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), (byte)0, (byte)0, 1L });

            migrationBuilder.InsertData(
                schema: "democentral",
                table: "Demo",
                columns: new[] { "MatchId", "DemoAnalyzerStatus", "DemoAnalyzerVersion", "DownloadUrl", "Event", "FileName", "FilePath", "FileStatus", "MatchDate", "MD5Hash", "Source", "UploadDate", "UploadStatus", "UploadType", "UploaderId" },
                values: new object[] { 2, (byte)0, "", null, null, null, null, (byte)0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, (byte)1, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), (byte)0, (byte)0, 2L });

            migrationBuilder.InsertData(
                schema: "democentral",
                table: "Demo",
                columns: new[] { "MatchId", "DemoAnalyzerStatus", "DemoAnalyzerVersion", "DownloadUrl", "Event", "FileName", "FilePath", "FileStatus", "MatchDate", "MD5Hash", "Source", "UploadDate", "UploadStatus", "UploadType", "UploaderId" },
                values: new object[] { 3, (byte)0, "", null, null, null, null, (byte)0, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, (byte)2, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), (byte)0, (byte)0, 3L });
        }
    }
}
