using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectOwnershipEmpresaAndDevFlowScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DevFlowRuns_Projects_ProjectId",
                table: "DevFlowRuns");

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "DevFlowRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMigrated",
                table: "DevFlowRuns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "DevFlowRuns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GateAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GateId = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsForcedBySuperUser = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GateAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GateAuditLogs_DevFlowGates_GateId",
                        column: x => x.GateId,
                        principalTable: "DevFlowGates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GateAuditLogs_IdentityUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InternalDevFlowAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActorUserId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalDevFlowAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalDevFlowAuditLogs_IdentityUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectOperators",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOperators", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ProjectOperators_IdentityUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectOperators_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmpresaUsers",
                columns: table => new
                {
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleInEmpresa = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmpresaUsers", x => new { x.EmpresaId, x.UserId });
                    table.ForeignKey(
                        name: "FK_EmpresaUsers_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmpresaUsers_IdentityUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_EmpresaId",
                table: "Projects",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_EmpresaUsers_UserId",
                table: "EmpresaUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GateAuditLogs_ActorUserId",
                table: "GateAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GateAuditLogs_GateId",
                table: "GateAuditLogs",
                column: "GateId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalDevFlowAuditLogs_ActorUserId",
                table: "InternalDevFlowAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOperators_UserId",
                table: "ProjectOperators",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DevFlowRuns_Projects_ProjectId",
                table: "DevFlowRuns",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Empresas_EmpresaId",
                table: "Projects",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DevFlowRuns_Projects_ProjectId",
                table: "DevFlowRuns");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Empresas_EmpresaId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "EmpresaUsers");

            migrationBuilder.DropTable(
                name: "GateAuditLogs");

            migrationBuilder.DropTable(
                name: "InternalDevFlowAuditLogs");

            migrationBuilder.DropTable(
                name: "ProjectOperators");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropIndex(
                name: "IX_Projects_EmpresaId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsMigrated",
                table: "DevFlowRuns");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "DevFlowRuns");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "DevFlowRuns",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_DevFlowRuns_Projects_ProjectId",
                table: "DevFlowRuns",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
