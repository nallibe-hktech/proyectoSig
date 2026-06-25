import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';

type EstadoTipoA = 'Alerta' | 'OK' | 'Sin presup.';
type EstadoTipoB = 'OK' | 'Alerta';

interface FilaTipoA {
  accion: string;
  ceco: string;
  presupuesto: number | null;
  facturado: number;
  desviacion: number | null;
  desvPct: number | null;
  estado: EstadoTipoA;
}

interface FilaTipoB {
  accion: string;
  ceco: string;
  facturado: number;
  costes: number;
  resultado: number;
  margenPct: number;
  estado: EstadoTipoB;
  verExceso?: boolean;
}

interface PartidaExceso {
  partida: string;
  origen: string;
  importe: number;
  pctSobreFacturado: number;
}

@Component({
  selector: 'app-errores-facturacion',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, MatIconModule, MatButtonModule, MatChipsModule, MatTableModule, MatFormFieldModule, MatSelectModule, MatInputModule],
  template: `
    <div class="sig-exec-page">
      <div class="sig-exec-header">
        <div class="sig-exec-titles">
          <h1 class="sig-exec-title">
            Errores de Facturación — Alertas de desviación
            <span class="env-badge env-badge--demo">Entorno Demo</span>
            <span class="env-badge env-badge--mes">Mayo 2026</span>
          </h1>
          <p class="sig-exec-sub">Conceptos de facturación con alertas bloqueantes y avisos por desviación</p>
        </div>
      </div>

      <!-- Filtros -->
      <div class="filtros">
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Departamento</mat-label>
          <mat-select multiple [ngModel]="fDepartamento()" (ngModelChange)="fDepartamento.set($event)" data-testid="ef-departamento">
            <mat-option value="Operaciones">Operaciones</mat-option>
            <mat-option value="Logística">Logística</mat-option>
            <mat-option value="Comercial">Comercial</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Cliente</mat-label>
          <mat-select multiple [ngModel]="fCliente()" (ngModelChange)="fCliente.set($event)" data-testid="ef-cliente">
            <mat-option value="DAIKIN">DAIKIN</mat-option>
            <mat-option value="Amex">Amex</mat-option>
            <mat-option value="Granini">Granini</mat-option>
            <mat-option value="Apple">Apple</mat-option>
            <mat-option value="ITC">ITC</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Acción</mat-label>
          <mat-select multiple [ngModel]="fAccion()" (ngModelChange)="fAccion.set($event)" data-testid="ef-accion">
            <mat-option value="GPVs">GPVs</mat-option>
            <mat-option value="Formaciones">Formaciones</mat-option>
            <mat-option value="Shop Small">Shop Small</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Rango de fechas</mat-label>
          <input matInput [ngModel]="fRango()" (ngModelChange)="fRango.set($event)" data-testid="ef-rango" />
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Año / Mes</mat-label>
          <mat-select [ngModel]="fAnioMes()" (ngModelChange)="fAnioMes.set($event)" data-testid="ef-aniomes">
            <mat-option value="2026-05">2026 · Mayo</mat-option>
            <mat-option value="2026-04">2026 · Abril</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Tipo de alerta</mat-label>
          <mat-select [ngModel]="fTipoAlerta()" (ngModelChange)="fTipoAlerta.set($event)" data-testid="ef-tipoalerta">
            <mat-option value="">Todas</mat-option>
            <mat-option value="presupuesto">Vs presupuesto</mat-option>
            <mat-option value="costes">Vs costes</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Severidad</mat-label>
          <mat-select [ngModel]="fSeveridad()" (ngModelChange)="fSeveridad.set($event)" data-testid="ef-severidad">
            <mat-option value="">Todas</mat-option>
            <mat-option value="bloqueante">Bloqueante</mat-option>
            <mat-option value="advertencia">Advertencia</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Estado</mat-label>
          <mat-select [ngModel]="fEstado()" (ngModelChange)="fEstado.set($event)" data-testid="ef-estado">
            <mat-option value="sin-revisar">Sin revisar</mat-option>
            <mat-option value="revisadas">Revisadas</mat-option>
          </mat-select>
        </mat-form-field>
        <div class="filtros-acciones">
          <button mat-flat-button color="primary" data-testid="ef-filtrar"><mat-icon>filter_alt</mat-icon> Filtrar</button>
          <button mat-stroked-button (click)="limpiar()" data-testid="ef-limpiar"><mat-icon>clear</mat-icon> Limpiar</button>
        </div>
      </div>

      <!-- Banner nota informativo -->
      <div class="sig-banner sig-banner--info">
        <mat-icon>info</mat-icon>
        <span>Esta pantalla analiza los conceptos de facturación con la misma lógica que Errores de Nómina/Pagos
          (alertas bloqueantes vs avisos). SIG propone una posible vista única ERRORES NÓMINA / FACTURACIÓN; aquí se
          mantiene separada hasta confirmarlo.</span>
      </div>

      <!-- KPIs -->
      <div class="sig-summary-grid">
        <div class="sig-summary-card">
          <div class="sig-summary-value">5</div>
          <div class="sig-summary-label">Acciones evaluadas</div>
        </div>
        <div class="sig-summary-card sig-summary-card--advertencia">
          <div class="sig-summary-value">2</div>
          <div class="sig-summary-label">Alerta vs presupuesto</div>
        </div>
        <div class="sig-summary-card sig-summary-card--bloqueante">
          <div class="sig-summary-value">1</div>
          <div class="sig-summary-label">Alerta facturado &lt; costes</div>
        </div>
        <div class="sig-summary-card">
          <div class="sig-summary-value">± 5 %&nbsp;⚠</div>
          <div class="sig-summary-label">Umbral desviación</div>
        </div>
      </div>

      <!-- ===== Tipo A ===== -->
      <section class="sig-block">
        <div class="sig-block-head">
          <div>
            <h2 class="sig-block-title">Tipo A · Desviación facturado vs presupuesto — Mayo 2026</h2>
            <p class="sig-block-sub">Umbral de alerta ± 5 % ⚠</p>
          </div>
          <button mat-stroked-button data-testid="ef-configurar-a"><mat-icon>settings</mat-icon> Configurar</button>
        </div>

        <div class="sig-note sig-note--warn">
          <mat-icon>warning</mat-icon>
          <span>La pantalla alerta cuando la facturación se desvía del presupuesto más de un ± x %.
            ⚠ SIG aún no ha fijado el umbral x% (se usa ±5 % de ejemplo). El presupuesto procede de Config. Presupuesto
            (entrada manual): sin presupuesto cargado no hay desviación calculable.</span>
        </div>

        <table mat-table [dataSource]="tipoA()" class="sig-mat-table">
          <ng-container matColumnDef="accion">
            <th mat-header-cell *matHeaderCellDef> Acción / Proyecto </th>
            <td mat-cell *matCellDef="let f">
              <span class="cell-strong">{{ f.accion }}</span>
              <span class="cell-ceco">{{ f.ceco }}</span>
            </td>
          </ng-container>
          <ng-container matColumnDef="presupuesto">
            <th mat-header-cell *matHeaderCellDef class="num"> Presupuesto </th>
            <td mat-cell *matCellDef="let f" class="num">
              {{ f.presupuesto === null ? '— ⚠' : (f.presupuesto | currency:'EUR':'symbol':'1.2-2':'es') }}
            </td>
          </ng-container>
          <ng-container matColumnDef="facturado">
            <th mat-header-cell *matHeaderCellDef class="num"> Facturado </th>
            <td mat-cell *matCellDef="let f" class="num">{{ f.facturado | currency:'EUR':'symbol':'1.2-2':'es' }}</td>
          </ng-container>
          <ng-container matColumnDef="desviacion">
            <th mat-header-cell *matHeaderCellDef class="num"> Desviación </th>
            <td mat-cell *matCellDef="let f" class="num">
              {{ f.desviacion === null ? '—' : signed(f.desviacion) }}
            </td>
          </ng-container>
          <ng-container matColumnDef="desvPct">
            <th mat-header-cell *matHeaderCellDef class="num"> Desv. % </th>
            <td mat-cell *matCellDef="let f" class="num">{{ f.desvPct === null ? '—' : signedPct(f.desvPct) }}</td>
          </ng-container>
          <ng-container matColumnDef="estado">
            <th mat-header-cell *matHeaderCellDef> Estado </th>
            <td mat-cell *matCellDef="let f">
              <span class="chip-estado" [class]="chipClassA(f.estado)">{{ f.estado }}</span>
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="colsA"></tr>
          <tr mat-row *matRowDef="let row; columns: colsA;" [class.row-alerta]="row.estado === 'Alerta'"></tr>
        </table>

        <div class="sig-total-row">
          <span class="total-label">Total periodo</span>
          <span class="total-cell num">{{ 28500 | currency:'EUR':'symbol':'1.2-2':'es' }}</span>
          <span class="total-cell num">{{ 35060 | currency:'EUR':'symbol':'1.2-2':'es' }}</span>
          <span class="total-cell num pos">+6.560,00 €</span>
          <span class="total-cell num pos">+23,0 %</span>
          <span class="total-cell"><span class="chip-estado chip-alerta">2 alertas</span></span>
        </div>

        <div class="sig-note sig-note--warn">
          <mat-icon>warning</mat-icon>
          <span>Clientes/acciones/CECOs reales (Acciones · Auditoría). DAIKIN factura 3.250,00 €
            (borrador de Facturación, margen 26,5 %). ⚠ Los presupuestos son ilustrativos (dependen de Config.
            Presupuesto, entrada manual) y el umbral ±x % está pendiente de definir por SIG.</span>
        </div>
      </section>

      <!-- ===== Tipo B ===== -->
      <section class="sig-block">
        <div class="sig-block-head">
          <div>
            <h2 class="sig-block-title">Tipo B · Facturado vs costes reales — Mayo 2026</h2>
            <p class="sig-block-sub">Alerta cuando lo facturado &lt; suma de costes (operativos + logística + …)</p>
          </div>
        </div>

        <div class="sig-note sig-note--neutral">
          <mat-icon>info</mat-icon>
          <span>La alerta salta no solo comparando presupuesto vs facturado, sino también lo facturado con los costes:
            si lo facturado es menor que todos los costes (operativos, logística…), se marca alerta. Al pinchar en la
            alerta se ve en qué partida está el exceso de coste. ⚠ Importes ilustrativos; los costes de
            logística/externos proceden de Contabilidad / Config. Factura (pendiente de cierre con SIG).</span>
        </div>

        <table mat-table [dataSource]="tipoB()" class="sig-mat-table">
          <ng-container matColumnDef="accion">
            <th mat-header-cell *matHeaderCellDef> Acción / Proyecto </th>
            <td mat-cell *matCellDef="let f">
              <span class="cell-strong">{{ f.accion }}</span>
              <span class="cell-ceco">{{ f.ceco }}</span>
            </td>
          </ng-container>
          <ng-container matColumnDef="facturado">
            <th mat-header-cell *matHeaderCellDef class="num"> Facturado </th>
            <td mat-cell *matCellDef="let f" class="num">{{ f.facturado | currency:'EUR':'symbol':'1.2-2':'es' }}</td>
          </ng-container>
          <ng-container matColumnDef="costes">
            <th mat-header-cell *matHeaderCellDef class="num"> Costes totales </th>
            <td mat-cell *matCellDef="let f" class="num">{{ f.costes | currency:'EUR':'symbol':'1.2-2':'es' }}</td>
          </ng-container>
          <ng-container matColumnDef="resultado">
            <th mat-header-cell *matHeaderCellDef class="num"> Resultado </th>
            <td mat-cell *matCellDef="let f" class="num" [class.pos]="f.resultado >= 0" [class.neg]="f.resultado < 0">
              {{ signed(f.resultado) }}
            </td>
          </ng-container>
          <ng-container matColumnDef="margenPct">
            <th mat-header-cell *matHeaderCellDef class="num"> Margen % </th>
            <td mat-cell *matCellDef="let f" class="num">{{ signedPct(f.margenPct) }}</td>
          </ng-container>
          <ng-container matColumnDef="estado">
            <th mat-header-cell *matHeaderCellDef> Estado </th>
            <td mat-cell *matCellDef="let f">
              @if (f.estado === 'OK') {
                <span class="chip-estado chip-ok">OK</span>
              } @else {
                <button mat-button class="ver-exceso" (click)="toggleExceso()" data-testid="ef-ver-exceso">
                  Ver exceso ›
                </button>
              }
            </td>
          </ng-container>
          <tr mat-header-row *matHeaderRowDef="colsB"></tr>
          <tr mat-row *matRowDef="let row; columns: colsB;" [class.row-alerta]="row.estado === 'Alerta'"></tr>
        </table>

        <!-- Drill-down -->
        @if (excesoAbierto()) {
          <div class="drilldown" data-testid="ef-drilldown">
            <div class="drilldown-head">
              <mat-icon>error_outline</mat-icon>
              <span>Exceso de coste — Granini GPVs · facturado 13.450 € &lt; costes 14.980 €</span>
            </div>
            <table mat-table [dataSource]="exceso()" class="sig-mat-table drilldown-table">
              <ng-container matColumnDef="partida">
                <th mat-header-cell *matHeaderCellDef> Partida de coste </th>
                <td mat-cell *matCellDef="let p">{{ p.partida }}</td>
              </ng-container>
              <ng-container matColumnDef="origen">
                <th mat-header-cell *matHeaderCellDef> Origen </th>
                <td mat-cell *matCellDef="let p">{{ p.origen }}</td>
              </ng-container>
              <ng-container matColumnDef="importe">
                <th mat-header-cell *matHeaderCellDef class="num"> Importe </th>
                <td mat-cell *matCellDef="let p" class="num">{{ p.importe | currency:'EUR':'symbol':'1.2-2':'es' }}</td>
              </ng-container>
              <ng-container matColumnDef="pct">
                <th mat-header-cell *matHeaderCellDef class="num"> % s/ facturado </th>
                <td mat-cell *matCellDef="let p" class="num">{{ p.pctSobreFacturado | number:'1.1-1':'es' }} %</td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="colsExceso"></tr>
              <tr mat-row *matRowDef="let row; columns: colsExceso;"></tr>
            </table>
            <div class="sig-total-row drilldown-total">
              <span class="total-label">Total costes</span>
              <span class="total-cell"></span>
              <span class="total-cell num neg">{{ 14980 | currency:'EUR':'symbol':'1.2-2':'es' }}</span>
              <span class="total-cell num">111,4 %</span>
            </div>
            <p class="drilldown-text">
              La partida que dispara el exceso es <strong>Logística</strong> (refacturado por encima de lo facturado al
              cliente). Aquí se diferenciará lo pagado a proveedores de lo refacturado (pendiente con SIG).
            </p>
          </div>
        }

        <div class="sig-total-row">
          <span class="total-label">Total periodo</span>
          <span class="total-cell num">{{ 35060 | currency:'EUR':'symbol':'1.2-2':'es' }}</span>
          <span class="total-cell num">{{ 33190 | currency:'EUR':'symbol':'1.2-2':'es' }}</span>
          <span class="total-cell num pos">+1.870,00 €</span>
          <span class="total-cell num pos">+5,3 %</span>
          <span class="total-cell"><span class="chip-estado chip-alerta">1 alerta</span></span>
        </div>

        <div class="sig-note sig-note--warn">
          <mat-icon>warning</mat-icon>
          <span>Regla añadida por SIG (Martha, 19/06): la alerta debe comparar lo facturado tanto con el presupuesto
            (Tipo A) como con los costes reales (Tipo B). El drill-down muestra la partida con el exceso. ⚠ Los costes
            de logística y servicios externos dependen del cierre de Contabilidad / Config. Factura con SIG; importes
            ilustrativos.</span>
        </div>
      </section>

      <!-- Barra de acciones inferior -->
      <div class="sig-action-bar">
        <button mat-flat-button color="primary" data-testid="ef-exportar"><mat-icon>download</mat-icon> Exportar desviaciones</button>
        <button mat-stroked-button routerLink="/config-presupuesto" data-testid="ef-ir-presupuesto"><mat-icon>tune</mat-icon> Ir a Config. Presupuesto</button>
        <button mat-stroked-button data-testid="ef-marcar-revisadas"><mat-icon>done_all</mat-icon> Marcar alertas como revisadas</button>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .sig-exec-page { padding: 28px 28px 40px; background: var(--sig-bg-app); min-height: 100vh; }
    .sig-exec-header { margin-bottom: 20px; }
    .sig-exec-title { font-size: 24px; font-weight: 700; color: var(--sig-text-heading); margin: 0 0 4px; display: flex; align-items: center; flex-wrap: wrap; gap: 10px; }
    .sig-exec-sub { font-size: 13px; color: var(--sig-text-muted); margin: 0; }

    .env-badge { font-size: 11px; font-weight: 700; padding: 3px 10px; border-radius: 12px; text-transform: uppercase; letter-spacing: .04em; }
    .env-badge--demo { background: rgba(245,158,11,.12); color: #f59e0b; border: 1px solid rgba(245,158,11,.25); }
    .env-badge--mes { background: var(--sig-bg-hover); color: var(--sig-text-muted); border: 1px solid var(--sig-border); }

    .filtros { display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 16px; align-items: flex-start; }
    .filtros mat-form-field { width: 180px; }
    .filtros-acciones { display: flex; gap: 8px; align-self: center; }

    .sig-banner { display: flex; gap: 10px; align-items: flex-start; border-radius: 10px; padding: 12px 16px; margin-bottom: 20px; font-size: 13px; line-height: 1.5; }
    .sig-banner mat-icon { flex: none; }
    .sig-banner--info { background: rgba(59,130,246,.08); border: 1px solid rgba(59,130,246,.25); color: var(--sig-text-primary); }
    .sig-banner--info mat-icon { color: #3b82f6; }

    .sig-summary-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 28px; }
    @media (max-width: 900px) { .sig-summary-grid { grid-template-columns: repeat(2, 1fr); } }
    .sig-summary-card { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; padding: 20px; text-align: center; }
    .sig-summary-value { font-size: 28px; font-weight: 700; color: var(--sig-text-heading); font-family: 'Roboto Mono', monospace; }
    .sig-summary-label { font-size: 12px; color: var(--sig-text-muted); text-transform: uppercase; letter-spacing: .05em; margin-top: 4px; }
    .sig-summary-card--bloqueante .sig-summary-value { color: #ef4444; }
    .sig-summary-card--advertencia .sig-summary-value { color: #f59e0b; }

    .sig-block { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; padding: 20px; margin-bottom: 24px; }
    .sig-block-head { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; margin-bottom: 14px; }
    .sig-block-title { font-size: 17px; font-weight: 700; color: var(--sig-text-heading); margin: 0 0 2px; }
    .sig-block-sub { font-size: 13px; color: var(--sig-text-muted); margin: 0; }

    .sig-note { display: flex; gap: 10px; align-items: flex-start; border-radius: 8px; padding: 10px 14px; margin: 12px 0; font-size: 12.5px; line-height: 1.5; }
    .sig-note mat-icon { flex: none; font-size: 20px; width: 20px; height: 20px; }
    .sig-note--warn { background: rgba(245,158,11,.08); border: 1px solid rgba(245,158,11,.22); color: var(--sig-text-primary); }
    .sig-note--warn mat-icon { color: #f59e0b; }
    .sig-note--neutral { background: var(--sig-bg-hover); border: 1px solid var(--sig-border); color: var(--sig-text-primary); }
    .sig-note--neutral mat-icon { color: var(--sig-text-muted); }

    .sig-mat-table {
      width: 100%; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 10px; overflow: hidden; border-collapse: collapse; margin-top: 8px;
      th.mat-mdc-header-cell { color: var(--sig-text-muted); font-size: 11px; font-weight: 700; letter-spacing: .05em; text-transform: uppercase; border-bottom-color: var(--sig-border); padding: 10px 12px; }
      td.mat-mdc-cell { color: var(--sig-text-primary); font-size: 13px; border-bottom-color: var(--sig-border); padding: 10px 12px; }
      .num { text-align: right; font-family: 'Roboto Mono', monospace; }
      th.num { text-align: right; }
    }
    .cell-strong { font-weight: 600; display: block; }
    .cell-ceco { font-size: 11px; color: var(--sig-text-muted); display: block; }
    .row-alerta td.mat-mdc-cell { background: rgba(239,68,68,.05); }
    .pos { color: #22c55e; }
    .neg { color: #ef4444; }

    .chip-estado { display: inline-block; padding: 2px 10px; border-radius: 12px; font-size: 11px; font-weight: 700; }
    .chip-alerta { background: rgba(239,68,68,.12); color: #ef4444; border: 1px solid rgba(239,68,68,.22); }
    .chip-ok { background: rgba(34,197,94,.12); color: #22c55e; border: 1px solid rgba(34,197,94,.22); }
    .chip-gris { background: var(--sig-bg-hover); color: var(--sig-text-muted); border: 1px solid var(--sig-border); }
    .ver-exceso { color: #ef4444; font-weight: 600; font-size: 12.5px; padding: 0 6px; min-width: 0; }

    .sig-total-row {
      display: grid; grid-template-columns: 2fr 1fr 1fr 1fr 1fr 1fr; gap: 8px; align-items: center;
      background: var(--sig-bg-hover); border: 1px solid var(--sig-border); border-radius: 8px; padding: 10px 12px; margin-top: 10px; font-size: 13px;
    }
    .total-label { font-weight: 700; color: var(--sig-text-heading); }
    .total-cell.num { text-align: right; font-family: 'Roboto Mono', monospace; }

    .drilldown { background: var(--sig-bg-hover); border: 1px solid var(--sig-border); border-left: 3px solid #ef4444; border-radius: 8px; padding: 16px; margin: 12px 0; }
    .drilldown-head { display: flex; gap: 8px; align-items: center; font-weight: 700; color: var(--sig-text-heading); font-size: 14px; margin-bottom: 10px; }
    .drilldown-head mat-icon { color: #ef4444; }
    .drilldown-table { margin-top: 4px; }
    .drilldown-total { grid-template-columns: 2fr 1fr 1fr 1fr; }
    .drilldown-text { font-size: 12.5px; color: var(--sig-text-primary); line-height: 1.5; margin: 12px 0 0; }

    .sig-action-bar { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 8px; }
  `],
})
export class ErroresFacturacionComponent {
  // ---- Filtros (maqueta) ----
  protected readonly fDepartamento = signal<string[]>([]);
  protected readonly fCliente = signal<string[]>([]);
  protected readonly fAccion = signal<string[]>([]);
  protected readonly fRango = signal('01/05/2026 – 31/05/2026');
  protected readonly fAnioMes = signal('2026-05');
  protected readonly fTipoAlerta = signal('');
  protected readonly fSeveridad = signal('');
  protected readonly fEstado = signal('sin-revisar');

