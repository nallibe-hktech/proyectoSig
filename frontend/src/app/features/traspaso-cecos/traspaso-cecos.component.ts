import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

/** Una fila del informe de traspasos entre CECOs (maqueta, datos ilustrativos). */
interface TraspasoRow {
  anio: number;
  mes: string;
  deptoOrigen: string;
  proyectoOrigen: string;
  cecoOrigen: string;
  deptoDestino: string;
  proyectoDestino: string;
  cecoDestino: string;
  /** Importe bruto traspasado, en euros. */
  bruto: number;
  /** Importe de gastos asociado, en euros. */
  gastos: number;
}

@Component({
  selector: 'app-traspaso-cecos',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, MatIconModule, MatButtonModule, MatTableModule, MatFormFieldModule, MatSelectModule],
  template: `
    <div class="sig-exec-page">
      <!-- Breadcrumb -->
      <nav class="breadcrumb">
        <a routerLink="/">Inicio</a>
        <span class="sep">&rsaquo;</span>
        <span>Configuración</span>
        <span class="sep">&rsaquo;</span>
        <span class="current">Traspaso entre CECOs</span>
      </nav>

      <div class="sig-exec-header">
        <div class="sig-exec-titles">
          <h1 class="sig-exec-title">
            Traspaso entre CECOs
            <span class="hdr-badge hdr-badge--info">Informe</span>
            <span class="hdr-badge hdr-badge--period">Mayo 2026</span>
          </h1>
          <p class="sig-exec-sub">Configuración · Traspasos entre centros de coste</p>
        </div>
      </div>

      <!-- Banner explicativo -->
      <div class="sig-banner">
        <mat-icon>info</mat-icon>
        <p>
          Informe de solo lectura. Suma únicamente conceptos de pago en los que el CECO del contrato
          (origen, datos de A3 Innuva) no coincide con el CECO real de la actividad
          (destino, datos de Payhawk / Celero / TravelPerk). Los pagos cuyo contrato y actividad comparten
          CECO se excluyen. Si ambos CECOs pertenecen al mismo departamento, se excluyen los totales &lt; 1.000 €.
        </p>
      </div>

      <!-- Filtros -->
      <div class="filtros">
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>CECO Destino</mat-label>
          <mat-select [ngModel]="fCecoDestino()" (ngModelChange)="fCecoDestino.set($event)" data-testid="f-ceco-destino">
            <mat-option value="">Todos</mat-option>
            @for (c of cecosDestino(); track c) { <mat-option [value]="c">{{ c }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Proyecto Destino</mat-label>
          <mat-select [ngModel]="fProyectoDestino()" (ngModelChange)="fProyectoDestino.set($event)" data-testid="f-proyecto-destino">
            <mat-option value="">Todos</mat-option>
            @for (p of proyectosDestino(); track p) { <mat-option [value]="p">{{ p }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Depto. Destino</mat-label>
          <mat-select [ngModel]="fDeptoDestino()" (ngModelChange)="fDeptoDestino.set($event)" data-testid="f-depto-destino">
            <mat-option value="">Todos</mat-option>
            @for (d of deptosDestino(); track d) { <mat-option [value]="d">{{ d }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>CECO Origen</mat-label>
          <mat-select [ngModel]="fCecoOrigen()" (ngModelChange)="fCecoOrigen.set($event)" data-testid="f-ceco-origen">
            <mat-option value="">Todos</mat-option>
            @for (c of cecosOrigen(); track c) { <mat-option [value]="c">{{ c }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Proyecto Origen</mat-label>
          <mat-select [ngModel]="fProyectoOrigen()" (ngModelChange)="fProyectoOrigen.set($event)" data-testid="f-proyecto-origen">
            <mat-option value="">Todos</mat-option>
            @for (p of proyectosOrigen(); track p) { <mat-option [value]="p">{{ p }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Depto. Origen</mat-label>
          <mat-select [ngModel]="fDeptoOrigen()" (ngModelChange)="fDeptoOrigen.set($event)" data-testid="f-depto-origen">
            <mat-option value="">Todos</mat-option>
            @for (d of deptosOrigen(); track d) { <mat-option [value]="d">{{ d }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Mes</mat-label>
          <mat-select [ngModel]="fMes()" (ngModelChange)="fMes.set($event)" data-testid="f-mes">
            <mat-option value="">Todos</mat-option>
            @for (m of meses(); track m) { <mat-option [value]="m">{{ m }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Año</mat-label>
          <mat-select [ngModel]="fAnio()" (ngModelChange)="fAnio.set($event)" data-testid="f-anio">
            <mat-option value="">Todos</mat-option>
            @for (a of anios(); track a) { <mat-option [value]="a">{{ a }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Cierre</mat-label>
          <mat-select [ngModel]="fCierre()" (ngModelChange)="fCierre.set($event)" data-testid="f-cierre">
            <mat-option value="">Todos</mat-option>
            <mat-option value="abierto">Abierto</mat-option>
            <mat-option value="cerrado">Cerrado</mat-option>
          </mat-select>
        </mat-form-field>
        <div class="filtros-acciones">
          <button mat-flat-button color="primary" (click)="aplicar()" data-testid="f-filtrar">Filtrar</button>
          <button mat-stroked-button (click)="limpiar()" data-testid="f-limpiar">Limpiar</button>
        </div>
      </div>

      <!-- KPIs -->
      <div class="sig-summary-grid">
        <div class="sig-summary-card">
          <div class="sig-summary-value">{{ cecosImplicados() }}</div>
          <div class="sig-summary-label">CECOs implicados</div>
        </div>
        <div class="sig-summary-card">
          <div class="sig-summary-value">{{ traspasosDetectados() }}</div>
          <div class="sig-summary-label">Traspasos detectados</div>
        </div>
        <div class="sig-summary-card sig-summary-card--gastos">
          <div class="sig-summary-value">{{ totalGastos() | number:'1.2-2':'es' }} €</div>
          <div class="sig-summary-label">Total gastos</div>
        </div>
        <div class="sig-summary-card sig-summary-card--bruto">
          <div class="sig-summary-value">{{ totalBruto() | number:'1.2-2':'es' }} €</div>
          <div class="sig-summary-label">Total traspasado (bruto)</div>
        </div>
      </div>

      <!-- Tabla -->
      <div class="tabla-header">
        <div>
          <h2 class="tabla-title">Traspasos entre CECOs · 2026</h2>
          <p class="tabla-sub">{{ filtered().length }} registros · agrupado por CECO origen &rarr; destino</p>
        </div>
        <div class="leyenda">
          <span class="leyenda-item"><span class="dot dot--mismo"></span> Mismo departamento</span>
          <span class="leyenda-item"><span class="dot dot--distinto"></span> Distinto departamento</span>
          <span class="leyenda-warn">⚠ color provisional</span>
        </div>
      </div>

      <table mat-table [dataSource]="filtered()" class="sig-mat-table">
        <ng-container matColumnDef="anio">
          <th mat-header-cell *matHeaderCellDef> Año </th>
          <td mat-cell *matCellDef="let r">{{ r.anio }}</td>
        </ng-container>
        <ng-container matColumnDef="mes">
          <th mat-header-cell *matHeaderCellDef> Mes </th>
          <td mat-cell *matCellDef="let r">{{ r.mes }}</td>
        </ng-container>
        <ng-container matColumnDef="deptoOrigen">
          <th mat-header-cell *matHeaderCellDef> Depto. Origen </th>
          <td mat-cell *matCellDef="let r">{{ r.deptoOrigen }}</td>
        </ng-container>
        <ng-container matColumnDef="proyectoOrigen">
          <th mat-header-cell *matHeaderCellDef> Proyecto Origen </th>
          <td mat-cell *matCellDef="let r">{{ r.proyectoOrigen }}</td>
        </ng-container>
        <ng-container matColumnDef="cecoOrigen">
          <th mat-header-cell *matHeaderCellDef> CECO Origen </th>
          <td mat-cell *matCellDef="let r"><code>{{ r.cecoOrigen }}</code></td>
        </ng-container>
        <ng-container matColumnDef="deptoDestino">
          <th mat-header-cell *matHeaderCellDef> Depto. Destino </th>
          <td mat-cell *matCellDef="let r">{{ r.deptoDestino }}</td>
        </ng-container>
        <ng-container matColumnDef="proyectoDestino">
          <th mat-header-cell *matHeaderCellDef> Proyecto Destino </th>
          <td mat-cell *matCellDef="let r">{{ r.proyectoDestino }}</td>
        </ng-container>
        <ng-container matColumnDef="cecoDestino">
          <th mat-header-cell *matHeaderCellDef> CECO Destino </th>
          <td mat-cell *matCellDef="let r"><code>{{ r.cecoDestino }}</code></td>
        </ng-container>
        <ng-container matColumnDef="bruto">
          <th mat-header-cell *matHeaderCellDef class="num"> Bruto </th>
          <td mat-cell *matCellDef="let r" class="num">{{ r.bruto | number:'1.2-2':'es' }} €</td>
        </ng-container>
        <ng-container matColumnDef="gastos">
          <th mat-header-cell *matHeaderCellDef class="num"> Gastos </th>
          <td mat-cell *matCellDef="let r" class="num">{{ r.gastos | number:'1.2-2':'es' }} €</td>
        </ng-container>

        <!-- Fila Total -->
        <ng-container matColumnDef="totalLabel">
          <td mat-footer-cell *matFooterCellDef [attr.colspan]="8" class="total-label">Total</td>
        </ng-container>
        <ng-container matColumnDef="totalBruto">
          <td mat-footer-cell *matFooterCellDef class="num total-num">{{ totalBruto() | number:'1.2-2':'es' }} €</td>
        </ng-container>
        <ng-container matColumnDef="totalGastos">
          <td mat-footer-cell *matFooterCellDef class="num total-num">{{ totalGastos() | number:'1.2-2':'es' }} €</td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let r; columns: displayedColumns;"
            [class.row--mismo]="r.deptoOrigen === r.deptoDestino"
            [class.row--distinto]="r.deptoOrigen !== r.deptoDestino"></tr>
        <tr mat-footer-row *matFooterRowDef="footerColumns" class="total-row"></tr>
      </table>

      <!-- Callout pendiente de SIG -->
      <div class="sig-callout sig-callout--warn">
        <mat-icon>warning</mat-icon>
        <p>
          ⚠ Pendiente de SIG (Yoana / Martha): confirmar el listado exacto de columnas de importe
          (aquí Bruto y Gastos según la leyenda), qué selecciona el filtro «Cierre», el significado del
          color de fila (provisional: ámbar = mismo departamento, verde = distinto departamento) y si
          se requieren subtotales (p. ej. por departamento, dado que la regla de los 1.000 € va por departamento).
        </p>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .sig-exec-page { padding: 28px 28px 40px; background: var(--sig-bg-app); min-height: 100vh; }

    .breadcrumb { font-size: 12px; color: var(--sig-text-muted); margin-bottom: 16px; display: flex; align-items: center; gap: 6px; }
    .breadcrumb a { color: var(--sig-text-muted); text-decoration: none; &:hover { text-decoration: underline; } }
    .breadcrumb .sep { opacity: .5; }
    .breadcrumb .current { color: var(--sig-text-primary); font-weight: 600; }

    .sig-exec-header { margin-bottom: 20px; }
    .sig-exec-title { font-size: 24px; font-weight: 700; color: var(--sig-text-heading); margin: 0 0 4px; display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
    .sig-exec-sub { font-size: 13px; color: var(--sig-text-muted); margin: 0; }
    .hdr-badge { font-size: 11px; font-weight: 700; padding: 3px 10px; border-radius: 12px; letter-spacing: .03em; }
    .hdr-badge--info { background: rgba(59,130,246,.12); color: #3b82f6; border: 1px solid rgba(59,130,246,.25); }
    .hdr-badge--period { background: var(--sig-bg-hover); color: var(--sig-text-muted); border: 1px solid var(--sig-border); }

    .sig-banner {
      display: flex; gap: 12px; align-items: flex-start;
      background: rgba(59,130,246,.06); border: 1px solid rgba(59,130,246,.2); border-radius: 12px;
      padding: 14px 18px; margin-bottom: 24px;
    }
    .sig-banner mat-icon { color: #3b82f6; flex-shrink: 0; }
    .sig-banner p { margin: 0; font-size: 13px; color: var(--sig-text-primary); line-height: 1.5; }

    .filtros { display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 24px; align-items: flex-start; }
    .filtros mat-form-field { width: 180px; }
    .filtros-acciones { display: flex; gap: 8px; align-self: center; }

    .sig-summary-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 28px; }
    @media (max-width: 900px) { .sig-summary-grid { grid-template-columns: repeat(2, 1fr); } }
    .sig-summary-card {
      background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px;
      padding: 20px; text-align: center;
    }
    .sig-summary-value { font-size: 30px; font-weight: 700; color: var(--sig-text-heading); font-family: 'Roboto Mono', monospace; }
    .sig-summary-label { font-size: 12px; color: var(--sig-text-muted); text-transform: uppercase; letter-spacing: .05em; margin-top: 4px; }
    .sig-summary-card--gastos .sig-summary-value { color: #f59e0b; }
    .sig-summary-card--bruto .sig-summary-value { color: #3b82f6; }

    .tabla-header { display: flex; justify-content: space-between; align-items: flex-end; margin-bottom: 12px; flex-wrap: wrap; gap: 12px; }
    .tabla-title { font-size: 16px; font-weight: 700; color: var(--sig-text-heading); margin: 0 0 2px; }
    .tabla-sub { font-size: 12px; color: var(--sig-text-muted); margin: 0; }
    .leyenda { display: flex; align-items: center; gap: 16px; font-size: 12px; color: var(--sig-text-muted); }
    .leyenda-item { display: inline-flex; align-items: center; gap: 6px; }
    .dot { width: 12px; height: 12px; border-radius: 3px; display: inline-block; }
    .dot--mismo { background: #f59e0b; }
    .dot--distinto { background: #22c55e; }
    .leyenda-warn { color: #f59e0b; font-weight: 600; }

    .sig-mat-table {
      width: 100%; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden;
      border-collapse: collapse;
      th.mat-mdc-header-cell { color: var(--sig-text-muted); font-size: 11px; font-weight: 700; letter-spacing: .05em; text-transform: uppercase; border-bottom-color: var(--sig-border); padding: 12px; }
      td.mat-mdc-cell { color: var(--sig-text-primary); font-size: 13px; border-bottom-color: var(--sig-border); padding: 12px; }
      td.mat-mdc-cell code { background: var(--sig-bg-hover); padding: 2px 6px; border-radius: 4px; font-size: 12px; }
      .num { text-align: right; font-variant-numeric: tabular-nums; }
      td.num { font-family: 'Roboto Mono', monospace; }
    }
    .row--mismo td.mat-mdc-cell { background: rgba(245,158,11,.07); }
    .row--distinto td.mat-mdc-cell { background: rgba(34,197,94,.05); }
    .total-row td.mat-mdc-footer-cell { border-top: 2px solid var(--sig-border); font-weight: 700; color: var(--sig-text-heading); padding: 12px; font-size: 13px; }
    .total-label { text-align: right; text-transform: uppercase; letter-spacing: .05em; }
    .total-num { text-align: right; font-family: 'Roboto Mono', monospace; }

    .sig-callout {
      display: flex; gap: 12px; align-items: flex-start; border-radius: 12px; padding: 14px 18px; margin-top: 24px;
    }
    .sig-callout p { margin: 0; font-size: 13px; line-height: 1.5; }
    .sig-callout--warn { background: rgba(245,158,11,.08); border: 1px solid rgba(245,158,11,.25); }
    .sig-callout--warn mat-icon { color: #f59e0b; flex-shrink: 0; }
    .sig-callout--warn p { color: var(--sig-text-primary); }
  `],
})
export class TraspasoCecosComponent {
  protected readonly displayedColumns = ['anio', 'mes', 'deptoOrigen', 'proyectoOrigen', 'cecoOrigen', 'deptoDestino', 'proyectoDestino', 'cecoDestino', 'bruto', 'gastos'];
  protected readonly footerColumns = ['totalLabel', 'totalBruto', 'totalGastos'];

