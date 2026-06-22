using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    [Migration("20260622105000_UpdateCreateVariantRisk")]
    public partial class UpdateCreateVariantRisk : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE tool_risk_configs
                SET risk_level = 'Medium',
                    requires_confirmation = TRUE,
                    updated_at = NOW()
                WHERE tool_name = 'create_variant';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE tool_risk_configs
                SET risk_level = 'Low',
                    requires_confirmation = FALSE,
                    updated_at = NOW()
                WHERE tool_name = 'create_variant';
                """);
        }
    }
}
