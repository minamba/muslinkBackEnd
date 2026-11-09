using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferFilledAndDiscussion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "filled_at",
                table: "job_offer",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "in_discussion_at",
                table: "job_offer",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_filled",
                table: "job_offer",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "offer_fill",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_offer_id = table.Column<int>(type: "integer", nullable: true),
                    recruiter_profile_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    company_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contract_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    filled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offer_fill", x => x.id);
                    table.ForeignKey(
                        name: "FK_offer_fill_job_offer_job_offer_id",
                        column: x => x.job_offer_id,
                        principalTable: "job_offer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_offer_fill_recruiter_profile_recruiter_profile_id",
                        column: x => x.recruiter_profile_id,
                        principalTable: "recruiter_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_offer_is_filled_filled_at",
                table: "job_offer",
                columns: new[] { "is_filled", "filled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_offer_fill_filled_at",
                table: "offer_fill",
                column: "filled_at");

            migrationBuilder.CreateIndex(
                name: "IX_offer_fill_job_offer_id",
                table: "offer_fill",
                column: "job_offer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offer_fill_recruiter_profile_id",
                table: "offer_fill",
                column: "recruiter_profile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "offer_fill");

            migrationBuilder.DropIndex(
                name: "IX_job_offer_is_filled_filled_at",
                table: "job_offer");

            migrationBuilder.DropColumn(
                name: "filled_at",
                table: "job_offer");

            migrationBuilder.DropColumn(
                name: "in_discussion_at",
                table: "job_offer");

            migrationBuilder.DropColumn(
                name: "is_filled",
                table: "job_offer");
        }
    }
}
