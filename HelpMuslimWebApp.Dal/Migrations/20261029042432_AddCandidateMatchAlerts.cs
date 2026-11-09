using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateMatchAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "match_alerts_disabled",
                table: "recruiter_profile",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "match_alerts_token",
                table: "recruiter_profile",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "candidate_match_alert",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_offer_id = table.Column<int>(type: "integer", nullable: false),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_match_alert", x => x.id);
                    table.ForeignKey(
                        name: "FK_candidate_match_alert_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidate_match_alert_job_offer_job_offer_id",
                        column: x => x.job_offer_id,
                        principalTable: "job_offer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_profile_match_alerts_token",
                table: "recruiter_profile",
                column: "match_alerts_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidate_match_alert_candidate_profile_id",
                table: "candidate_match_alert",
                column: "candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_match_alert_job_offer_id_candidate_profile_id",
                table: "candidate_match_alert",
                columns: new[] { "job_offer_id", "candidate_profile_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidate_match_alert");

            migrationBuilder.DropIndex(
                name: "IX_recruiter_profile_match_alerts_token",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "match_alerts_disabled",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "match_alerts_token",
                table: "recruiter_profile");
        }
    }
}
