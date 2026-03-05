using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchPlanModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BranchPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DevFlowRunId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    FormatVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchPlans_DevFlowRuns_DevFlowRunId",
                        column: x => x.DevFlowRunId,
                        principalTable: "DevFlowRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BranchPlans_IdentityUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BranchPlanItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchPlanId = table.Column<int>(type: "integer", nullable: false),
                    StoryId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TaskId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BranchName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchPlanItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchPlanItems_BranchPlans_BranchPlanId",
                        column: x => x.BranchPlanId,
                        principalTable: "BranchPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchPlanItems_BranchName",
                table: "BranchPlanItems",
                column: "BranchName");

            migrationBuilder.CreateIndex(
                name: "IX_BranchPlanItems_BranchPlanId",
                table: "BranchPlanItems",
                column: "BranchPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchPlanItems_BranchPlanId_TaskId",
                table: "BranchPlanItems",
                columns: new[] { "BranchPlanId", "TaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BranchPlans_CreatedByUserId",
                table: "BranchPlans",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchPlans_DevFlowRunId",
                table: "BranchPlans",
                column: "DevFlowRunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchPlanItems");

            migrationBuilder.DropTable(
                name: "BranchPlans");
        }
    }
}
