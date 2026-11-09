using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_id = table.Column<int>(type: "integer", nullable: false),
                    target_label = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    author_user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    author_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    author_candidate_profile_id = table.Column<int>(type: "integer", nullable: true),
                    author_recruiter_profile_id = table.Column<int>(type: "integer", nullable: true),
                    author_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    author_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    justification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    admin_note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    handled_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_candidate_profile_author_candidate_profile_id",
                        column: x => x.author_candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_recruiter_profile_author_recruiter_profile_id",
                        column: x => x.author_recruiter_profile_id,
                        principalTable: "recruiter_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_author_candidate_profile_id",
                table: "report",
                column: "author_candidate_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_author_recruiter_profile_id",
                table: "report",
                column: "author_recruiter_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_author_user_id_target_type_target_id",
                table: "report",
                columns: new[] { "author_user_id", "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_report_target_type_status_created_at",
                table: "report",
                columns: new[] { "target_type", "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report");
        }
    }
}
