using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddPremium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "premium_cancel_at_period_end",
                table: "candidate_profile",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "premium_granted_until",
                table: "candidate_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "premium_paid_until",
                table: "candidate_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_customer_id",
                table: "candidate_profile",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_subscription_id",
                table: "candidate_profile",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "premium_setting",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    candidate_monthly_price_cents = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premium_setting", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "premium_setting",
                columns: new[] { "id", "candidate_monthly_price_cents", "is_enabled", "updated_at" },
                values: new object[] { 1, 699, false, null });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profile_stripe_customer_id",
                table: "candidate_profile",
                column: "stripe_customer_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "premium_setting");

            migrationBuilder.DropIndex(
                name: "IX_candidate_profile_stripe_customer_id",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "premium_cancel_at_period_end",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "premium_granted_until",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "premium_paid_until",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "stripe_customer_id",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "stripe_subscription_id",
                table: "candidate_profile");
        }
    }
}
