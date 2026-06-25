import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';

type Severidad = 'Bloqueante' | 'Aviso' | 'Info';

interface ErrorNominaRow {
  recurso: string;
  nif: string;
  accion: string;
  ceco: string;
  tipoError: string;
  detalle: string;
  severidad: Severidad;
  estado: string;
}

@Component({
  selector: 'app-errores-nomina',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatChipsModule, MatTableModule, MatFormFieldModule, MatSelectModule, MatInputModule],
  template: `
    <div class="sig-exec-page">
      <div class="sig-exec-header">
        <div class="sig-exec-titles">
          <h1 class="sig-exec-title">
            Errores de Nómina / Pagos
            <span class="badge-env">Entorno Demo</span>
            <span class="badge-periodo">Mayo 2026</span>
          </h1>
          <p class="sig-exec-sub">Validación del cierre de pagos antes de generar el fichero de nómina</p>
        </div>
      </div>

      <!-- Banner bloqueo parcial (SIG pendiente de catálogo) -->
      <div class="sig-banner-warn" role="alert">
        <mat-icon>warning_amber</mat-icon>
        <span>
          <strong>Pantalla bloqueada parcialmente.</strong> SIG indicó que la lista cerrada de tipos de error
          llegará «en otro documento» aún no entregado. Los tipos y severidades de abajo son una propuesta
          ilustrativa para fijar la estructura; se ajustarán al recibir el catálogo de validaciones de SIG.
        </span>
      </div>

      <!-- KPIs resumen -->
      <div class="sig-summary-grid">
        <div class="sig-summary-card">
          <div class="sig-summary-value">{{ recursosAfectados() }}</div>
          <div class="sig-summary-label">Recursos afectados</div>
        </div>
        <div class="sig-summary-card sig-summary-card--advertencia">
          <div class="sig-summary-value">{{ avisos() }}</div>
          <div class="sig-summary-label">Avisos</div>
        </div>
        <div class="sig-summary-card sig-summary-card--bloqueante">
          <div class="sig-summary-value">{{ bloqueantes() }}</div>
          <div class="sig-summary-label">Bloqueantes</div>
        </div>
        <div class="sig-summary-card">
          <div class="sig-summary-value">{{ incidencias().length }}</div>
          <div class="sig-summary-label">Incidencias detectadas</div>
        </div>
      </div>

      <!-- Filtros -->
      <div class="filtros">
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Año</mat-label>
          <mat-select [ngModel]="fAnio()" (ngModelChange)="fAnio.set($event)" data-testid="f-anio">
            <mat-option value="2026">2026</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Mes</mat-label>
          <mat-select [ngModel]="fMes()" (ngModelChange)="fMes.set($event)" data-testid="f-mes">
            <mat-option value="Mayo">Mayo</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic" class="rango">
          <mat-label>Rango de fechas</mat-label>
          <input matInput [ngModel]="fRango()" (ngModelChange)="fRango.set($event)" data-testid="f-rango" />
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Acción</mat-label>
          <mat-select multiple [ngModel]="fAcciones()" (ngModelChange)="fAcciones.set($event)" placeholder="Todas" data-testid="f-accion">
            <mat-option value="DAIKIN">DAIKIN</mat-option>
            <mat-option value="Granini GPVs">Granini GPVs</mat-option>
            <mat-option value="Amex Shop Small">Amex Shop Small</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Cliente</mat-label>
          <mat-select multiple [ngModel]="fClientes()" (ngModelChange)="fClientes.set($event)" placeholder="Todos" data-testid="f-cliente">
            <mat-option value="DAIKIN">DAIKIN</mat-option>
            <mat-option value="Granini">Granini</mat-option>
            <mat-option value="Amex">Amex</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Departamento</mat-label>
          <mat-select multiple [ngModel]="fDepartamentos()" (ngModelChange)="fDepartamentos.set($event)" placeholder="Todos" data-testid="f-departamento">
            <mat-option value="Operaciones">Operaciones</mat-option>
            <mat-option value="Comercial">Comercial</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Severidad</mat-label>
          <mat-select [ngModel]="fSeveridad()" (ngModelChange)="fSeveridad.set($event)" data-testid="f-severidad">
            <mat-option value="">Todas</mat-option>
            <mat-option value="Bloqueante">Bloqueante</mat-option>
            <mat-option value="Aviso">Aviso</mat-option>
            <mat-option value="Info">Info</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Estado</mat-label>
          <mat-select [ngModel]="fEstado()" (ngModelChange)="fEstado.set($event)" data-testid="f-estado">
            <mat-option value="Sin resolver">Sin resolver</mat-option>
            <mat-option value="Resuelto">Resuelto</mat-option>
            <mat-option value="">Todos</mat-option>
          </mat-select>
        </mat-form-field>
        <div class="filtros-acciones">
          <button mat-flat-button color="primary" data-testid="btn-filtrar">
            <mat-icon>filter_list</mat-icon> Filtrar
          </button>
          <button mat-stroked-button (click)="limpiar()" data-testid="btn-limpiar">
            <mat-icon>clear</mat-icon> Limpiar
          </button>
        </div>
      </div>

      <!-- Sección de validación -->
      <div class="sig-section-head">
        <h2 class="sig-section-title">Validación del cierre de pagos — Mayo 2026</h2>
        <button mat-stroked-button (click)="revalidar()" data-testid="btn-revalidar">
          <mat-icon>refresh</mat-icon> Revalidar
        </button>
      </div>

      <!-- Tabla de incidencias -->
      <table mat-table [dataSource]="incidencias()" class="sig-mat-table">
        <ng-container matColumnDef="recurso">
          <th mat-header-cell *matHeaderCellDef> Recurso </th>
          <td mat-cell *matCellDef="let r">
            <div class="recurso-nombre">{{ r.recurso }}</div>
            <div class="recurso-nif">{{ r.nif }}</div>
          </td>
        </ng-container>

        <ng-container matColumnDef="accion">
          <th mat-header-cell *matHeaderCellDef> Acción / CECO </th>
          <td mat-cell *matCellDef="let r">
            <div class="accion-nombre">{{ r.accion }}</div>
            <div class="accion-ceco">CECO {{ r.ceco }}</div>
          </td>
        </ng-container>

        <ng-container matColumnDef="tipoError">
          <th mat-header-cell *matHeaderCellDef> Tipo de error </th>
          <td mat-cell *matCellDef="let r">{{ r.tipoError }}</td>
        </ng-container>

        <ng-container matColumnDef="detalle">
          <th mat-header-cell *matHeaderCellDef> Detalle </th>
          <td mat-cell *matCellDef="let r"><span class="detalle">{{ r.detalle }}</span></td>
        </ng-container>

        <ng-container matColumnDef="severidad">
          <th mat-header-cell *matHeaderCellDef> Severidad </th>
          <td mat-cell *matCellDef="let r">
            <mat-chip [class]="'sig-chip--' + chipClass(r.severidad)">
              <mat-icon>{{ severidadIcon(r.severidad) }}</mat-icon>
              {{ r.severidad }}
            </mat-chip>
          </td>
        </ng-container>

        <ng-container matColumnDef="estado">
          <th mat-header-cell *matHeaderCellDef> Estado </th>
          <td mat-cell *matCellDef="let r">
            <mat-chip class="sig-chip--pendiente"><mat-icon>pending</mat-icon> {{ r.estado }}</mat-chip>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="sig-data-row"></tr>
      </table>

      <!-- Nota callout -->
      <div class="sig-callout">
        <mat-icon>info</mat-icon>
        <span>
          Recursos y CECOs reales (plantilla DAIKIN · Auditoría). El error «Sin contrato/llamamiento»
          refleja un gap real: el campo llamamiento/contrato no venía en la plantilla. Mientras existan
          incidencias bloqueantes, la pantalla de Pagos no debe permitir <strong>Generar fichero nómina</strong>.
        </span>
      </div>

      <!-- Barra de acciones inferior -->
      <div class="sig-action-bar">
        @if (bloqueantes() > 0) {
          <span class="bloqueo-msg">
            <mat-icon>block</mat-icon>
            {{ bloqueantes() }} incidencias bloqueantes — no se puede generar el fichero de nómina
          </span>
        }
        <span class="spacer"></span>
        <button mat-stroked-button (click)="marcarRevisado()" data-testid="btn-revisado">
          <mat-icon>done_all</mat-icon> Marcar como revisado
        </button>
        <button mat-flat-button color="primary" [disabled]="bloqueantes() > 0" data-testid="btn-generar-nomina">
          <mat-icon>description</mat-icon> Generar fichero nómina
        </button>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .sig-exec-page { padding: 28px 28px 40px; background: var(--sig-bg-app); min-height: 100vh; }
    .sig-exec-header { margin-bottom: 20px; }
    .sig-exec-title { font-size: 24px; font-weight: 700; color: var(--sig-text-heading); margin: 0 0 4px; display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
    .sig-exec-sub { font-size: 13px; color: var(--sig-text-muted); margin: 0; }

    .badge-env, .badge-periodo { font-size: 11px; font-weight: 600; padding: 3px 10px; border-radius: 10px; letter-spacing: .03em; }
    .badge-env { background: rgba(99,102,241,.12); color: #6366f1; border: 1px solid rgba(99,102,241,.25); }
    .badge-periodo { background: var(--sig-bg-hover); color: var(--sig-text-muted); border: 1px solid var(--sig-border); }

    .sig-banner-warn {
      display: flex; gap: 12px; align-items: flex-start;
      background: rgba(245,158,11,.1); border: 1px solid rgba(245,158,11,.35); border-left: 4px solid #f59e0b;
      border-radius: 10px; padding: 14px 16px; margin-bottom: 24px; font-size: 13px; line-height: 1.5; color: var(--sig-text-primary);
    }
    .sig-banner-warn mat-icon { color: #f59e0b; flex-shrink: 0; }

    .sig-summary-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 24px; }
    @media (max-width: 900px) { .sig-summary-grid { grid-template-columns: repeat(2, 1fr); } }
    .sig-summary-card { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; padding: 20px; text-align: center; }
    .sig-summary-value { font-size: 32px; font-weight: 700; color: var(--sig-text-heading); font-family: 'Roboto Mono', monospace; }
    .sig-summary-label { font-size: 12px; color: var(--sig-text-muted); text-transform: uppercase; letter-spacing: .05em; margin-top: 4px; }
    .sig-summary-card--bloqueante .sig-summary-value { color: #ef4444; }
    .sig-summary-card--advertencia .sig-summary-value { color: #f59e0b; }

    .filtros { display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 24px; align-items: center; }
    .filtros mat-form-field { width: 180px; }
    .filtros .rango { width: 220px; }
    .filtros-acciones { display: flex; gap: 8px; }

    .sig-section-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
    .sig-section-title { font-size: 16px; font-weight: 700; color: var(--sig-text-heading); margin: 0; }

    .sig-mat-table {
      width: 100%; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden;
      border-collapse: collapse;
      th.mat-mdc-header-cell { color: var(--sig-text-muted); font-size: 11px; font-weight: 700; letter-spacing: .05em; text-transform: uppercase; border-bottom-color: var(--sig-border); padding: 12px; }
      td.mat-mdc-cell { color: var(--sig-text-primary); font-size: 13px; border-bottom-color: var(--sig-border); padding: 12px; vertical-align: top; }
    }
    .sig-data-row:hover { background: var(--sig-bg-hover); }
    .recurso-nombre, .accion-nombre { font-weight: 600; color: var(--sig-text-heading); }
    .recurso-nif, .accion-ceco { font-size: 11px; color: var(--sig-text-muted); margin-top: 2px; }
    .detalle { color: var(--sig-text-muted); }

    .sig-chip--bloqueante { background: rgba(239,68,68,.1); color: #ef4444 !important; border: 1px solid rgba(239,68,68,.2); }
    .sig-chip--advertencia { background: rgba(245,158,11,.1); color: #f59e0b !important; border: 1px solid rgba(245,158,11,.2); }
    .sig-chip--info { background: rgba(59,130,246,.1); color: #3b82f6 !important; border: 1px solid rgba(59,130,246,.2); }
    .sig-chip--pendiente { background: rgba(245,158,11,.1); color: #f59e0b !important; border: 1px solid rgba(245,158,11,.2); }

    .sig-callout {
      display: flex; gap: 12px; align-items: flex-start;
      background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-left: 4px solid #3b82f6;
      border-radius: 10px; padding: 14px 16px; margin: 20px 0; font-size: 13px; line-height: 1.5; color: var(--sig-text-primary);
    }
    .sig-callout mat-icon { color: #3b82f6; flex-shrink: 0; }

    .sig-action-bar {
      display: flex; align-items: center; gap: 12px; flex-wrap: wrap;
      background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; padding: 14px 16px;
    }
    .sig-action-bar .spacer { flex: 1; }
    .bloqueo-msg { display: inline-flex; align-items: center; gap: 6px; color: #ef4444; font-weight: 600; font-size: 13px; }
    .bloqueo-msg mat-icon { color: #ef4444; }
  `],
})
export class ErroresNominaComponent {
  protected readonly displayedColumns = ['recurso', 'accion', 'tipoError', 'detalle', 'severidad', 'estado'];

