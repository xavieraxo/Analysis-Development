using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDevFlowArtifact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevFlowRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentStage = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevFlowRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevFlowRuns_IdentityUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DevFlowRuns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DevFlowArtifacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DevFlowRunId = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    AgentRole = table.Column<int>(type: "integer", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevFlowArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevFlowArtifacts_DevFlowRuns_DevFlowRunId",
                        column: x => x.DevFlowRunId,
                        principalTable: "DevFlowRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevFlowArtifacts_DevFlowRunId",
                table: "DevFlowArtifacts",
                column: "DevFlowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_DevFlowArtifacts_DevFlowRunId_Stage",
                table: "DevFlowArtifacts",
                columns: new[] { "DevFlowRunId", "Stage" });

            migrationBuilder.CreateIndex(
                name: "IX_DevFlowRuns_CreatedByUserId",
                table: "DevFlowRuns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DevFlowRuns_ProjectId",
                table: "DevFlowRuns",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevFlowArtifacts");

            migrationBuilder.DropTable(
                name: "DevFlowRuns");
        }
    }
}