  // Datos ilustrativos embebidos (maqueta solo-lectura).
  protected readonly rows = signal<TraspasoRow[]>([
    { anio: 2026, mes: 'Mayo', deptoOrigen: 'Field DAIKIN', proyectoOrigen: 'DAIKIN Madrid', cecoOrigen: '023301', deptoDestino: 'Field DAIKIN', proyectoDestino: 'DAIKIN Barcelona', cecoDestino: '023302', bruto: 1675.00, gastos: 245.30 },
    { anio: 2026, mes: 'Mayo', deptoOrigen: 'Field DAIKIN', proyectoOrigen: 'DAIKIN Madrid', cecoOrigen: '023301', deptoDestino: 'Sales Optimising', proyectoDestino: 'Coty Verano 26', cecoDestino: '031540', bruto: 1268.93, gastos: 118.40 },
    { anio: 2026, mes: 'Mayo', deptoOrigen: 'Gestión Personas', proyectoOrigen: 'Granini GPVs', cecoOrigen: '025888', deptoDestino: 'Sales Optimising', proyectoDestino: 'Amex Shop Small', cecoDestino: '035501', bruto: 980.00, gastos: 64.10 },
    { anio: 2026, mes: 'Abril', deptoOrigen: 'Gestión Personas', proyectoOrigen: 'Granini GPVs', cecoOrigen: '025888', deptoDestino: 'Gestión Personas', proyectoDestino: 'JDE Rutas', cecoDestino: '025890', bruto: 1540.00, gastos: 132.75 },
    { anio: 2026, mes: 'Mayo', deptoOrigen: 'Sales Optimising', proyectoOrigen: 'Amex Shop Small', cecoOrigen: '035501', deptoDestino: 'Field DAIKIN', proyectoDestino: 'DAIKIN Madrid', cecoDestino: '023301', bruto: 2110.00, gastos: 305.00 },
    { anio: 2026, mes: 'Mayo', deptoOrigen: 'Sales Optimising', proyectoOrigen: 'Amex New', cecoOrigen: '035502', deptoDestino: 'Sales Optimising', proyectoDestino: 'Apple BA', cecoDestino: '036010', bruto: 1430.50, gastos: 96.20 },
    { anio: 2025, mes: 'Dic.', deptoOrigen: 'Sales Optimising', proyectoOrigen: 'Apple Formaciones', cecoOrigen: '036012', deptoDestino: 'Gestión Personas', proyectoDestino: 'ITC GPVs', cecoDestino: '030120', bruto: 1890.00, gastos: 210.45 },
    { anio: 2026, mes: 'Mayo', deptoOrigen: 'Field DAIKIN', proyectoOrigen: 'DAIKIN Barcelona', cecoOrigen: '023302', deptoDestino: 'Sales Optimising', proyectoDestino: 'Coty Implantaciones', cecoDestino: '031541', bruto: 760.00, gastos: 41.60 },
    { anio: 2026, mes: 'Abril', deptoOrigen: 'Gestión Personas', proyectoOrigen: 'Granini GPVs', cecoOrigen: '025888', deptoDestino: 'Sales Optimising', proyectoDestino: 'Apple BA', cecoDestino: '036010', bruto: 1205.00, gastos: 88.90 },
    { anio: 2026, mes: 'Mayo', deptoOrigen: 'Sales Optimising', proyectoOrigen: 'Amex Shop Small', cecoOrigen: '035501', deptoDestino: 'Sales Optimising', proyectoDestino: 'NPI Watch 03-26', cecoDestino: '035503', bruto: 3250.00, gastos: 412.00 },
    { anio: 2026, mes: 'Marzo', deptoOrigen: 'Field DAIKIN', proyectoOrigen: 'DAIKIN Madrid', cecoOrigen: '023301', deptoDestino: 'Gestión Personas', proyectoDestino: 'JDE Rutas', cecoDestino: '025890', bruto: 1015.40, gastos: 73.15 },
    { anio: 2026, mes: 'Mayo', deptoOrigen: 'Sales Optimising', proyectoOrigen: 'Amex New', cecoOrigen: '035502', deptoDestino: 'Field DAIKIN', proyectoDestino: 'DAIKIN Barcelona', cecoDestino: '023302', bruto: 845.00, gastos: 52.30 },
  ]);