  // Filtros (maqueta) — valores por defecto del spec penpot.
  protected readonly fAnio = signal('2026');
  protected readonly fMes = signal('Mayo');
  protected readonly fRango = signal('01/05/2026 – 31/05/2026');
  protected readonly fAcciones = signal<string[]>([]);
  protected readonly fClientes = signal<string[]>([]);
  protected readonly fDepartamentos = signal<string[]>([]);
  protected readonly fSeveridad = signal('');
  protected readonly fEstado = signal('Sin resolver');

  // Datos ilustrativos embebidos (plantilla DAIKIN · Auditoría).
  protected readonly incidencias = signal<ErrorNominaRow[]>([
    {
      recurso: 'Castellsagués Nogueras, Nil',
      nif: 'NIF 77124973Q',
      accion: 'DAIKIN',
      ceco: '023301',
      tipoError: 'Sin contrato/llamamiento',
      detalle: 'El recurso no tiene llamamiento (contrato) asignado en el periodo.',
      severidad: 'Bloqueante',
      estado: 'Sin resolver',
    },
    {
      recurso: 'Martín Luque, Alejandro',
      nif: 'NIF 74889553N',
      accion: 'DAIKIN',
      ceco: '023301',
      tipoError: 'DNI no localizado en A3 Innuva',
      detalle: 'No se encuentra el trabajador en el maestro de nóminas para volcar el fichero.',
      severidad: 'Bloqueante',
      estado: 'Sin resolver',
    },
    {
      recurso: 'Castellsagués Nogueras, Nil',
      nif: 'NIF 77124973Q',
      accion: 'DAIKIN',
      ceco: '023301',
      tipoError: 'Concepto manual sin observación',
      detalle: '«Plus desplazamiento (+30,00 €)» registrado sin texto justificativo.',
      severidad: 'Aviso',
      estado: 'Sin resolver',
    },
    {
      recurso: 'Soler, Sergi',
      nif: '— · NIF no facilitado',
      accion: 'Granini GPVs',
      ceco: '025888',
      tipoError: 'Importe fuera de rango',
      detalle: 'Kilometraje (no Payhawk) supera el máximo configurado para la acción.',
      severidad: 'Aviso',
      estado: 'Sin resolver',
    },
    {
      recurso: 'Pastor, Antonio',
      nif: '— · NIF no facilitado',
      accion: 'Amex Shop Small',
      ceco: '035501',
      tipoError: 'Periodo no abierto',
      detalle: 'Existen pagos con fecha fuera del rango del periodo activo (revisar en Periodos).',
      severidad: 'Info',
      estado: 'Sin resolver',
    },
  ]);

  protected readonly recursosAfectados = computed(
    () => new Set(this.incidencias().map(i => i.nif)).size,
  );
  protected readonly avisos = computed(
    () => this.incidencias().filter(i => i.severidad === 'Aviso').length,
  );
  protected readonly bloqueantes = computed(
    () => this.incidencias().filter(i => i.severidad === 'Bloqueante').length,
  );

  protected chipClass(sev: Severidad): string {
    switch (sev) {
      case 'Bloqueante': return 'bloqueante';
      case 'Aviso': return 'advertencia';
      default: return 'info';
    }
  }

  protected severidadIcon(sev: Severidad): string {
    switch (sev) {
      case 'Bloqueante': return 'block';
      case 'Aviso': return 'warning';
      default: return 'info';
    }
  }

  protected limpiar(): void {
    this.fAcciones.set([]);
    this.fClientes.set([]);
    this.fDepartamentos.set([]);
    this.fSeveridad.set('');
    this.fEstado.set('Sin resolver');
  }

  protected revalidar(): void {
    // Maqueta: en la versión real dispararía la revalidación del cierre de pagos.
  }

  protected marcarRevisado(): void {
    // Maqueta: marcaría las incidencias revisadas sin levantar el bloqueo.
  }
}
