using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingAutomation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cost_price",
                table: "product_variants",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "email_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    template_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "queued"),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    preheader = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    html_body = table.Column<string>(type: "text", nullable: false),
                    text_body = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "vi-VN"),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "order_promo_cost_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    promo_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    discount_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    subtotal_before_discount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    shipping_fee_before_promo = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    shipping_fee_charged = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    shipping_subsidy = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_after_discount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    estimated_cost_of_goods = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    estimated_gross_profit_before_promo = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    estimated_gross_profit_after_promo = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    margin_loss = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    attribution_campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_promo_cost_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_promo_cost_snapshots_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_promo_cost_snapshots_promo_codes_promo_code_id",
                        column: x => x.promo_code_id,
                        principalTable: "promo_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "subscribers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    subscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unsubscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    unsubscribe_token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    confirmation_token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_open_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_click_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscribers", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscribers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "email_send_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    template_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_send_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_send_logs_email_jobs_email_job_id",
                        column: x => x.email_job_id,
                        principalTable: "email_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketing_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "email"),
                    is_opt_in = table.Column<bool>(type: "boolean", nullable: false),
                    source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    consent_version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "2026-01"),
                    consented_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ip_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    user_agent_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketing_consents", x => x.id);
                    table.ForeignKey(
                        name: "FK_marketing_consents_subscribers_subscriber_id",
                        column: x => x.subscriber_id,
                        principalTable: "subscribers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_marketing_consents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_email_jobs_status_scheduled_at",
                table: "email_jobs",
                columns: new[] { "status", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "IX_email_send_logs_email_job_id",
                table: "email_send_logs",
                column: "email_job_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_templates_key_locale_version",
                table: "email_templates",
                columns: new[] { "key", "locale", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketing_consents_subscriber_id_channel_is_opt_in",
                table: "marketing_consents",
                columns: new[] { "subscriber_id", "channel", "is_opt_in" });

            migrationBuilder.CreateIndex(
                name: "IX_marketing_consents_user_id",
                table: "marketing_consents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_order_promo_cost_snapshots_promo_created_at",
                table: "order_promo_cost_snapshots",
                columns: new[] { "promo_code_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_order_promo_cost_snapshots_order_id",
                table: "order_promo_cost_snapshots",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_confirmation_token",
                table: "subscribers",
                column: "confirmation_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_email",
                table: "subscribers",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_unsubscribe_token",
                table: "subscribers",
                column: "unsubscribe_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscribers_user_id",
                table: "subscribers",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_send_logs");

            migrationBuilder.DropTable(
                name: "email_templates");

            migrationBuilder.DropTable(
                name: "marketing_consents");

            migrationBuilder.DropTable(
                name: "order_promo_cost_snapshots");

            migrationBuilder.DropTable(
                name: "email_jobs");

            migrationBuilder.DropTable(
                name: "subscribers");

            migrationBuilder.DropColumn(
                name: "cost_price",
                table: "product_variants");
        }
    }
}