  // Filtros (signals). Año por defecto = 2026; el resto "Todos".
  protected readonly fCecoDestino = signal('');
  protected readonly fProyectoDestino = signal('');
  protected readonly fDeptoDestino = signal('');
  protected readonly fCecoOrigen = signal('');
  protected readonly fProyectoOrigen = signal('');
  protected readonly fDeptoOrigen = signal('');
  protected readonly fMes = signal('');
  // El penpot muestra los 12 registros del informe (incluido 1 de 2025). El badge "2026"
  // de cabecera es contextual; el select Año arranca en "Todos" para reproducir los KPIs del spec.
  protected readonly fAnio = signal<number | ''>('');
  protected readonly fCierre = signal('');

  // Opciones de filtro derivadas de los datos.
  private uniq<T>(values: readonly T[]): T[] {
    return [...new Set(values)];
  }
  protected readonly cecosDestino = computed(() => this.uniq(this.rows().map(r => r.cecoDestino)).sort());
  protected readonly proyectosDestino = computed(() => this.uniq(this.rows().map(r => r.proyectoDestino)).sort());
  protected readonly deptosDestino = computed(() => this.uniq(this.rows().map(r => r.deptoDestino)).sort());
  protected readonly cecosOrigen = computed(() => this.uniq(this.rows().map(r => r.cecoOrigen)).sort());
  protected readonly proyectosOrigen = computed(() => this.uniq(this.rows().map(r => r.proyectoOrigen)).sort());
  protected readonly deptosOrigen = computed(() => this.uniq(this.rows().map(r => r.deptoOrigen)).sort());
  protected readonly meses = computed(() => this.uniq(this.rows().map(r => r.mes)));
  protected readonly anios = computed(() => this.uniq(this.rows().map(r => r.anio)).sort((a, b) => b - a));