  // ---- Columnas ----
  protected readonly colsA = ['accion', 'presupuesto', 'facturado', 'desviacion', 'desvPct', 'estado'];
  protected readonly colsB = ['accion', 'facturado', 'costes', 'resultado', 'margenPct', 'estado'];
  protected readonly colsExceso = ['partida', 'origen', 'importe', 'pct'];

  // ---- Drill-down ----
  protected readonly excesoAbierto = signal(false);

  // ---- Datos ilustrativos: Tipo A ----
  protected readonly tipoA = signal<FilaTipoA[]>([
    { accion: 'DAIKIN', ceco: 'CECO 023301', presupuesto: 3500, facturado: 3250, desviacion: -250, desvPct: -7.1, estado: 'Alerta' },
    { accion: 'Amex Shop Small', ceco: 'CECO 035501', presupuesto: 9000, facturado: 9180, desviacion: 180, desvPct: 2.0, estado: 'OK' },
    { accion: 'Granini GPVs', ceco: 'CECO 025888', presupuesto: 12000, facturado: 13450, desviacion: 1450, desvPct: 12.1, estado: 'Alerta' },
    { accion: 'Apple Formaciones', ceco: 'Apple RST', presupuesto: 4000, facturado: 3980, desviacion: -20, desvPct: -0.5, estado: 'OK' },
    { accion: 'ITC GPVs', ceco: 'Independent Tobacco', presupuesto: null, facturado: 5200, desviacion: null, desvPct: null, estado: 'Sin presup.' },
  ]);

