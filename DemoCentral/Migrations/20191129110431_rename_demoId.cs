using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DemoCentral.Migrations
{
    public partial class rename_demoId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "democentral");

            migrationBuilder.CreateTable(
                name: "__efmigrationhistory",
                schema: "democentral",
                columns: table => new
                {
                    MigrationId = table.Column<string>(unicode: false, maxLength: 150, nullable: false),
                    ContextKey = table.Column<string>(unicode: false, maxLength: 300, nullable: false),
                    Model = table.Column<byte[]>(type: "longblob", nullable: false),
                    ProductVersion = table.Column<string>(unicode: false, maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___efmigrationhistory", x => x.MigrationId);
                });

            migrationBuilder.CreateTable(
                name: "Demo",
                schema: "democentral",
                columns: table => new
                {
                    MatchId = table.Column<int>(type: "int(11)", nullable: false),
                    MatchDate = table.Column<DateTime>(nullable: false),
                    UploaderId = table.Column<long>(type: "bigint(20)", nullable: false),
                    UploadType = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    UploadStatus = table.Column<byte>(nullable: false),
                    Source = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    DownloadUrl = table.Column<string>(type: "longtext", nullable: true),
                    FileName = table.Column<string>(type: "longtext", nullable: true),
                    FilePath = table.Column<string>(type: "longtext", nullable: true),
                    MD5Hash = table.Column<string>(type: "longtext", nullable: true),
                    FileStatus = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    DemoAnalyzerStatus = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    DemoAnalyzerVersion = table.Column<string>(nullable: true),
                    UploadDate = table.Column<DateTime>(nullable: false),
                    Event = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demo", x => x.MatchId);
                });

            migrationBuilder.CreateTable(
                name: "InQueue",
                schema: "democentral",
                columns: table => new
                {
                    MatchId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MatchDate = table.Column<DateTime>(nullable: false),
                    UploaderId = table.Column<long>(type: "bigint(20)", nullable: false),
                    UploadType = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    Source = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    InsertDate = table.Column<DateTime>(nullable: false),
                    DFWQUEUE = table.Column<short>(nullable: false),
                    SOQUEUE = table.Column<short>(nullable: false),
                    Retries = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InQueue", x => x.MatchId);
                });

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "__efmigrationhistory",
                schema: "democentral");

            migrationBuilder.DropTable(
                name: "Demo",
                schema: "democentral");

            migrationBuilder.DropTable(
                name: "InQueue",
                schema: "democentral");
        }
    }
}
