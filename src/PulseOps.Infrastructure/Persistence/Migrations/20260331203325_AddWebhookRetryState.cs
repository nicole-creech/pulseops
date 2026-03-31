using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookRetryState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "WebhookDeliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAttempts",
                table: "WebhookDeliveries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAtUtc",
                table: "WebhookDeliveries",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastError",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "MaxAttempts",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                table: "WebhookDeliveries");
        }
    }
}
