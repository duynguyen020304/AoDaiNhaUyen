using AoDaiNhaUyen.Infrastructure.Data;

using Microsoft.EntityFrameworkCore.Infrastructure;

using Microsoft.EntityFrameworkCore.Migrations;


#nullable disable


namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260621093000_FixEmailTemplateDomains")]
    public partial class FixEmailTemplateDomains : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE email_templates
                SET config_json = jsonb_set(
                    config_json,
                    '{ctaUrl}',
                    '"https://aodainhauyen.io.vn/products"'::jsonb,
                    false
                )
                WHERE key IN ('marketing.promo', 'subscriber.welcome');

                UPDATE email_templates
                SET config_json = jsonb_set(
                    config_json,
                    '{ctaUrl}',
                    '"https://aodainhauyen.io.vn/blog"'::jsonb,
                    false
                )
                WHERE key = 'marketing.newsletter';

                UPDATE email_templates
                SET config_json = jsonb_set(
                    config_json,
                    '{ctaUrl}',
                    '"https://aodainhauyen.io.vn/account/orders"'::jsonb,
                    false
                )
                WHERE key = 'order.confirmation';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE email_templates
                SET config_json = jsonb_set(
                    config_json,
                    '{ctaUrl}',
                    '"https://aodainhauyen.com/products"'::jsonb,
                    false
                )
                WHERE key IN ('marketing.promo', 'subscriber.welcome');

                UPDATE email_templates
                SET config_json = jsonb_set(
                    config_json,
                    '{ctaUrl}',
                    '"https://aodainhauyen.com/blog"'::jsonb,
                    false
                )
                WHERE key = 'marketing.newsletter';

                UPDATE email_templates
                SET config_json = jsonb_set(
                    config_json,
                    '{ctaUrl}',
                    '"https://aodainhauyen.com/account/orders"'::jsonb,
                    false
                )
                WHERE key = 'order.confirmation';
                """);
        }
    }
}