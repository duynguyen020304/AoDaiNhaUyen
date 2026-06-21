using AoDaiNhaUyen.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260621053200_AddSafeEmailTemplateConfig")]
    public partial class AddSafeEmailTemplateConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "template_type",
                table: "email_templates",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "legacy.html");

            migrationBuilder.AddColumn<string>(
                name: "config_json",
                table: "email_templates",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "email_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                INSERT INTO email_templates (id, key, name, subject, preheader, html_body, text_body, template_type, config_json, is_system, locale, version, is_active, is_deleted, created_at, updated_at)
                VALUES
                  ('0f6e85a3-53e3-4f21-9b61-c8754ad98801', 'marketing.promo', 'Khuyến mãi', 'Ưu đãi áo dài dành riêng cho bạn', 'Khám phá ưu đãi mới nhất từ Áo Dài Nhã Uyên', '', NULL, 'marketing.promo', '{"heading":"Ưu đãi áo dài cuối tuần","intro":"Một lựa chọn tinh tế cho những khoảnh khắc đặc biệt.","body":"Nhận ưu đãi cho các thiết kế áo dài mới, chất liệu mềm mại và phom dáng tôn nét Việt.","ctaText":"Xem ưu đãi","ctaUrl":"https://aodainhauyen.io.vn/products","footerNote":"Ưu đãi có thể kết thúc sớm khi hết số lượng."}'::jsonb, true, 'vi-VN', 1, true, false, now(), now()),
                  ('2b66d2d1-1ac8-4cf9-8932-bd5179e61131', 'marketing.newsletter', 'Newsletter', 'Bản tin Áo Dài Nhã Uyên', 'Cảm hứng mặc đẹp và câu chuyện áo dài mới nhất', '', NULL, 'marketing.newsletter', '{"heading":"Cảm hứng áo dài trong tuần","intro":"Những gợi ý phối áo dài, câu chuyện chất liệu và thiết kế mới.","body":"Nhã Uyên chọn lọc các thiết kế trang nhã cho sự kiện gia đình, lễ hội và khoảnh khắc thường ngày.","ctaText":"Đọc thêm","ctaUrl":"https://aodainhauyen.com/blog"}'::jsonb, true, 'vi-VN', 1, true, false, now(), now()),
                  ('d211d105-338f-42a1-8669-c28f9f7f7b95', 'subscriber.welcome', 'Chào mừng đăng ký nhận tin', 'Chào mừng bạn đến với Áo Dài Nhã Uyên', 'Cảm ơn bạn đã gia nhập cộng đồng yêu áo dài', '', NULL, 'subscriber.welcome', '{"heading":"Chào mừng bạn đến với Áo Dài Nhã Uyên","intro":"Cảm ơn bạn đã đăng ký nhận tin.","body":"Bạn sẽ nhận cảm hứng mặc đẹp, mẹo chăm sóc áo dài và ưu đãi riêng.","ctaText":"Khám phá bộ sưu tập","ctaUrl":"https://aodainhauyen.io.vn/products"}'::jsonb, true, 'vi-VN', 1, true, false, now(), now()),
                  ('32d0b417-0534-42a7-99d8-9a1e104491cc', 'order.confirmation', 'Xác nhận đơn hàng', 'Nhã Uyên đã nhận đơn hàng của bạn', 'Thông tin đơn hàng và bước xử lý tiếp theo', '', NULL, 'order.confirmation', '{"heading":"Xác nhận đơn hàng","intro":"Cảm ơn bạn đã tin chọn Áo Dài Nhã Uyên.","body":"Chúng tôi đã nhận được đơn hàng và sẽ liên hệ khi đơn được xử lý.","ctaText":"Xem đơn hàng","ctaUrl":"https://aodainhauyen.com/account/orders","orderCode":"ADNU-2026-0001"}'::jsonb, true, 'vi-VN', 1, true, false, now(), now())
                ON CONFLICT (key, locale, version) DO UPDATE SET
                  name = EXCLUDED.name,
                  subject = EXCLUDED.subject,
                  preheader = EXCLUDED.preheader,
                  template_type = EXCLUDED.template_type,
                  config_json = EXCLUDED.config_json,
                  is_system = true,
                  is_active = true,
                  is_deleted = false,
                  updated_at = now();
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM email_templates
                WHERE id IN (
                  '0f6e85a3-53e3-4f21-9b61-c8754ad98801',
                  '2b66d2d1-1ac8-4cf9-8932-bd5179e61131',
                  'd211d105-338f-42a1-8669-c28f9f7f7b95',
                  '32d0b417-0534-42a7-99d8-9a1e104491cc'
                );
                """);

            migrationBuilder.DropColumn(name: "template_type", table: "email_templates");
            migrationBuilder.DropColumn(name: "config_json", table: "email_templates");
            migrationBuilder.DropColumn(name: "is_system", table: "email_templates");
        }
    }
}
