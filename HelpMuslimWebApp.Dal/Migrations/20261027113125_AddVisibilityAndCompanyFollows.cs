using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddVisibilityAndCompanyFollows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_hidden",
                table: "job_offer",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_hidden",
                table: "candidate_profile",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "company_follow",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    recruiter_profile_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_follow", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_follow_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_company_follow_recruiter_profile_recruiter_profile_id",
                        column: x => x.recruiter_profile_id,
                        principalTable: "recruiter_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_follow_candidate_profile_id_recruiter_profile_id",
                table: "company_follow",
                columns: new[] { "candidate_profile_id", "recruiter_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_follow_recruiter_profile_id",
                table: "company_follow",
                column: "recruiter_profile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_follow");

            migrationBuilder.DropColumn(
                name: "is_hidden",
                table: "job_offer");

            migrationBuilder.DropColumn(
                name: "is_hidden",
                table: "candidate_profile");
        }
    }
}
