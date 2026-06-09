using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customer_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    anonymous_session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    event_type = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    promo_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: true),
                    campaign_send_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    medium = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    campaign = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_customer_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_events_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_customer_events_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_customer_events_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_customer_events_promo_codes_promo_code_id",
                        column: x => x.promo_code_id,
                        principalTable: "promo_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_customer_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "idx_customer_events_campaign_occurred_at",
                table: "customer_events",
                columns: new[] { "campaign_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_customer_events_session_occurred_at",
                table: "customer_events",
                columns: new[] { "anonymous_session_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_customer_events_type_occurred_at",
                table: "customer_events",
                columns: new[] { "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "idx_customer_events_user_occurred_at",
                table: "customer_events",
                columns: new[] { "user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_customer_events_order_id",
                table: "customer_events",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_events_product_id",
                table: "customer_events",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_events_product_variant_id",
                table: "customer_events",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_events_promo_code_id",
                table: "customer_events",
                column: "promo_code_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_events");
        }
    }
}
