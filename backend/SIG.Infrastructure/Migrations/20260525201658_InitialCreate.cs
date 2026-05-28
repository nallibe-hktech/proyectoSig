using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nif = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pais = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    codigo_postal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    contacto_nombre = table.Column<string>(type: "text", nullable: true),
                    contacto_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    contacto_telefono = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "concepts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    formula_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concepts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cost_centers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cost_centers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_departments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "periods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_bizneo_empleados",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    empleado_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    nif = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    departamento = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_bizneo_empleados", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_bizneo_horas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    registro_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    horas = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_bizneo_horas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_celero_visitas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visita_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    action_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    tipo_visita = table.Column<int>(type: "integer", nullable: false),
                    punto_montado = table.Column<int>(type: "integer", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_celero_visitas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_intratime_fichajes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fichaje_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    entrada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    salida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_intratime_fichajes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_pay_hawk_gastos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gasto_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    importe = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_pay_hawk_gastos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "variables",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    question_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mapeo_valores_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_variables", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    interlocutor_nombre = table.Column<string>(type: "text", nullable: true),
                    interlocutor_email = table.Column<string>(type: "text", nullable: true),
                    interlocutor_telefono = table.Column<string>(type: "text", nullable: true),
                    fecha_alta = table.Column<DateOnly>(type: "date", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nif = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellidos = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "actions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_actions_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actions_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_actions_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "closures",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    period_id = table.Column<int>(type: "integer", nullable: false),
                    coste_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    facturacion_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    margen = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    paso_actual = table.Column<int>(type: "integer", nullable: false),
                    comentarios = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_closures", x => x.id);
                    table.ForeignKey(
                        name: "fk_closures_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_closures_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_cost_centers",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    cost_center_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_cost_centers", x => new { x.project_id, x.cost_center_id });
                    table.ForeignKey(
                        name: "fk_project_cost_centers_cost_centers_cost_center_id",
                        column: x => x.cost_center_id,
                        principalTable: "cost_centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_cost_centers_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    old_value_json = table.Column<string>(type: "jsonb", nullable: true),
                    new_value_json = table.Column<string>(type: "jsonb", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "concept_users",
                columns: table => new
                {
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_concept_users", x => new { x.concept_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_concept_users_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_concept_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_users",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_users", x => new { x.project_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_project_users_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_project_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "action_concepts",
                columns: table => new
                {
                    action_id = table.Column<int>(type: "integer", nullable: false),
                    concept_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_action_concepts", x => new { x.action_id, x.concept_id });
                    table.ForeignKey(
                        name: "fk_action_concepts_actions_action_id",
                        column: x => x.action_id,
                        principalTable: "actions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_action_concepts_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "action_users",
                columns: table => new
                {
                    action_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_action_users", x => new { x.action_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_action_users_actions_action_id",
                        column: x => x.action_id,
                        principalTable: "actions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_action_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    closure_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    paso_origen = table.Column<int>(type: "integer", nullable: false),
                    paso_destino = table.Column<int>(type: "integer", nullable: false),
                    accion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    motivo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_history_closures_closure_id",
                        column: x => x.closure_id,
                        principalTable: "closures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_approval_history_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "approvals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    closure_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    paso = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motivo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    fecha_decision = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_approvals_closures_closure_id",
                        column: x => x.closure_id,
                        principalTable: "closures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_approvals_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approvals_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "closure_lines",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    closure_id = table.Column<int>(type: "integer", nullable: false),
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    importe = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    datos_entrada_json = table.Column<string>(type: "jsonb", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tiene_incidencia = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_closure_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_closure_lines_closures_closure_id",
                        column: x => x.closure_id,
                        principalTable: "closures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_closure_lines_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_closure_lines_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "calculation_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    closure_line_id = table.Column<int>(type: "integer", nullable: false),
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    formula_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    inputs_json = table.Column<string>(type: "jsonb", nullable: false),
                    resultado = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    incidencias = table.Column<string>(type: "jsonb", nullable: true),
                    sistema_origen = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calculation_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_calculation_logs_closure_lines_closure_line_id",
                        column: x => x.closure_line_id,
                        principalTable: "closure_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_calculation_logs_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_action_concepts_concept_id",
                table: "action_concepts",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_action_users_user_id",
                table: "action_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_actions_client_id",
                table: "actions",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_actions_department_id",
                table: "actions",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_actions_project_id",
                table: "actions",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_history_closure_id",
                table: "approval_history",
                column: "closure_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_history_user_id",
                table: "approval_history",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_closure_id",
                table: "approvals",
                column: "closure_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_role_id",
                table: "approvals",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_user_id",
                table: "approvals",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_calculation_logs_closure_line_id",
                table: "calculation_logs",
                column: "closure_line_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calculation_logs_concept_id",
                table: "calculation_logs",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_closure_lines_closure_id",
                table: "closure_lines",
                column: "closure_id");

            migrationBuilder.CreateIndex(
                name: "ix_closure_lines_concept_id",
                table: "closure_lines",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_closure_lines_user_id",
                table: "closure_lines",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_closures_period_id",
                table: "closures",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_closures_project_id_period_id",
                table: "closures",
                columns: new[] { "project_id", "period_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_concept_users_user_id",
                table: "concept_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_centers_codigo",
                table: "cost_centers",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_periods_nombre",
                table: "periods",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_project_cost_centers_cost_center_id",
                table: "project_cost_centers",
                column: "cost_center_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_users_user_id",
                table: "project_users",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_client_id",
                table: "projects",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_nombre",
                table: "roles",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staging_bizneo_empleados_hash",
                table: "staging_bizneo_empleados",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staging_bizneo_horas_hash",
                table: "staging_bizneo_horas",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staging_celero_visitas_hash",
                table: "staging_celero_visitas",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staging_intratime_fichajes_hash",
                table: "staging_intratime_fichajes",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staging_pay_hawk_gastos_hash",
                table: "staging_pay_hawk_gastos",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_department_id",
                table: "users",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_nif",
                table: "users",
                column: "nif",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "action_concepts");

            migrationBuilder.DropTable(
                name: "action_users");

            migrationBuilder.DropTable(
                name: "approval_history");

            migrationBuilder.DropTable(
                name: "approvals");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "calculation_logs");

            migrationBuilder.DropTable(
                name: "concept_users");

            migrationBuilder.DropTable(
                name: "project_cost_centers");

            migrationBuilder.DropTable(
                name: "project_users");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "staging_bizneo_empleados");

            migrationBuilder.DropTable(
                name: "staging_bizneo_horas");

            migrationBuilder.DropTable(
                name: "staging_celero_visitas");

            migrationBuilder.DropTable(
                name: "staging_intratime_fichajes");

            migrationBuilder.DropTable(
                name: "staging_pay_hawk_gastos");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "variables");

            migrationBuilder.DropTable(
                name: "actions");

            migrationBuilder.DropTable(
                name: "closure_lines");

            migrationBuilder.DropTable(
                name: "cost_centers");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "closures");

            migrationBuilder.DropTable(
                name: "concepts");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "periods");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "clients");
        }
    }
}
