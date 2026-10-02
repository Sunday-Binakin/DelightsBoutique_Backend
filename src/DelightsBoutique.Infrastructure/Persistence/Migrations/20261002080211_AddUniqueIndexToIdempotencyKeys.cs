using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DelightsBoutique.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToIdempotencyKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_idempotency_keys_key_endpoint",
                table: "idempotency_keys");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_key_endpoint",
                table: "idempotency_keys",
                columns: new[] { "key", "endpoint" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_idempotency_keys_key_endpoint",
                table: "idempotency_keys");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_keys_key_endpoint",
                table: "idempotency_keys",
                columns: new[] { "key", "endpoint" });
        }
    }
}
