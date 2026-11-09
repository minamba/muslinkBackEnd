using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "offer_favorite",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    candidate_profile_id = table.Column<int>(type: "integer", nullable: false),
                    job_offer_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_offer_favorite", x => x.id);
                    table.ForeignKey(
                        name: "FK_offer_favorite_candidate_profile_candidate_profile_id",
                        column: x => x.candidate_profile_id,
                        principalTable: "candidate_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_offer_favorite_job_offer_job_offer_id",
                        column: x => x.job_offer_id,
                        principalTable: "job_offer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_offer_favorite_candidate_profile_id_job_offer_id",
                table: "offer_favorite",
                columns: new[] { "candidate_profile_id", "job_offer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_offer_favorite_job_offer_id",
                table: "offer_favorite",
                column: "job_offer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "offer_favorite");
        }
    }
}
