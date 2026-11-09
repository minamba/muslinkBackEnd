using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddCvCoach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cv_coach_analysis",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    cv_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    result = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_coach_analysis", x => x.id);
                    table.ForeignKey(
                        name: "FK_cv_coach_analysis_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cv_coach_analysis_candidate_profile_id_created_at",
                table: "cv_coach_analysis",
                columns: new[] { "candidate_profile_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cv_coach_analysis");
        }
    }
}
