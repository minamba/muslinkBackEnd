using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpMuslimWebApp.IdentityServer.Migrations
{
    // Le tableau de bord a désormais un droit par onglet. Les rôles existants gardent ce qu'ils voyaient :
    // « Vue d'ensemble » (ouverte à tout administrateur jusque-là) pour tout rôle qui a au moins un droit,
    // « Abonnements » (rattaché à « Fréquentation » jusque-là) pour les rôles qui ont stats.view.
    // Frais, Gains, Anthropic, coach, mode test et démo étaient réservés au super admin : aucun rôle ne les reçoit.
    /// <inheritdoc />
    public partial class GrantDashboardPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT rc."RoleId", 'permission', 'dashboard.overview'
                FROM "AspNetRoleClaims" rc
                WHERE rc."ClaimType" = 'permission'
                  AND NOT EXISTS (SELECT 1 FROM "AspNetRoleClaims" x
                                  WHERE x."RoleId" = rc."RoleId" AND x."ClaimType" = 'permission' AND x."ClaimValue" = 'dashboard.overview');
                """);
            migrationBuilder.Sql("""
                INSERT INTO "AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT rc."RoleId", 'permission', 'dashboard.subscriptions'
                FROM "AspNetRoleClaims" rc
                WHERE rc."ClaimType" = 'permission' AND rc."ClaimValue" = 'stats.view'
                  AND NOT EXISTS (SELECT 1 FROM "AspNetRoleClaims" x
                                  WHERE x."RoleId" = rc."RoleId" AND x."ClaimType" = 'permission' AND x."ClaimValue" = 'dashboard.subscriptions');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "AspNetRoleClaims"
                WHERE "ClaimType" = 'permission' AND "ClaimValue" IN ('dashboard.overview', 'dashboard.subscriptions');
                """);
        }
    }
}
