using AoDaiNhaUyen.Infrastructure.Data;

using Microsoft.EntityFrameworkCore.Infrastructure;

using Microsoft.EntityFrameworkCore.Migrations;


#nullable disable


namespace AoDaiNhaUyen.Infrastructure.Data.Migrations

{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260621090000_MigrateAuthAndInvoiceTemplatesToDatabase")]
    public partial class MigrateAuthAndInvoiceTemplatesToDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO email_templates (id, key, name, subject, preheader, html_body, text_body, template_type, config_json, is_system, locale, version, is_active, is_deleted, created_at, updated_at)
                VALUES
                  ('a1b2c3d4-e5f6-7890-abcd-ef1234567890', 'auth.verify_email', 'Xác thực tài khoản', 'Xác thực tài khoản Áo Dài Nhã Uyên', 'Vui lòng xác thực email để kích hoạt tài khoản', '', NULL, 'auth.verify_email', '{"greeting":"Xin chào","body":"Cảm ơn bạn đã tạo tài khoản. Vui lòng xác thực email để kích hoạt và đăng nhập.","buttonText":"Xác thực tài khoản","expiryInfo":"Liên kết có hiệu lực trong 24 giờ."}'::jsonb, true, 'vi-VN', 1, true, false, now(), now()),
                  
                  ('b2c3d4e5-f6a7-8901-bcde-f12345678901', 'auth.reset_password', 'Đặt lại mật khẩu', 'Đặt lại mật khẩu Áo Dài Nhã Uyên', 'Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.', '', NULL, 'auth.reset_password', '{"greeting":"Xin chào","body":"Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.","buttonText":"Đặt lại mật khẩu","info1":"Nếu bạn không yêu cầu, vui lòng bỏ qua email này.","info2":"Liên kết đặt lại mật khẩu có hiệu lực trong 24 giờ."}'::jsonb, true, 'vi-VN', 1, true, false, now(), now()),
                  
                  ('c3d4e5f6-a7b8-9012-cdef-123456789012', 'order.invoice', 'Hóa đơn đơn hàng', 'Hóa đơn đơn hàng {{orderCode}}', 'Thông tin chi tiết hóa đơn đơn hàng của bạn', '', NULL, 'order.invoice', '{"heading":"Hóa đơn đặt hàng","statusLabelPaid":"Đã thanh toán","statusLabelPending":"Chờ thanh toán","recipientLabel":"Người nhận","addressLabel":"Địa chỉ","itemsTableHeaders":["Sản phẩm","Phân loại","Số lượng","Đơn giá","Thành tiền"],"subtotalLabel":"Tạm tính","shippingFeeLabel":"Phí vận chuyển","totalLabel":"Tổng thanh toán"}'::jsonb, true, 'vi-VN', 1, true, false, now(), now()),
                  
                  ('d4e5f6a7-b8c9-0123-def0-234567890123', 'marketing.confirm_subscription', 'Xác nhận đăng ký nhận tin', 'Xác nhận đăng ký nhận tin từ Áo Dài Nhã Uyên', 'Vui lòng xác nhận email để hoàn tất đăng ký', '', NULL, 'marketing.confirm_subscription', '{"greeting":"Chào bạn","body":"Cảm ơn bạn đã đăng ký nhận tin từ Áo Dài Nhã Uyên. Vui lòng xác nhận email để hoàn tất đăng ký.","buttonText":"Xác nhận đăng ký","info":"Nếu nút không hoạt động, vui lòng sao chép liên kết bên dưới."}'::jsonb, true, 'vi-VN', 1, true, false, now(), now())
                
                ON CONFLICT (key, locale, version) DO UPDATE SET
                  name = EXCLUDED.name,
                  subject = EXCLUDED.subject,
                  preheader = EXCLUDED.preheader,
                  html_body = EXCLUDED.html_body,
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
                  'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                  'b2c3d4e5-f6a7-8901-bcde-f12345678901',
                  'c3d4e5f6-a7b8-9012-cdef-123456789012',
                  'd4e5f6a7-b8c9-0123-def0-234567890123'
                );
                """);
        }
    }
}