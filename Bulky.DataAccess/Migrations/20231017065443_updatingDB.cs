using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bulky.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class updatingDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Companies_ComapnyId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "ComapnyId",
                table: "AspNetUsers",
                newName: "CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_ComapnyId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Companies_CompanyId",
                table: "AspNetUsers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Companies_CompanyId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "AspNetUsers",
                newName: "ComapnyId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_CompanyId",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_ComapnyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Companies_ComapnyId",
                table: "AspNetUsers",
                column: "ComapnyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }
    }
}
