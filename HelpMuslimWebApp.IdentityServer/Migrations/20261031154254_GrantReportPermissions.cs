using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.IdentityServer.Migrations
{
    // Nouveaux droits « Signalements » : donnés aux rôles qui modèrent déjà les messages (messages.view)
    /// <inheritdoc />
    public partial class GrantReportPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in new[] { "reports.view", "reports.manage" })
                migrationBuilder.Sql($"""
                    INSERT INTO "AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue")
                    SELECT DISTINCT rc."RoleId", 'permission', '{permission}'
                    FROM "AspNetRoleClaims" rc
                    WHERE rc."ClaimType" = 'permission' AND rc."ClaimValue" = 'messages.view'
                      AND NOT EXISTS (SELECT 1 FROM "AspNetRoleClaims" x
                                      WHERE x."RoleId" = rc."RoleId" AND x."ClaimType" = 'permission' AND x."ClaimValue" = '{permission}');
                    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "AspNetRoleClaims"
                WHERE "ClaimType" = 'permission' AND "ClaimValue" IN ('reports.view', 'reports.manage');
                """);
        }
    }
}
