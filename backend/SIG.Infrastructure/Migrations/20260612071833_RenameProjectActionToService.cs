using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameProjectActionToService : Migration
    {
        /// <summary>
        /// Migración PPT Ola 1 — eliminar Project, renombrar Action→Service.
        /// PRESERVA DATOS (renames + re-apuntado). Opción A: cada artefacto que colgaba de Project
        /// (closures, conceptos, tarifas, presupuestos, CECOs, mapeos, staging) se reasigna al
        /// "servicio principal" del proyecto = la Action con id mínimo. CeleroMission/ActionUser/
        /// ActionConcept conservan su service_id = antiguo action_id (las ids de actions se mantienen
        /// al renombrar la tabla a services).
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                -- 0. Tabla huérfana 'incidencias' (no está en el modelo; vacía; su FK a projects bloquearía el drop)
                DROP TABLE IF EXISTS incidencias CASCADE;

                -- 0b. Vistas BI que dependen de projects/closures.project_id (se recrean al final con service_id)
                DROP VIEW IF EXISTS bi.v_cierres_por_periodo;
                DROP VIEW IF EXISTS bi.v_aprobaciones_pendientes;

                -- 1. Mapa project_id -> servicio principal (Action con id mínimo del proyecto)
                CREATE TEMP TABLE _p2s AS
                    SELECT project_id, MIN(id) AS service_id FROM actions GROUP BY project_id;

                -- 2. actions -> services (conserva filas e ids)
                ALTER TABLE actions DROP CONSTRAINT fk_actions_projects_project_id;
                ALTER TABLE actions RENAME TO services;
                ALTER TABLE services RENAME CONSTRAINT pk_actions TO pk_services;
                ALTER TABLE services RENAME CONSTRAINT fk_actions_clients_client_id TO fk_services_clients_client_id;
                ALTER TABLE services RENAME CONSTRAINT fk_actions_departments_department_id TO fk_services_departments_department_id;
                ALTER INDEX ix_actions_client_id RENAME TO ix_services_client_id;
                ALTER INDEX ix_actions_department_id RENAME TO ix_services_department_id;
                ALTER TABLE services ADD COLUMN interlocutor_nombre text;
                ALTER TABLE services ADD COLUMN interlocutor_email text;
                ALTER TABLE services ADD COLUMN interlocutor_telefono text;
                ALTER TABLE services ADD COLUMN fecha_alta date NOT NULL DEFAULT DATE '2025-01-01';
                UPDATE services s SET interlocutor_nombre = p.interlocutor_nombre,
                                      interlocutor_email = p.interlocutor_email,
                                      interlocutor_telefono = p.interlocutor_telefono,
                                      fecha_alta = p.fecha_alta
                    FROM projects p WHERE s.project_id = p.id;
                ALTER TABLE services ALTER COLUMN fecha_alta DROP DEFAULT;
                UPDATE services SET estado = CASE estado WHEN 'Activa' THEN 'Activo' WHEN 'Inactiva' THEN 'Inactivo' ELSE estado END;
                ALTER TABLE services DROP COLUMN project_id;  -- su índice ix_actions_project_id cae con la columna

                -- 3. action_concepts -> service_concepts
                ALTER TABLE action_concepts RENAME TO service_concepts;
                ALTER TABLE service_concepts RENAME COLUMN action_id TO service_id;
                ALTER TABLE service_concepts RENAME CONSTRAINT pk_action_concepts TO pk_service_concepts;
                ALTER TABLE service_concepts RENAME CONSTRAINT fk_action_concepts_actions_action_id TO fk_service_concepts_services_service_id;
                ALTER TABLE service_concepts RENAME CONSTRAINT fk_action_concepts_concepts_concept_id TO fk_service_concepts_concepts_concept_id;
                ALTER INDEX ix_action_concepts_concept_id RENAME TO ix_service_concepts_concept_id;

                -- 4. action_users -> service_users (+ usuarios de project al servicio principal)
                ALTER TABLE action_users RENAME TO service_users;
                ALTER TABLE service_users RENAME COLUMN action_id TO service_id;
                ALTER TABLE service_users RENAME CONSTRAINT pk_action_users TO pk_service_users;
                ALTER TABLE service_users RENAME CONSTRAINT fk_action_users_actions_action_id TO fk_service_users_services_service_id;
                ALTER TABLE service_users RENAME CONSTRAINT fk_action_users_users_user_id TO fk_service_users_users_user_id;
                ALTER INDEX ix_action_users_user_id RENAME TO ix_service_users_user_id;
                INSERT INTO service_users (service_id, user_id)
                    SELECT m.service_id, pu.user_id
                    FROM project_users pu JOIN _p2s m ON m.project_id = pu.project_id
                    ON CONFLICT (service_id, user_id) DO NOTHING;

                -- 5. project_cost_centers -> service_cost_centers (re-apuntado al servicio principal)
                ALTER TABLE project_cost_centers DROP CONSTRAINT fk_project_cost_centers_projects_project_id;
                ALTER TABLE project_cost_centers RENAME TO service_cost_centers;
                ALTER TABLE service_cost_centers RENAME COLUMN project_id TO service_id;
                UPDATE service_cost_centers scc SET service_id = m.service_id FROM _p2s m WHERE scc.service_id = m.project_id;
                DELETE FROM service_cost_centers WHERE service_id NOT IN (SELECT id FROM services);
                ALTER TABLE service_cost_centers RENAME CONSTRAINT pk_project_cost_centers TO pk_service_cost_centers;
                ALTER TABLE service_cost_centers RENAME CONSTRAINT fk_project_cost_centers_cost_centers_cost_center_id TO fk_service_cost_centers_cost_centers_cost_center_id;
                ALTER INDEX ix_project_cost_centers_cost_center_id RENAME TO ix_service_cost_centers_cost_center_id;
                ALTER TABLE service_cost_centers ADD CONSTRAINT fk_service_cost_centers_services_service_id
                    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE CASCADE;

                -- 6. concepts: project_id -> service_id (principal; null queda null)
                ALTER TABLE concepts DROP CONSTRAINT fk_concepts_projects_project_id;
                ALTER TABLE concepts RENAME COLUMN project_id TO service_id;
                UPDATE concepts c SET service_id = m.service_id FROM _p2s m WHERE c.service_id = m.project_id;
                UPDATE concepts SET service_id = NULL WHERE service_id IS NOT NULL AND service_id NOT IN (SELECT id FROM services);
                ALTER INDEX ix_concepts_project_id RENAME TO ix_concepts_service_id;
                ALTER TABLE concepts ADD CONSTRAINT fk_concepts_services_service_id
                    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE SET NULL;

                -- 7. closures: project_id -> service_id (Opción A: servicio principal)
                ALTER TABLE closures DROP CONSTRAINT fk_closures_projects_project_id;
                ALTER TABLE closures RENAME COLUMN project_id TO service_id;
                UPDATE closures c SET service_id = m.service_id FROM _p2s m WHERE c.service_id = m.project_id;
                DELETE FROM closures WHERE service_id NOT IN (SELECT id FROM services);
                ALTER INDEX ix_closures_project_id_period_id RENAME TO ix_closures_service_id_period_id;
                ALTER TABLE closures ADD CONSTRAINT fk_closures_services_service_id
                    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE RESTRICT;

                -- 8. tarifas_proyecto -> tarifas_servicio
                ALTER TABLE tarifas_proyecto DROP CONSTRAINT fk_tarifas_proyecto_projects_project_id;
                ALTER TABLE tarifas_proyecto RENAME TO tarifas_servicio;
                ALTER TABLE tarifas_servicio RENAME COLUMN project_id TO service_id;
                UPDATE tarifas_servicio t SET service_id = m.service_id FROM _p2s m WHERE t.service_id = m.project_id;
                DELETE FROM tarifas_servicio WHERE service_id NOT IN (SELECT id FROM services);
                ALTER TABLE tarifas_servicio RENAME CONSTRAINT pk_tarifas_proyecto TO pk_tarifas_servicio;
                ALTER INDEX ix_tarifas_proyecto_project_id RENAME TO ix_tarifas_servicio_service_id;
                ALTER TABLE tarifas_servicio ADD CONSTRAINT fk_tarifas_servicio_services_service_id
                    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE CASCADE;

                -- 9. presupuestos_proyecto -> presupuestos_servicio
                ALTER TABLE presupuestos_proyecto DROP CONSTRAINT fk_presupuestos_proyecto_projects_project_id;
                ALTER TABLE presupuestos_proyecto RENAME TO presupuestos_servicio;
                ALTER TABLE presupuestos_servicio RENAME COLUMN project_id TO service_id;
                UPDATE presupuestos_servicio p SET service_id = m.service_id FROM _p2s m WHERE p.service_id = m.project_id;
                DELETE FROM presupuestos_servicio WHERE service_id NOT IN (SELECT id FROM services);
                ALTER TABLE presupuestos_servicio RENAME CONSTRAINT pk_presupuestos_proyecto TO pk_presupuestos_servicio;
                ALTER TABLE presupuestos_servicio RENAME CONSTRAINT fk_presupuestos_proyecto_periods_period_id TO fk_presupuestos_servicio_periods_period_id;
                ALTER INDEX ix_presupuestos_proyecto_project_id_period_id RENAME TO ix_presupuestos_servicio_service_id_period_id;
                ALTER INDEX ix_presupuestos_proyecto_period_id RENAME TO ix_presupuestos_servicio_period_id;
                ALTER TABLE presupuestos_servicio ADD CONSTRAINT fk_presupuestos_servicio_services_service_id
                    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE CASCADE;

                -- 10. celero_service_mappings: project_id -> service_id (servicio principal)
                ALTER TABLE celero_service_mappings DROP CONSTRAINT fk_celero_service_mappings_projects_project_id;
                ALTER TABLE celero_service_mappings RENAME COLUMN project_id TO service_id;
                UPDATE celero_service_mappings c SET service_id = m.service_id FROM _p2s m WHERE c.service_id = m.project_id;
                DELETE FROM celero_service_mappings WHERE service_id NOT IN (SELECT id FROM services);
                ALTER INDEX ix_celero_service_mappings_project_id RENAME TO ix_celero_service_mappings_service_id;
                ALTER TABLE celero_service_mappings ADD CONSTRAINT fk_celero_service_mappings_services_service_id
                    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE RESTRICT;

                -- 11. celero_mission_mappings: action_id -> service_id (action_id ya es el id de service)
                ALTER TABLE celero_mission_mappings DROP CONSTRAINT fk_celero_mission_mappings_actions_action_id;
                ALTER TABLE celero_mission_mappings RENAME COLUMN action_id TO service_id;
                ALTER INDEX ix_celero_mission_mappings_action_id RENAME TO ix_celero_mission_mappings_service_id;
                ALTER TABLE celero_mission_mappings ADD CONSTRAINT fk_celero_mission_mappings_services_service_id
                    FOREIGN KEY (service_id) REFERENCES services(id) ON DELETE RESTRICT;

                -- 12. staging: project_id/action_id -> service_id (sin FK; re-apuntado al servicio principal)
                ALTER TABLE staging_celero_visitas DROP COLUMN project_id;
                ALTER TABLE staging_celero_visitas RENAME COLUMN action_id TO service_id;

                ALTER TABLE staging_bizneo_absences RENAME COLUMN project_id TO service_id;
                UPDATE staging_bizneo_absences s SET service_id = COALESCE((SELECT m.service_id FROM _p2s m WHERE m.project_id = s.service_id), s.service_id);

                ALTER TABLE staging_pay_hawk_gastos RENAME COLUMN project_id TO service_id;
                UPDATE staging_pay_hawk_gastos s SET service_id = COALESCE((SELECT m.service_id FROM _p2s m WHERE m.project_id = s.service_id), s.service_id);

                ALTER TABLE staging_sgpv_visitas RENAME COLUMN project_id TO service_id;
                UPDATE staging_sgpv_visitas s SET service_id = (SELECT m.service_id FROM _p2s m WHERE m.project_id = s.service_id) WHERE s.service_id IS NOT NULL;

                ALTER TABLE staging_intratime_expenses RENAME COLUMN project_id TO service_id;
                UPDATE staging_intratime_expenses s SET service_id = (SELECT m.service_id FROM _p2s m WHERE m.project_id = s.service_id) WHERE s.service_id IS NOT NULL;

                -- 13. eliminar tablas de Project
                DROP TABLE project_users;
                DROP TABLE projects;

                DROP TABLE _p2s;

                -- 14. recrear vistas BI con el nuevo esquema (service_id)
                CREATE VIEW bi.v_cierres_por_periodo AS
                    SELECT c.service_id, s.nombre AS service_nombre, c.period_id, per.nombre AS periodo_nombre,
                           c.coste_total, c.facturacion_total, c.margen, c.estado
                    FROM closures c
                        JOIN services s ON s.id = c.service_id
                        JOIN periods per ON per.id = c.period_id;

                CREATE VIEW bi.v_aprobaciones_pendientes AS
                    SELECT c.id AS closure_id, c.service_id, s.nombre AS servicio_nombre, c.paso_actual,
                           r.nombre AS rol_pendiente, EXTRACT(day FROM now() - c.updated_at)::integer AS dias_pendiente
                    FROM closures c
                        JOIN services s ON s.id = c.service_id
                        LEFT JOIN approvals a ON a.closure_id = c.id AND a.estado::text = 'Pendiente'::text
                        LEFT JOIN roles r ON r.id = a.role_id
                    WHERE c.estado::text = ANY (ARRAY['Borrador','EnAprobacion','Rechazado']::text[]);
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Migración estructural irreversible (colapso Project→Service con pérdida de la
            // granularidad de proyecto). Para revertir, restaurar el backup previo a la Ola 1.
            throw new System.NotSupportedException(
                "RenameProjectActionToService es una migración de una sola dirección. Restaurar desde backup para revertir.");
        }
    }
}
