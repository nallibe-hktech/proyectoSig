using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentModelsConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create payment_models table
            migrationBuilder.CreateTable(
                name: "payment_models",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    model_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_until = table.Column<DateOnly>(type: "date", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_models", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_models_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_models_client_id",
                table: "payment_models",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_models_client_id_model_type",
                table: "payment_models",
                columns: new[] { "client_id", "model_type" });

            // Create payment_rates_configuration table
            migrationBuilder.CreateTable(
                name: "payment_rates_configuration",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    concept_id = table.Column<int>(type: "integer", nullable: true),
                    year = table.Column<int>(type: "integer", nullable: false),
                    month = table.Column<int>(type: "integer", nullable: true),
                    base_rate = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    rate_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rate_formula = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    min_value = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    max_value = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_rates_configuration", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_rates_configuration_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payment_rates_configuration_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_rates_configuration_client_id",
                table: "payment_rates_configuration",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_rates_configuration_concept_id",
                table: "payment_rates_configuration",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_rates_configuration_client_year_month",
                table: "payment_rates_configuration",
                columns: new[] { "client_id", "year", "month" });

            // Create employee_payment_model_mappings table
            migrationBuilder.CreateTable(
                name: "employee_payment_model_mappings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    employee_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    payment_model_id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_until = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_payment_model_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_employee_payment_model_mappings_payment_models_payment_model_id",
                        column: x => x.payment_model_id,
                        principalTable: "payment_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_employee_payment_model_mappings_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_payment_model_mappings_payment_model_id",
                table: "employee_payment_model_mappings",
                column: "payment_model_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_payment_model_mappings_client_id",
                table: "employee_payment_model_mappings",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_payment_model_mappings_employee_code",
                table: "employee_payment_model_mappings",
                column: "employee_code");

            // Create concept_validation_rules table
            migrationBuilder.CreateTable(
                name: "concept_validation_rules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    payment_model_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_applicable = table.Column<bool>(type: "boolean", nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    calculation_method = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    aggregation_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concept_validation_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_concept_validation_rules_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_concept_validation_rules_concept_id",
                table: "concept_validation_rules",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_concept_validation_rules_concept_payment_model",
                table: "concept_validation_rules",
                columns: new[] { "concept_id", "payment_model_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "concept_validation_rules");

            migrationBuilder.DropTable(
                name: "employee_payment_model_mappings");

            migrationBuilder.DropTable(
                name: "payment_rates_configuration");

            migrationBuilder.DropTable(
                name: "payment_models");
        }
    }
}
