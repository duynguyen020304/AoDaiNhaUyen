using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStylistCatalogTryOnAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_threads",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    guest_key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "web"),
                    claimed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_threads", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_threads_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "product_ai_assets",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    variant_id = table.Column<long>(type: "bigint", nullable: true),
                    asset_kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    file_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_ai_assets", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_ai_assets_product_variants_variant_id",
                        column: x => x.variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_product_ai_assets_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_style_profiles",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    style_keywords_jsonb = table.Column<string>(type: "jsonb", nullable: true),
                    formality = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    silhouette = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    primary_color_family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    secondary_color_family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_style_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_style_profiles_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "style_scenarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_style_scenarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    thread_id = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "user"),
                    content = table.Column<string>(type: "text", nullable: false),
                    intent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    client_message_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    prompt_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    usage_jsonb = table.Column<string>(type: "jsonb", nullable: true),
                    finish_reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    tool_calls_jsonb = table.Column<string>(type: "jsonb", nullable: true),
                    structured_payload_jsonb = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_threads_thread_id",
                        column: x => x.thread_id,
                        principalTable: "chat_threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_pairings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    base_product_id = table.Column<long>(type: "bigint", nullable: false),
                    paired_product_id = table.Column<long>(type: "bigint", nullable: false),
                    scenario_id = table.Column<long>(type: "bigint", nullable: true),
                    score = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_pairings", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_pairings_products_base_product_id",
                        column: x => x.base_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_pairings_products_paired_product_id",
                        column: x => x.paired_product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_pairings_style_scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "style_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "product_scenarios",
                columns: table => new
                {
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    scenario_id = table.Column<long>(type: "bigint", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_scenarios", x => new { x.product_id, x.scenario_id });
                    table.ForeignKey(
                        name: "FK_product_scenarios_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_scenarios_style_scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "style_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_attachments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    thread_id = table.Column<long>(type: "bigint", nullable: false),
                    message_id = table.Column<long>(type: "bigint", nullable: true),
                    kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "user_image"),
                    file_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    metadata_jsonb = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_attachments_chat_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_chat_attachments_chat_threads_thread_id",
                        column: x => x.thread_id,
                        principalTable: "chat_threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_thread_memory",
                columns: table => new
                {
                    thread_id = table.Column<long>(type: "bigint", nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    facts_jsonb = table.Column<string>(type: "jsonb", nullable: true),
                    resolved_refs_jsonb = table.Column<string>(type: "jsonb", nullable: true),
                    last_message_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_thread_memory", x => x.thread_id);
                    table.ForeignKey(
                        name: "FK_chat_thread_memory_chat_messages_last_message_id",
                        column: x => x.last_message_id,
                        principalTable: "chat_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_chat_thread_memory_chat_threads_thread_id",
                        column: x => x.thread_id,
                        principalTable: "chat_threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_chat_attachments_thread_id",
                table: "chat_attachments",
                column: "thread_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_attachments_message_id",
                table: "chat_attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_messages_thread_id",
                table: "chat_messages",
                column: "thread_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_thread_memory_last_message_id",
                table: "chat_thread_memory",
                column: "last_message_id");

            migrationBuilder.CreateIndex(
                name: "idx_chat_threads_guest_key_hash",
                table: "chat_threads",
                column: "guest_key_hash");

            migrationBuilder.CreateIndex(
                name: "idx_chat_threads_user_id",
                table: "chat_threads",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_product_ai_assets_lookup",
                table: "product_ai_assets",
                columns: new[] { "product_id", "asset_kind", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_product_ai_assets_product_id",
                table: "product_ai_assets",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_ai_assets_variant_id",
                table: "product_ai_assets",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_pairings_base_product_id_paired_product_id_scenario~",
                table: "product_pairings",
                columns: new[] { "base_product_id", "paired_product_id", "scenario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_pairings_paired_product_id",
                table: "product_pairings",
                column: "paired_product_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_pairings_scenario_id",
                table: "product_pairings",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_scenarios_scenario_id",
                table: "product_scenarios",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_style_profiles_product_id",
                table: "product_style_profiles",
                column: "product_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_style_scenarios_slug",
                table: "style_scenarios",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_attachments");

            migrationBuilder.DropTable(
                name: "chat_thread_memory");

            migrationBuilder.DropTable(
                name: "product_ai_assets");

            migrationBuilder.DropTable(
                name: "product_pairings");

            migrationBuilder.DropTable(
                name: "product_scenarios");

            migrationBuilder.DropTable(
                name: "product_style_profiles");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "style_scenarios");

            migrationBuilder.DropTable(
                name: "chat_threads");
        }
    }
}
