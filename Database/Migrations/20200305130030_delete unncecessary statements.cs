using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class deleteunncecessarystatements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InQueue",
                table: "InQueue");

            migrationBuilder.RenameTable(
                name: "InQueue",
                newName: "InQueueDemo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InQueueDemo",
                table: "InQueueDemo",
                column: "MatchId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_InQueueDemo",
                table: "InQueueDemo");

            migrationBuilder.RenameTable(
                name: "InQueueDemo",
                newName: "InQueue");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InQueue",
                table: "InQueue",
                column: "MatchId");
        }
    }
}
