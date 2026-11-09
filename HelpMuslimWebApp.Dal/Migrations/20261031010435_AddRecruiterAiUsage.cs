using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterAiUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recruiter_ai_usage",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recruiter_profile_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    feature = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    model = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recruiter_ai_usage", x => x.id);
                    table.ForeignKey(
                        name: "FK_recruiter_ai_usage_recruiter_profile_recruiter_profile_id",
                        column: x => x.recruiter_profile_id,
                        principalTable: "recruiter_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_ai_usage_created_at",
                table: "recruiter_ai_usage",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_recruiter_ai_usage_recruiter_profile_id_created_at",
                table: "recruiter_ai_usage",
                columns: new[] { "recruiter_profile_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recruiter_ai_usage");
        }
    }
}
