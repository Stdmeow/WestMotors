using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WestMotors.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationRequestNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationRequests_Clients_ClientId",
                table: "ApplicationRequests");

            migrationBuilder.AlterColumn<string>(
                name: "RequestType",
                table: "ApplicationRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "ApplicationRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ClientFullName",
                table: "ApplicationRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactInfo",
                table: "ApplicationRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreferredContactMethod",
                table: "ApplicationRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "ApplicationRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationRequests_Clients_ClientId",
                table: "ApplicationRequests",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationRequests_Clients_ClientId",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "ClientFullName",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "ContactInfo",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "PreferredContactMethod",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "ApplicationRequests");

            migrationBuilder.AlterColumn<string>(
                name: "RequestType",
                table: "ApplicationRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "ApplicationRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationRequests_Clients_ClientId",
                table: "ApplicationRequests",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
