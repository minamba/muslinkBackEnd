using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterPro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // L'interrupteur existant devient celui des candidats ; celui des recruteurs est nouveau (coupé)
            migrationBuilder.RenameColumn(
                name: "is_enabled",
                table: "premium_setting",
                newName: "candidates_enabled");

            migrationBuilder.AddColumn<bool>(
                name: "is_founder",
                table: "recruiter_profile",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_free_sponsor_at",
                table: "recruiter_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "pro_cancel_at_period_end",
                table: "recruiter_profile",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pro_granted_until",
                table: "recruiter_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pro_paid_until",
                table: "recruiter_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_customer_id",
                table: "recruiter_profile",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_subscription_id",
                table: "recruiter_profile",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "recruiters_enabled",
                table: "premium_setting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "recruiter_monthly_price_cents",
                table: "premium_setting",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "recruiter_yearly_price_cents",
                table: "premium_setting",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sponsored_offer_price_cents",
                table: "premium_setting",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "sponsored_until",
                table: "job_offer",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "views_count",
                table: "job_offer",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "premium_setting",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "recruiter_monthly_price_cents", "recruiter_yearly_price_cents", "sponsored_offer_price_cents" },
                values: new object[] { 4900, 49000, 2900 });

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_profile_stripe_customer_id",
                table: "recruiter_profile",
                column: "stripe_customer_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recruiter_profile_stripe_customer_id",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "is_founder",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "last_free_sponsor_at",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "pro_cancel_at_period_end",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "pro_granted_until",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "pro_paid_until",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "stripe_customer_id",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "stripe_subscription_id",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "recruiters_enabled",
                table: "premium_setting");

            migrationBuilder.DropColumn(
                name: "recruiter_monthly_price_cents",
                table: "premium_setting");

            migrationBuilder.DropColumn(
                name: "recruiter_yearly_price_cents",
                table: "premium_setting");

            migrationBuilder.DropColumn(
                name: "sponsored_offer_price_cents",
                table: "premium_setting");

            migrationBuilder.DropColumn(
                name: "sponsored_until",
                table: "job_offer");

            migrationBuilder.DropColumn(
                name: "views_count",
                table: "job_offer");

            migrationBuilder.RenameColumn(
                name: "candidates_enabled",
                table: "premium_setting",
                newName: "is_enabled");
        }
    }
}
