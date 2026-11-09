using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyPage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "benefits",
                table: "recruiter_profile",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cover_path",
                table: "recruiter_profile",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "culture",
                table: "recruiter_profile",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "facebook_url",
                table: "recruiter_profile",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "founded_year",
                table: "recruiter_profile",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "instagram_url",
                table: "recruiter_profile",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "linkedin_url",
                table: "recruiter_profile",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "looking_for",
                table: "recruiter_profile",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "recruitment_steps",
                table: "recruiter_profile",
                type: "text[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "youtube_url",
                table: "recruiter_profile",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "company_media",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recruiter_profile_id = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    youtube_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    quote = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_media", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_media_recruiter_profile_recruiter_profile_id",
                        column: x => x.recruiter_profile_id,
                        principalTable: "recruiter_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_media_recruiter_profile_id_kind_position",
                table: "company_media",
                columns: new[] { "recruiter_profile_id", "kind", "position" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "company_media");

            migrationBuilder.DropColumn(
                name: "benefits",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "cover_path",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "culture",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "facebook_url",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "founded_year",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "instagram_url",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "linkedin_url",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "looking_for",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "recruitment_steps",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "youtube_url",
                table: "recruiter_profile");
        }
    }
}
