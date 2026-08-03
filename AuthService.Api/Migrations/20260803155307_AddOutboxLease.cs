using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LockId",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntilUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockId",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LockedUntilUtc",
                table: "OutboxMessages");
        }
    }
}
