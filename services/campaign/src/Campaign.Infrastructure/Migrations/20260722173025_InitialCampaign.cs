using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Campaign.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "campaign_number_seq");

            migrationBuilder.CreateSequence(
                name: "case_number_seq");

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    campaign_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_segment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    discount_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaigns", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "subscriber_profiles",
                columns: table => new
                {
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gsm_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    current_plan = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    tenure_months = table.Column<int>(type: "integer", nullable: false),
                    avg_monthly_data_gb = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    avg_monthly_call_minutes = table.Column<int>(type: "integer", nullable: false),
                    monthly_spend_tl = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    package_purchase_count = table.Column<int>(type: "integer", nullable: false),
                    complaint_count = table.Column<int>(type: "integer", nullable: false),
                    days_since_last_activity = table.Column<int>(type: "integer", nullable: false),
                    past_acceptance_rate = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    current_segment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriber_profiles", x => x.subscriber_id);
                });

            migrationBuilder.CreateTable(
                name: "offers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recommendation_score = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    conversion_probability = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    is_priority = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_offers_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "optimization_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    case_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    assigned_expert_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sla_deadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sla_breached = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    expert_note = table.Column<string>(type: "text", nullable: true),
                    conversion_lift = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_optimization_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_optimization_cases_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "offer_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stars = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_ratings", x => x.id);
                    table.CheckConstraint("ck_offer_ratings_stars", "stars BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_offer_ratings_offers_offer_id",
                        column: x => x.offer_id,
                        principalTable: "offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "case_status_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    to_status = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_case_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_case_status_history_optimization_cases_case_id",
                        column: x => x.case_id,
                        principalTable: "optimization_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_campaign_number",
                table: "campaigns",
                column: "campaign_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_case_status_history_case_id",
                table: "case_status_history",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_ratings_offer_id",
                table: "offer_ratings",
                column: "offer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offers_campaign_id_subscriber_id",
                table: "offers",
                columns: new[] { "campaign_id", "subscriber_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offers_subscriber_id",
                table: "offers",
                column: "subscriber_id");

            migrationBuilder.CreateIndex(
                name: "ix_optimization_cases_assigned_expert_id_status",
                table: "optimization_cases",
                columns: new[] { "assigned_expert_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_optimization_cases_campaign_id",
                table: "optimization_cases",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_optimization_cases_case_number",
                table: "optimization_cases",
                column: "case_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_optimization_cases_status_priority",
                table: "optimization_cases",
                columns: new[] { "status", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "case_status_history");

            migrationBuilder.DropTable(
                name: "offer_ratings");

            migrationBuilder.DropTable(
                name: "subscriber_profiles");

            migrationBuilder.DropTable(
                name: "optimization_cases");

            migrationBuilder.DropTable(
                name: "offers");

            migrationBuilder.DropTable(
                name: "campaigns");

            migrationBuilder.DropSequence(
                name: "campaign_number_seq");

            migrationBuilder.DropSequence(
                name: "case_number_seq");
        }
    }
}
