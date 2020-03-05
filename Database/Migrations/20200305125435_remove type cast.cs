using Microsoft.EntityFrameworkCore.Migrations;

namespace Database.Migrations
{
    public partial class removetypecast : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Md5hash",
                table: "Demo",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DownloadUrl",
                table: "Demo",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BlobUrl",
                table: "Demo",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Md5hash",
                table: "Demo",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DownloadUrl",
                table: "Demo",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BlobUrl",
                table: "Demo",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
