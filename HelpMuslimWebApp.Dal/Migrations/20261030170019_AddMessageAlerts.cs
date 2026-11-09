using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "message_alerts_disabled",
                table: "recruiter_profile",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "candidate_alerted_at",
                table: "conversation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "recruiter_alerted_at",
                table: "conversation",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "message_alerts_disabled",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "candidate_alerted_at",
                table: "conversation");

            migrationBuilder.DropColumn(
                name: "recruiter_alerted_at",
                table: "conversation");
        }
    }
}
