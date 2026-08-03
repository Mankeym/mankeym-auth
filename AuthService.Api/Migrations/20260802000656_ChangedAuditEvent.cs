using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangedAuditEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_AspNetUsers_ActorUserId",
                table: "AuditEvents");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorUserId",
                table: "AuditEvents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_AspNetUsers_ActorUserId",
                table: "AuditEvents",
                column: "ActorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_AspNetUsers_ActorUserId",
                table: "AuditEvents");

            migrationBuilder.AlterColumn<Guid>(
                name: "ActorUserId",
                table: "AuditEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_AspNetUsers_ActorUserId",
                table: "AuditEvents",
                column: "ActorUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
