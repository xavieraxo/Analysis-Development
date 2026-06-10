using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gateway.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDevFlowTypeAndPipelineStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlowType",
                table: "DevFlowRuns",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Realign stored Status values after inserting PendingApproval at position 2.
            migrationBuilder.Sql("""
                UPDATE "DevFlowRuns" SET "Status" = 5 WHERE "Status" = 4;
                UPDATE "DevFlowRuns" SET "Status" = 4 WHERE "Status" = 3;
                UPDATE "DevFlowRuns" SET "Status" = 3 WHERE "Status" = 2;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "DevFlowRuns"
                SET "Status" = CASE "Status"
                    WHEN 5 THEN 4
                    WHEN 4 THEN 3
                    WHEN 3 THEN 2
                    WHEN 2 THEN 2
                    ELSE "Status"
                END;
                """);

            migrationBuilder.DropColumn(
                name: "FlowType",
                table: "DevFlowRuns");
        }
    }
}
