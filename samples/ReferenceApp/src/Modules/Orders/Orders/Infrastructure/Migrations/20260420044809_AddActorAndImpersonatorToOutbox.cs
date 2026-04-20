using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActorAndImpersonatorToOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                schema: "orders",
                table: "__outbox_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ImpersonatorUserId",
                schema: "orders",
                table: "__outbox_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "orders",
                table: "__outbox_events",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorUserId",
                schema: "orders",
                table: "__outbox_events");

            migrationBuilder.DropColumn(
                name: "ImpersonatorUserId",
                schema: "orders",
                table: "__outbox_events");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "orders",
                table: "__outbox_events");
        }
    }
}
