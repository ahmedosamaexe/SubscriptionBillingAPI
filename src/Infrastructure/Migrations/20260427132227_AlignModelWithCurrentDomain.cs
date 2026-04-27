using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionBillingAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignModelWithCurrentDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsageLogs_UserId",
                table: "UsageLogs");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "UsageLogs");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "UsageLogs");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "UsageLogs",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Metric",
                table: "UsageLogs",
                newName: "Action");

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Count",
                table: "UsageLogs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "UsageLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "UsageLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "UsageLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GracePeriodEndDate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAt",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentRetryCount",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Subscriptions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRequests",
                table: "Plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "Plans",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "MaxRequests", "StripePriceId" },
                values: new object[] { 100, null });

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "MaxRequests", "StripePriceId" },
                values: new object[] { 10000, null });

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "MaxRequests", "StripePriceId" },
                values: new object[] { 0, null });

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_UserId_Action_Month_Year",
                table: "UsageLogs",
                columns: new[] { "UserId", "Action", "Month", "Year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UsageLogs_UserId_Action_Month_Year",
                table: "UsageLogs");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Count",
                table: "UsageLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "UsageLogs");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "UsageLogs");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "UsageLogs");

            migrationBuilder.DropColumn(
                name: "GracePeriodEndDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PausedAt",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentRetryCount",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "MaxRequests",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "Plans");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "UsageLogs",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "UsageLogs",
                newName: "Metric");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "UsageLogs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "UsageLogs",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_UsageLogs_UserId",
                table: "UsageLogs",
                column: "UserId");
        }
    }
}
