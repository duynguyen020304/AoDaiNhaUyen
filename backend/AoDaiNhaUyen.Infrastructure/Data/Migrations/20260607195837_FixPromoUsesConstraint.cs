using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixPromoUsesConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_promo_uses",
                table: "promo_codes");

            migrationBuilder.AddCheckConstraint(
                name: "ck_promo_uses",
                table: "promo_codes",
                sql: "current_uses >= 0 AND (max_uses = 0 OR current_uses <= max_uses)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_promo_uses",
                table: "promo_codes");

            migrationBuilder.AddCheckConstraint(
                name: "ck_promo_uses",
                table: "promo_codes",
                sql: "current_uses >= 0 AND current_uses <= max_uses");
        }
    }
}
