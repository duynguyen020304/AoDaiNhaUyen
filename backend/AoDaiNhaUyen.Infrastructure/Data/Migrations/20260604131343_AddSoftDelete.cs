using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_phone",
                table: "users");

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "user_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "user_sessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "user_sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "user_roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "user_roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user_roles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "user_roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "user_roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "user_addresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user_addresses",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "user_addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "user_addresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "user_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "user_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "user_accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "style_scenarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "style_scenarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "shipments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "shipments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "shipments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "shipments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "reviews",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "reviews",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "reviews",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "product_variants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "product_variants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "product_style_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "product_style_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "product_style_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "product_scenarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "product_scenarios",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "product_scenarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "product_pairings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "product_pairings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "product_pairings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "product_images",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "product_images",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "product_images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "product_images",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "product_ai_assets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "product_ai_assets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "payments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "payments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "payments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "password_reset_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "password_reset_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "password_reset_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "order_items",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "order_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "order_items",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "measurement_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "measurement_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "measurement_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "image_validation_cache_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "image_validation_cache_entries",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "image_validation_cache_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "image_validation_cache_entries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "email_verification_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "email_verification_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "email_verification_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "chat_threads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "chat_threads",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "chat_threads",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "chat_thread_memory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "chat_thread_memory",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "chat_thread_memory",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "chat_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "chat_messages",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "chat_messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "chat_messages",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "chat_attachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "chat_attachments",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "chat_attachments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "chat_attachments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "categories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "categories",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "carts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "carts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "carts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "cart_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "cart_items",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "cart_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                table: "users",
                column: "phone",
                unique: true,
                filter: "NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_phone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "users");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "user_roles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "user_addresses");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "user_accounts");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "style_scenarios");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "style_scenarios");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "products");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "products");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "product_style_profiles");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "product_style_profiles");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "product_style_profiles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "product_scenarios");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "product_scenarios");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "product_scenarios");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "product_pairings");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "product_pairings");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "product_pairings");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "product_ai_assets");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "product_ai_assets");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "measurement_profiles");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "measurement_profiles");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "measurement_profiles");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "image_validation_cache_entries");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "image_validation_cache_entries");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "image_validation_cache_entries");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "image_validation_cache_entries");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "email_verification_tokens");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_threads");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "chat_threads");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "chat_threads");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_thread_memory");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "chat_thread_memory");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "chat_thread_memory");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_attachments");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "chat_attachments");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "chat_attachments");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "chat_attachments");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "carts");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "cart_items");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "cart_items");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "cart_items");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                table: "users",
                column: "phone",
                unique: true);
        }
    }
}
