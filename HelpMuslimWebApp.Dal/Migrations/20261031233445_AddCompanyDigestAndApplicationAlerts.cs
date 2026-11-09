using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDigestAndApplicationAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "application_alerts_disabled",
                table: "recruiter_profile",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "company_offer_digest",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    job_offer_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_offer_digest", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_offer_digest_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_company_offer_digest_job_offer_job_offer_id",
                        column: x => x.job_offer_id,
                        principalTable: "job_offer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_offer_digest_candidate_profile_id",
                table: "company_offer_digest",
                column: "candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_company_offer_digest_job_offer_id_candidate_profile_id",
                table: "company_offer_digest",
                columns: new[] { "job_offer_id", "candidate_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_offer_digest_sent_at",
                table: "company_offer_digest",
                column: "sent_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_offer_digest");

            migrationBuilder.DropColumn(
                name: "application_alerts_disabled",
                table: "recruiter_profile");
        }
    }
}
