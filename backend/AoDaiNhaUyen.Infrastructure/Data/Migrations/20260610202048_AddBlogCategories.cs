using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "blog_category_id",
                table: "blog_posts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "blog_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blog_categories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_blog_posts_blog_category_id",
                table: "blog_posts",
                column: "blog_category_id");

            migrationBuilder.CreateIndex(
                name: "idx_blog_categories_active_sort_order",
                table: "blog_categories",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "idx_blog_categories_slug_unique",
                table: "blog_categories",
                column: "slug",
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.AddForeignKey(
                name: "FK_blog_posts_blog_categories_blog_category_id",
                table: "blog_posts",
                column: "blog_category_id",
                principalTable: "blog_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_blog_posts_blog_categories_blog_category_id",
                table: "blog_posts");

            migrationBuilder.DropTable(
                name: "blog_categories");

            migrationBuilder.DropIndex(
                name: "idx_blog_posts_blog_category_id",
                table: "blog_posts");

            migrationBuilder.DropColumn(
                name: "blog_category_id",
                table: "blog_posts");
        }
    }
}
