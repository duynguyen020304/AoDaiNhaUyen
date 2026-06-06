using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AoDaiNhaUyen.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncProductIsPublicProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Column is_public already exists from migration 20260606025722_AddProductIsPublic.
            // This migration only syncs the EF model snapshot with the C# entity property.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: column was created by 20260606025722_AddProductIsPublic.
        }
    }
}
