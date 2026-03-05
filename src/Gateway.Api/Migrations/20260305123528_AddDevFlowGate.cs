using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDevFlowGate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevFlowGates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DevFlowRunId = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DecisionComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DecidedByUserId = table.Column<int>(type: "integer", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevFlowGates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevFlowGates_DevFlowRuns_DevFlowRunId",
                        column: x => x.DevFlowRunId,
                        principalTable: "DevFlowRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevFlowGates_IdentityUsers_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevFlowGates_DecidedByUserId",
                table: "DevFlowGates",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DevFlowGates_DevFlowRunId",
                table: "DevFlowGates",
                column: "DevFlowRunId");

            migrationBuilder.CreateIndex(
                name: "IX_DevFlowGates_DevFlowRunId_Stage",
                table: "DevFlowGates",
                columns: new[] { "DevFlowRunId", "Stage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevFlowGates");
        }
    }
}
