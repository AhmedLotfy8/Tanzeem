using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tanzeem.Persistence.Data.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscription_Users_UserId",
                table: "Subscription");

            migrationBuilder.DropIndex(
                name: "IX_Users_StripeCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Subscription");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Subscription",
                newName: "CompanyId");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "Subscription",
                newName: "CurrentPeriodEnd");

            migrationBuilder.RenameIndex(
                name: "IX_Subscription_UserId",
                table: "Subscription",
                newName: "IX_Subscription_CompanyId");

            migrationBuilder.AlterColumn<string>(
                name: "StripeSubscriptionId",
                table: "Subscription",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Subscription",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "Plan",
                table: "Subscription",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Subscription",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscription_Companies_CompanyId",
                table: "Subscription",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subscription_Companies_CompanyId",
                table: "Subscription");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Subscription");

            migrationBuilder.RenameColumn(
                name: "CurrentPeriodEnd",
                table: "Subscription",
                newName: "StartedAt");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "Subscription",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Subscription_CompanyId",
                table: "Subscription",
                newName: "IX_Subscription_UserId");

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StripeSubscriptionId",
                table: "Subscription",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Subscription",
                type: "int",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "Plan",
                table: "Subscription",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Subscription",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Users_StripeCustomerId",
                table: "Users",
                column: "StripeCustomerId",
                unique: true,
                filter: "[StripeCustomerId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Subscription_Users_UserId",
                table: "Subscription",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