  // ---- Datos ilustrativos: Tipo B ----
  protected readonly tipoB = signal<FilaTipoB[]>([
    { accion: 'DAIKIN', ceco: 'CECO 023301', facturado: 3250, costes: 2389, resultado: 861, margenPct: 26.5, estado: 'OK' },
    { accion: 'Granini GPVs', ceco: 'CECO 025888', facturado: 13450, costes: 14980, resultado: -1530, margenPct: -11.4, estado: 'Alerta' },
  ]);

  // ---- Drill-down ilustrativo ----
  protected readonly exceso = signal<PartidaExceso[]>([
    { partida: 'Costes operativos (nómina + dietas + km)', origen: 'A3 Innuva · Payhawk', importe: 9420, pctSobreFacturado: 70.0 },
    { partida: 'Logística (stock + refacturado)', origen: 'Galán / MediaPost (SharePoint)', importe: 4310, pctSobreFacturado: 32.0 },
    { partida: 'Servicios externos / proveedores', origen: 'Contabilidad', importe: 1250, pctSobreFacturado: 9.3 },
  ]);

  protected toggleExceso(): void {
    this.excesoAbierto.update(v => !v);
  }

  protected limpiar(): void {
    this.fDepartamento.set([]);
    this.fCliente.set([]);
    this.fAccion.set([]);
    this.fTipoAlerta.set('');
    this.fSeveridad.set('');
    this.fEstado.set('sin-revisar');
  }

  protected chipClassA(estado: EstadoTipoA): string {
    if (estado === 'Alerta') return 'chip-alerta';
    if (estado === 'OK') return 'chip-ok';
    return 'chip-gris';
  }

  protected signed(value: number): string {
    const abs = Math.abs(value).toLocaleString('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    return `${value >= 0 ? '+' : '−'} ${abs} €`;
  }

  protected signedPct(value: number): string {
    const abs = Math.abs(value).toLocaleString('es-ES', { minimumFractionDigits: 1, maximumFractionDigits: 1 });
    return `${value >= 0 ? '+' : '−'}${abs} %`;
  }
}