  protected readonly filtered = computed<TraspasoRow[]>(() => {
    const cd = this.fCecoDestino(), pd = this.fProyectoDestino(), dd = this.fDeptoDestino();
    const co = this.fCecoOrigen(), po = this.fProyectoOrigen(), dor = this.fDeptoOrigen();
    const mes = this.fMes(), anio = this.fAnio();
    return this.rows().filter(r =>
      (!cd || r.cecoDestino === cd)
      && (!pd || r.proyectoDestino === pd)
      && (!dd || r.deptoDestino === dd)
      && (!co || r.cecoOrigen === co)
      && (!po || r.proyectoOrigen === po)
      && (!dor || r.deptoOrigen === dor)
      && (!mes || r.mes === mes)
      && (anio === '' || r.anio === anio));
  });

  // KPIs derivados de las filas filtradas.
  protected readonly traspasosDetectados = computed(() => this.filtered().length);
  protected readonly cecosImplicados = computed(() =>
    this.uniq([...this.filtered().map(r => r.cecoOrigen), ...this.filtered().map(r => r.cecoDestino)]).length);
  protected readonly totalBruto = computed(() => this.filtered().reduce((acc, r) => acc + r.bruto, 0));
  protected readonly totalGastos = computed(() => this.filtered().reduce((acc, r) => acc + r.gastos, 0));

  protected aplicar(): void {
    // Maqueta: el filtrado es reactivo vía signals; este botón es ilustrativo.
  }

  protected limpiar(): void {
    this.fCecoDestino.set('');
    this.fProyectoDestino.set('');
    this.fDeptoDestino.set('');
    this.fCecoOrigen.set('');
    this.fProyectoOrigen.set('');
    this.fDeptoOrigen.set('');
    this.fMes.set('');
    this.fAnio.set('');
    this.fCierre.set('');
  }
}
