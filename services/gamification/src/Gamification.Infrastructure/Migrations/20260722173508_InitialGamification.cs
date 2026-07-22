using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gamification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialGamification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "badges",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_badges", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "expert_scores",
                columns: table => new
                {
                    expert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    total_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    completed_case_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    fast_completion_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    target_exceeded_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    riskli_kayip_saved_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expert_scores", x => x.expert_id);
                });

            migrationBuilder.CreateTable(
                name: "point_transactions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    expert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_point_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "segment_completion_counts",
                columns: table => new
                {
                    expert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    completed_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_segment_completion_counts", x => new { x.expert_id, x.segment });
                });

            migrationBuilder.CreateTable(
                name: "expert_badges",
                columns: table => new
                {
                    expert_id = table.Column<Guid>(type: "uuid", nullable: false),
                    badge_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    earned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expert_badges", x => new { x.expert_id, x.badge_code });
                    table.ForeignKey(
                        name: "fk_expert_badges_badges_badge_code",
                        column: x => x.badge_code,
                        principalTable: "badges",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expert_badges_badge_code",
                table: "expert_badges",
                column: "badge_code");

            migrationBuilder.CreateIndex(
                name: "ix_point_transactions_event_id",
                table: "point_transactions",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_point_transactions_expert_id_created_at",
                table: "point_transactions",
                columns: new[] { "expert_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expert_badges");

            migrationBuilder.DropTable(
                name: "expert_scores");

            migrationBuilder.DropTable(
                name: "point_transactions");

            migrationBuilder.DropTable(
                name: "segment_completion_counts");

            migrationBuilder.DropTable(
                name: "badges");
        }
    }
}
