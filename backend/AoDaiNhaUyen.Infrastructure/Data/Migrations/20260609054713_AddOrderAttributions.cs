using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderAttributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order_attributions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    anonymous_session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    first_touch_source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    first_touch_medium = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    first_touch_campaign = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    first_touch_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_touch_source = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    last_touch_medium = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    last_touch_campaign = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    last_touch_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    promo_code_id = table.Column<Guid>(type: "uuid", nullable: true),
                    promo_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    attributed_revenue = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    attributed_discount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    attributed_shipping_subsidy = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_attributions", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_attributions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_attributions_promo_codes_promo_code_id",
                        column: x => x.promo_code_id,
                        principalTable: "promo_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_order_attributions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_order_attributions_last_touch",
                table: "order_attributions",
                columns: new[] { "last_touch_source", "last_touch_medium", "last_touch_campaign" });

            migrationBuilder.CreateIndex(
                name: "idx_order_attributions_promo_code_id",
                table: "order_attributions",
                column: "promo_code_id");

            migrationBuilder.CreateIndex(
                name: "idx_order_attributions_user_created_at",
                table: "order_attributions",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_order_attributions_order_id",
                table: "order_attributions",
                column: "order_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_attributions");
        }
    }
}
