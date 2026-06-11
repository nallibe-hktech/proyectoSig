using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGalanAndMediapostStagingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staging_galan_entradas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_articulo = table.Column<string>(type: "text", nullable: false),
                    codigo_departamento = table.Column<string>(type: "text", nullable: false),
                    codigo_familia = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    unidades = table.Column<int>(type: "integer", nullable: false),
                    empresa = table.Column<string>(type: "text", nullable: false),
                    almacen = table.Column<string>(type: "text", nullable: false),
                    celda = table.Column<string>(type: "text", nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_galan_entradas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_galan_salidas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    albaran = table.Column<string>(type: "text", nullable: false),
                    numero_pedido_tercero = table.Column<string>(type: "text", nullable: true),
                    codigo_articulo = table.Column<string>(type: "text", nullable: false),
                    codigo_departamento = table.Column<string>(type: "text", nullable: false),
                    codigo_familia = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    unidades = table.Column<int>(type: "integer", nullable: false),
                    codigo_transporte = table.Column<string>(type: "text", nullable: true),
                    matricula = table.Column<string>(type: "text", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    destinatario = table.Column<string>(type: "text", nullable: true),
                    almacen = table.Column<string>(type: "text", nullable: false),
                    celda = table.Column<string>(type: "text", nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_galan_salidas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_galan_stocks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo_articulo = table.Column<string>(type: "text", nullable: false),
                    codigo_departamento = table.Column<string>(type: "text", nullable: false),
                    codigo_familia = table.Column<string>(type: "text", nullable: false),
                    codigo_celda = table.Column<string>(type: "text", nullable: false),
                    stock_b = table.Column<decimal>(type: "numeric", nullable: false),
                    stock_a = table.Column<decimal>(type: "numeric", nullable: false),
                    stock = table.Column<decimal>(type: "numeric", nullable: false),
                    almacen = table.Column<string>(type: "text", nullable: false),
                    familia = table.Column<string>(type: "text", nullable: false),
                    sub_familia = table.Column<string>(type: "text", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_galan_stocks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_mediapost_pedidos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    pedido_id = table.Column<string>(type: "text", nullable: false),
                    referencia_pedido = table.Column<string>(type: "text", nullable: false),
                    codigo_articulo = table.Column<string>(type: "text", nullable: false),
                    fecha_pedido = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    destinatario_nombre = table.Column<string>(type: "text", nullable: true),
                    direccion_entrega = table.Column<string>(type: "text", nullable: true),
                    codigo_postal = table.Column<string>(type: "text", nullable: true),
                    ciudad = table.Column<string>(type: "text", nullable: true),
                    provincia = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_mediapost_pedidos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_mediapost_recepciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recepcion_id = table.Column<string>(type: "text", nullable: false),
                    referencia_recepcion = table.Column<string>(type: "text", nullable: false),
                    codigo_articulo = table.Column<string>(type: "text", nullable: false),
                    fecha_recepcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cantidad = table.Column<int>(type: "integer", nullable: false),
                    cantidad_dañada = table.Column<int>(type: "integer", nullable: true),
                    estado = table.Column<string>(type: "text", nullable: false),
                    almacen = table.Column<string>(type: "text", nullable: true),
                    observaciones = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_mediapost_recepciones", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staging_galan_entradas");

            migrationBuilder.DropTable(
                name: "staging_galan_salidas");

            migrationBuilder.DropTable(
                name: "staging_galan_stocks");

            migrationBuilder.DropTable(
                name: "staging_mediapost_pedidos");

            migrationBuilder.DropTable(
                name: "staging_mediapost_recepciones");
        }
    }
}
