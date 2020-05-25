using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class analysissucceeded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnalysisSucceeded",
                table: "Demo",
                nullable: false,
                defaultValue: false);


            // GeneticStatus / UploadStatus:
            //     Unknown = 0,
            //     Success = 10,
            //     Failure = 20,

            migrationBuilder.Sql(
            @"
                UPDATE Demo
                SET AnalysisSucceeded = CASE
                    WHEN AnalysisStatus = 0 THEN NULL
                    WHEN AnalysisStatus = 10 THEN TRUE
                    WHEN AnalysisStatus = 20 THEN FALSE
                    ELSE NULL
                    END
            ");

            migrationBuilder.DropColumn(
                name: "AnalysisStatus",
                table: "Demo");

            migrationBuilder.AlterColumn<int>(
                name: "AnalysisBlockReason",
                table: "Demo",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisSucceeded",
                table: "Demo");

            migrationBuilder.AlterColumn<int>(
                name: "AnalysisBlockReason",
                table: "Demo",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "AnalysisStatus",
                table: "Demo",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);
        }
    }
}
