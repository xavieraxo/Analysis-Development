using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDevFlowStageMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevFlowStageMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DevFlowRunId = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Sender = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevFlowStageMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevFlowStageMessages_DevFlowRuns_DevFlowRunId",
                        column: x => x.DevFlowRunId,
                        principalTable: "DevFlowRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevFlowStageMessages_DevFlowRunId",
                table: "DevFlowStageMessages",
                column: "DevFlowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_DevFlowStageMessages_DevFlowRunId_Stage_CreatedAt",
                table: "DevFlowStageMessages",
                columns: new[] { "DevFlowRunId", "Stage", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevFlowStageMessages");
        }
    }
}
