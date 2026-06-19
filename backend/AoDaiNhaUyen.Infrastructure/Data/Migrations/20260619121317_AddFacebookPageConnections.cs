using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFacebookPageConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "facebook_page_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    page_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    encrypted_page_access_token = table.Column<string>(type: "text", nullable: false),
                    token_last4 = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facebook_page_connections", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_facebook_page_connections_active_name",
                table: "facebook_page_connections",
                columns: new[] { "is_active", "page_name" });

            migrationBuilder.CreateIndex(
                name: "idx_facebook_page_connections_page_id_unique",
                table: "facebook_page_connections",
                column: "page_id",
                unique: true,
                filter: "NOT is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "facebook_page_connections");
        }
    }
}
