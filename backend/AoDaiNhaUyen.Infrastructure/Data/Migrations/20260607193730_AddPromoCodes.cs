using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promo_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    discount_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    min_order_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    max_uses = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_uses = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    free_shipping = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_codes", x => x.id);
                    table.CheckConstraint("ck_promo_dates", "end_date > start_date");
                    table.CheckConstraint("ck_promo_discount_value", "discount_value >= 0");
                    table.CheckConstraint("ck_promo_uses", "current_uses >= 0 AND current_uses <= max_uses");
                });

            migrationBuilder.CreateTable(
                name: "order_promo_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    promo_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_amount_applied = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_promo_codes", x => x.id);
                    table.CheckConstraint("ck_order_promo_discount", "discount_amount_applied >= 0");
                    table.ForeignKey(
                        name: "FK_order_promo_codes_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_promo_codes_promo_codes_promo_code_id",
                        column: x => x.promo_code_id,
                        principalTable: "promo_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_promo_codes_order_id_promo_code_id",
                table: "order_promo_codes",
                columns: new[] { "order_id", "promo_code_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_promo_codes_promo_code_id",
                table: "order_promo_codes",
                column: "promo_code_id");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_code",
                table: "promo_codes",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_promo_codes");

            migrationBuilder.DropTable(
                name: "promo_codes");
        }
    }
}
