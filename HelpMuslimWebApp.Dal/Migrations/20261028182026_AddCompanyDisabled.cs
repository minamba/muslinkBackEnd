using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.Dal.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDisabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "disabled_at",
                table: "recruiter_profile",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_disabled",
                table: "recruiter_profile",
                type: "boolean",
                nullable: true,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "disabled_at",
                table: "recruiter_profile");

            migrationBuilder.DropColumn(
                name: "is_disabled",
                table: "recruiter_profile");
        }
    }
}
