using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Host.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedByAndImpersonatorIdToAuditable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NacUsers_TenantId",
                schema: "identity",
                table: "NacUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "identity",
                table: "NacUsers");

            migrationBuilder.AddColumn<string>(
                name: "ImpersonatorId",
                schema: "identity",
                table: "NacUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHost",
                schema: "identity",
                table: "NacUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "identity",
                table: "NacUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BaseTemplateId",
                schema: "identity",
                table: "NacRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "identity",
                table: "NacRoles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "NacRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "identity",
                table: "NacRoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpersonatorId",
                schema: "identity",
                table: "NacRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "NacRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTemplate",
                schema: "identity",
                table: "NacRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "identity",
                table: "NacRoles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "identity",
                table: "NacRoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "identity",
                table: "NacRoles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NacPermissionGrants",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PermissionName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NacPermissionGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NacUserTenantMemberships",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvitedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    ImpersonatorId = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NacUserTenantMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NacUserTenantMemberships_NacUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "NacUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NacMembershipRoles",
                schema: "identity",
                columns: table => new
                {
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NacMembershipRoles", x => new { x.MembershipId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_NacMembershipRoles_NacRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "NacRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NacMembershipRoles_NacUserTenantMemberships_MembershipId",
                        column: x => x.MembershipId,
                        principalSchema: "identity",
                        principalTable: "NacUserTenantMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NacRoles_TenantId_NormalizedName",
                schema: "identity",
                table: "NacRoles",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NacMembershipRoles_RoleId",
                schema: "identity",
                table: "NacMembershipRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_NacPermissionGrants_Provider_Permission_Tenant",
                schema: "identity",
                table: "NacPermissionGrants",
                columns: new[] { "ProviderName", "ProviderKey", "PermissionName", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NacPermissionGrants_TenantId_ProviderName",
                schema: "identity",
                table: "NacPermissionGrants",
                columns: new[] { "TenantId", "ProviderName" });

            migrationBuilder.CreateIndex(
                name: "IX_NacUserTenantMemberships_TenantId",
                schema: "identity",
                table: "NacUserTenantMemberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_NacUserTenantMemberships_UserId_TenantId",
                schema: "identity",
                table: "NacUserTenantMemberships",
                columns: new[] { "UserId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NacMembershipRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "NacPermissionGrants",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "NacUserTenantMemberships",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_NacRoles_TenantId_NormalizedName",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "ImpersonatorId",
                schema: "identity",
                table: "NacUsers");

            migrationBuilder.DropColumn(
                name: "IsHost",
                schema: "identity",
                table: "NacUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "NacUsers");

            migrationBuilder.DropColumn(
                name: "BaseTemplateId",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "ImpersonatorId",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "IsTemplate",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "NacRoles");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "identity",
                table: "NacUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_NacUsers_TenantId",
                schema: "identity",
                table: "NacUsers",
                column: "TenantId");
        }
    }
}
