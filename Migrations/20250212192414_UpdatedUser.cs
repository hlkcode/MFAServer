using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MFAServer.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Surname",
                table: "AspNetUsers",
                newName: "SecretKey");

            migrationBuilder.RenameColumn(
                name: "OtherNames",
                table: "AspNetUsers",
                newName: "FullName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SecretKey",
                table: "AspNetUsers",
                newName: "Surname");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "AspNetUsers",
                newName: "OtherNames");
        }
    }
}
