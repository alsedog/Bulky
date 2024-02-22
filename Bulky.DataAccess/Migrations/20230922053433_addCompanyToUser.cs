using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bulky.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addCompanyToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "ComapnyId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "City", "Name", "PhoneNumber", "PostalCode", "State", "StreetAddress" },
                values: new object[,]
                {
                    { 1001, "Tech City", "Tech Solution", "090078601", "12121", "IL", "123 Tech St" },
                    { 1002, "Techville", "Another Name for Tech Solution", "090012345", "54321", "CA", "456 Tech St" },
                    { 1003, "Techland", "Yet Another Name for Tech Solution", "090098765", "98765", "NY", "789 Tech St" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ComapnyId",
                table: "AspNetUsers",
                column: "ComapnyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Companies_ComapnyId",
                table: "AspNetUsers",
                column: "ComapnyId",
                principalTable: "Companies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Companies_ComapnyId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ComapnyId",
                table: "AspNetUsers");

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DropColumn(
                name: "ComapnyId",
                table: "AspNetUsers");

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "City", "Name", "PhoneNumber", "PostalCode", "State", "StreetAddress" },
                values: new object[,]
                {
                    { 1, "Tech City", "Tech Solution", "090078601", "12121", "IL", "123 Tech St" },
                    { 2, "Techville", "Another Name for Tech Solution", "090012345", "54321", "CA", "456 Tech St" },
                    { 3, "Techland", "Yet Another Name for Tech Solution", "090098765", "98765", "NY", "789 Tech St" }
                });
        }
    }
}
