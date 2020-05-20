using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class refactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            #region Current Queue Information
            // No custom migration code added for this section under the premise that we will truncate the InQueueDemo table when applying this migration.

            // These are replaced by 
            migrationBuilder.DropColumn(
                name: "DDQUEUE",
                table: "InQueueDemo");

            migrationBuilder.DropColumn(
                name: "DFWQUEUE",
                table: "InQueueDemo");

            migrationBuilder.DropColumn(
                name: "SOQUEUE",
                table: "InQueueDemo");

            // This column holding a value from enum `Queue`
            migrationBuilder.AddColumn<byte>(
                name: "CurrentQueue",
                table: "InQueueDemo",
                nullable: false,
                defaultValue: (byte)0);

            #endregion

            #region Replacement
            // No custom migration code added for this section
            //under the premise that we will truncate the InQueueDemo table when applying this migration.

            // Replaced with 
            migrationBuilder.DropColumn(
                name: "Retries",
                table: "InQueueDemo");

            // This
            migrationBuilder.AddColumn<int>(
                name: "RetryAttemptsOnCurrentFailure",
                table: "InQueueDemo",
                nullable: false,
                defaultValue: 0);
            #endregion

            #region Dropped because not in use

            migrationBuilder.DropColumn(
                name: "MatchDate",
                table: "InQueueDemo");

            migrationBuilder.DropColumn(
                name: "UploaderId",
                table: "InQueueDemo");

            migrationBuilder.DropColumn(
                name: "DatabaseVersion",
                table: "Demo");
            
            migrationBuilder.DropColumn(
                name: "DemoFileWorkerVersion",
                table: "Demo");

            migrationBuilder.DropColumn(
                name: "Event",
                table: "Demo");

            #endregion

            #region Analysis Status
            migrationBuilder.AddColumn<byte>(
                name: "AnalysisStatus",
                table: "Demo",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "AnalysisBlockReason",
                table: "Demo",
                nullable: true);


            #region Model Reference
            // GeneticStatus / UploadStatus:
            //     Unknown = 0,
            //     Success = 10,
            //     Failure = 20,

            // DemoFileWorkerStatus 
            //     New = 0,
            //     InQueue = 1,
            //     Finished = 2,

            //     BlobStorageDownloadFailed = 31,
            //     UnzipFailed = 32,
            //     DuplicateCheckFailed = 33,
            //     AnalyzerFailed = 34, 
            //     CacheUploadFailed = 35,
            #endregion


            // Convert DemoFileWorkerStatus to DemoAnalysisBlock
            migrationBuilder.Sql(
            @"
                UPDATE Demo
                SET DemoAnalysisBlock = CASE
                    WHEN DemoFileWorkerStatus = 0 THEN NULL
                    WHEN DemoFileWorkerStatus = 1 THEN NULL
                    WHEN DemoFileWorkerStatus = 2 THEN NULL
                    WHEN DemoFileWorkerStatus = 31 THEN 2010
                    WHEN DemoFileWorkerStatus = 32 THEN 2020
                    WHEN DemoFileWorkerStatus = 33 THEN 2030
                    WHEN DemoFileWorkerStatus = 34 THEN 2050
                    WHEN DemoFileWorkerStatus = 35 THEN 2070
                    ELSE 2000
                    END
            ");

            // Convert UploadStatus to AnalysisStatus
            // If it has succeeded and DemoFileWorkerStatus is `Finished` = Success
            // Else consider it a Failure.
            migrationBuilder.Sql(
            @"
                UPDATE Demo
                SET AnalysisStatus = CASE
                    WHEN UploadStatus = 10 AND DemoFileWorkerStatus == 2 THEN 10
                    ELSE 20
                    END
            ");

            migrationBuilder.DropColumn(
                name: "UploadStatus",
                table: "Demo");

            migrationBuilder.DropColumn(
                name: "DemoFileWorkerStatus",
                table: "Demo");

            # endregion

            migrationBuilder.RenameColumn(
                name: "Md5hash",
                table: "Demo",
                newName: "MD5Hash");

            migrationBuilder.AlterColumn<long>(
                name: "MatchId",
                table: "InQueueDemo",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<byte>(
                name: "CurrentQueue",
                table: "InQueueDemo",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddForeignKey(
                name: "FK_InQueueDemo_Demo_MatchId",
                table: "InQueueDemo",
                column: "MatchId",
                principalTable: "Demo",
                principalColumn: "MatchId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InQueueDemo_Demo_MatchId",
                table: "InQueueDemo");

            migrationBuilder.DropColumn(
                name: "CurrentQueue",
                table: "InQueueDemo");

            migrationBuilder.DropColumn(
                name: "RetryAttemptsOnCurrentFailure",
                table: "InQueueDemo");

            migrationBuilder.DropColumn(
                name: "AnalysisStatus",
                table: "Demo");

            migrationBuilder.DropColumn(
                name: "DemoAnalysisBlock",
                table: "Demo");

            migrationBuilder.RenameColumn(
                name: "MD5Hash",
                table: "Demo",
                newName: "Md5hash");

            migrationBuilder.AlterColumn<long>(
                name: "MatchId",
                table: "InQueueDemo",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long))
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddColumn<bool>(
                name: "DDQUEUE",
                table: "InQueueDemo",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DFWQUEUE",
                table: "InQueueDemo",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MatchDate",
                table: "InQueueDemo",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Retries",
                table: "InQueueDemo",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "SOQUEUE",
                table: "InQueueDemo",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "UploaderId",
                table: "InQueueDemo",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseVersion",
                table: "Demo",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "DemoFileWorkerStatus",
                table: "Demo",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "DemoFileWorkerVersion",
                table: "Demo",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Event",
                table: "Demo",
                type: "longtext CHARACTER SET utf8mb4",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "UploadStatus",
                table: "Demo",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);
        }
    }
}
