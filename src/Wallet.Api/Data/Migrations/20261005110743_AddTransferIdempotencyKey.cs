using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_InitiatedByUserId",
                table: "Transfers");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Transfers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_InitiatedByUserId_IdempotencyKey",
                table: "Transfers",
                columns: new[] { "InitiatedByUserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transfers_InitiatedByUserId_IdempotencyKey",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Transfers");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_InitiatedByUserId",
                table: "Transfers",
                column: "InitiatedByUserId");
        }
    }
}
