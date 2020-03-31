using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Demo",
                columns: table => new
                {
                    MatchId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MatchDate = table.Column<DateTime>(nullable: false),
                    UploaderId = table.Column<long>(nullable: false),
                    UploadType = table.Column<byte>(nullable: false),
                    UploadStatus = table.Column<byte>(nullable: false),
                    Source = table.Column<byte>(nullable: false),
                    DownloadUrl = table.Column<string>(nullable: true),
                    BlobUrl = table.Column<string>(nullable: true),
                    Md5hash = table.Column<string>(nullable: true),
                    FileStatus = table.Column<byte>(nullable: false),
                    Quality = table.Column<byte>(nullable: false),
                    FramesPerSecond = table.Column<byte>(nullable: false),
                    DemoFileWorkerStatus = table.Column<byte>(nullable: false),
                    DemoFileWorkerVersion = table.Column<string>(nullable: true),
                    DatabaseVersion = table.Column<string>(nullable: true),
                    UploadDate = table.Column<DateTime>(nullable: false),
                    Event = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demo", x => x.MatchId);
                });

            migrationBuilder.CreateTable(
                name: "InQueueDemo",
                columns: table => new
                {
                    MatchId = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UploaderId = table.Column<long>(nullable: false),
                    MatchDate = table.Column<DateTime>(nullable: false),
                    InsertDate = table.Column<DateTime>(nullable: false),
                    DDQUEUE = table.Column<bool>(nullable: false),
                    DFWQUEUE = table.Column<bool>(nullable: false),
                    SOQUEUE = table.Column<bool>(nullable: false),
                    Retries = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InQueueDemo", x => x.MatchId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Demo");

            migrationBuilder.DropTable(
                name: "InQueueDemo");
        }
    }
}
