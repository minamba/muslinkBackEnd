using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateShareToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "share_contact",
                table: "candidate_profile",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "share_token",
                table: "candidate_profile",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profile_share_token",
                table: "candidate_profile",
                column: "share_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_candidate_profile_share_token",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "share_contact",
                table: "candidate_profile");

            migrationBuilder.DropColumn(
                name: "share_token",
                table: "candidate_profile");
        }
    }
}
