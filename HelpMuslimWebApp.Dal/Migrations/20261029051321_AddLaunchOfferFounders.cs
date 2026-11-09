using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddLaunchOfferFounders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "founder_revoked_at",
                table: "recruiter_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "founder_since",
                table: "recruiter_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "identity_verified_at",
                table: "recruiter_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_email_confirmed",
                table: "recruiter_profile",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_name",
                table: "recruiter_profile",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rna",
                table: "recruiter_profile",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "signup_ip_hash",
                table: "recruiter_profile",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siren",
                table: "recruiter_profile",
                type: "character varying(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "siret",
                table: "recruiter_profile",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "founder_auto_grant",
                table: "premium_setting",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "founder_slots",
                table: "premium_setting",
                type: "integer",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<bool>(
                name: "launch_offer_enabled",
                table: "premium_setting",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "premium_setting",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "founder_auto_grant", "founder_slots", "launch_offer_enabled" },
                values: new object[] { true, 50, false });

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_profile_rna",
                table: "recruiter_profile",
                column: "rna",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_profile_siren",
                table: "recruiter_profile",
                column: "siren",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recruiter_profile_rna",
                table: "recruiter_profile");

            migrationBuilder.DropIndex(
                name: "IX_recruiter_profile_siren",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "founder_revoked_at",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "founder_since",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "identity_verified_at",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "is_email_confirmed",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "legal_name",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "rna",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "signup_ip_hash",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "siren",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "siret",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "founder_auto_grant",
                table: "premium_setting");

            migrationBuilder.DropColumn(
                name: "founder_slots",
                table: "premium_setting");

            migrationBuilder.DropColumn(
                name: "launch_offer_enabled",
                table: "premium_setting");
        }
    }
}
