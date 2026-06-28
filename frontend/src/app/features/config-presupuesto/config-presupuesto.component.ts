import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatTableModule } from '@angular/material/table';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { ForecastResumenComponent } from '../forecast/forecast-resumen.component';
import { ConfigPresupuestoService } from '../../core/api/config-presupuesto.service';
import { ServiceService } from '../../core/api/services.service';
import { ClientService } from '../../core/api/clients.service';
import { ConceptService } from '../../core/api/concepts.service';
import { AuthService } from '../../core/auth/auth.service';
import { NotifyService } from '../../core/notify.service';
import { ClientListItemDto, ConfigPresupuestoDto, ConceptListItemDto, PartidaPresupuestoDto, ServiceListItemDto } from '../../models/dtos';
import { TipoPartidaPresupuesto } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';

interface EditorState {
  id?: number; nombre: string; tipo: TipoPartidaPresupuesto; anio: number | null;
  presupuesto: number; consumido: number; descripcion: string;
}
interface EditorConceptoState { id?: number; nombre: string; fechaDesde: string; fechaHasta?: string | null; }

// Configuración de Presupuesto (prototipo 24/28, PPT slide 35): partidas de presupuesto por acción/servicio.
// ENTRADA MANUAL (el prototipo lo dice). Barra de filtros + KPIs + tabla con barras de avance + márgenes.
@Component({
  selector: 'app-config-presupuesto',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatTooltipModule, MatButtonToggleModule, MatTableModule,
    MatDatepickerModule, MatNativeDateModule,
    BreadcrumbsComponent, SkeletonComponent, ForecastResumenComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Configuración de Presupuesto' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title"><mat-icon class="title-icon">savings</mat-icon> Configuración de Presupuesto</h1>
        @if (vista() === 'presupuesto' && canManage() && serviceId()) {
          <button mat-flat-button color="primary" (click)="nueva()" data-testid="btn-nueva-partida">
            <mat-icon>add</mat-icon> Añadir partida
          </button>
        }
        @if (vista() === 'conceptos-pago' && canManage()) {
          <button mat-flat-button color="primary" (click)="nuevoConcepto()" data-testid="btn-nuevo-concepto-pago">
            <mat-icon>add</mat-icon> Nuevo concepto
          </button>
        }
      </div>

      <!-- Pestañas (penpot): Presupuesto confirmado | Forecast ventas / GPP | Conceptos de pago -->
      <mat-button-toggle-group class="vista-toggle" [value]="vista()" (change)="onCambioVista($event.value)" data-testid="vista-toggle">
        <mat-button-toggle value="presupuesto" data-testid="tab-presupuesto">Presupuesto confirmado</mat-button-toggle>
        <mat-button-toggle value="forecast" data-testid="tab-forecast">Forecast ventas / GPP</mat-button-toggle>
        <mat-button-toggle value="conceptos-pago" data-testid="tab-conceptos-pago">Conceptos de pago</mat-button-toggle>
      </mat-button-toggle-group>

      @if (vista() === 'forecast') {
        <app-forecast-resumen [embedded]="true" />
      } @else if (vista() === 'conceptos-pago') {

        <!-- TAB: Conceptos de pago -->
        @if (loadingConceptos()) {
          <sig-skeleton [count]="3" />
        } @else if (conceptosPago().length === 0) {
          <mat-card><mat-card-content><p class="empty">No hay conceptos de pago. Crea el primero.</p></mat-card-content></mat-card>
        } @else {
          <mat-card>
            <mat-card-content>
              <div class="section-head"><span class="dot"></span><h2>Conceptos de pago</h2></div>
              <table class="sig-table" [dataSource]="conceptosPago()" mat-table>
                <ng-container matColumnDef="nombre">
                  <th mat-header-cell *matHeaderCellDef>Nombre</th>
                  <td mat-cell *matCellDef="let c" class="cell-title">{{ c.nombre }}</td>
                </ng-container>
                <ng-container matColumnDef="desde">
                  <th mat-header-cell *matHeaderCellDef>Desde</th>
                  <td mat-cell *matCellDef="let c" class="mono-num">{{ c.fechaDesde | date:'dd/MM/yyyy' }}</td>
                </ng-container>
                <ng-container matColumnDef="hasta">
                  <th mat-header-cell *matHeaderCellDef>Hasta</th>
                  <td mat-cell *matCellDef="let c" class="mono-num">{{ c.fechaHasta ? (c.fechaHasta | date:'dd/MM/yyyy') : '—' }}</td>
                </ng-container>
                <ng-container matColumnDef="acciones">
                  <th mat-header-cell *matHeaderCellDef style="text-align: right;">Acciones</th>
                  <td mat-cell *matCellDef="let c">
                    <div class="sig-table-actions" style="justify-content: flex-end;">
                      <a mat-icon-button [routerLink]="['/concepts', c.id, 'formula']" matTooltip="Editar fórmula" data-testid="btn-formula-pago"><mat-icon>functions</mat-icon></a>
                      @if (canManage()) {
                        <button mat-icon-button (click)="editarConcepto(c)" matTooltip="Editar" data-testid="btn-editar-pago"><mat-icon>edit</mat-icon></button>
                        <button mat-icon-button (click)="eliminarConcepto(c)" matTooltip="Eliminar" data-testid="btn-eliminar-pago"><mat-icon>delete_outline</mat-icon></button>
                      }
                    </div>
                  </td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="['nombre', 'desde', 'hasta', 'acciones']"></tr>
                <tr mat-row *matRowDef="let row; columns: ['nombre', 'desde', 'hasta', 'acciones']; trackBy: (_, i) => i"></tr>
              </table>
            </mat-card-content>
          </mat-card>
        }

        <!-- Editor de concepto de pago -->
        @if (editorConcepto(); as ed) {
          <mat-card class="editor-card">
            <mat-card-content>
              <div class="section-head"><span class="dot dot-blue"></span><h2>{{ ed.id ? 'Editar concepto' : 'Nuevo concepto' }}</h2></div>
              <div class="sig-form-row">
                <mat-form-field appearance="outline" class="full">
                  <mat-label>Nombre del concepto</mat-label>
                  <input matInput [(ngModel)]="ed.nombre" placeholder="Ej. Incentivo por visita" data-testid="input-nombre-concepto-pago" />
                </mat-form-field>
              </div>
              <div class="sig-form-row">
                <mat-form-field appearance="outline" class="full">
                  <mat-label>Desde *</mat-label>
                  <input matInput [matDatepicker]="dpDesde" [(ngModel)]="ed.fechaDesde" data-testid="input-desde-pago" />
                  <mat-datepicker-toggle matIconSuffix [for]="dpDesde" /><mat-datepicker #dpDesde />
                </mat-form-field>
                <mat-form-field appearance="outline" class="full">
                  <mat-label>Hasta</mat-label>
                  <input matInput [matDatepicker]="dpHasta" [(ngModel)]="ed.fechaHasta" data-testid="input-hasta-pago" />
                  <mat-datepicker-toggle matIconSuffix [for]="dpHasta" /><mat-datepicker #dpHasta />
                </mat-form-field>
              </div>
              <p style="color: var(--sig-text-muted); font-size: 12px; margin: 8px 0;">
                <mat-icon style="vertical-align: middle; font-size: 16px; width: 16px; height: 16px; margin-right: 4px;">info</mat-icon>
                La fórmula se edita desde el botón <strong>Editar fórmula</strong>.
              </p>
              <div class="editor-actions">
                <button mat-flat-button color="primary" [disabled]="!ed.nombre.trim() || !ed.fechaDesde || savingConcepto()" (click)="guardarConcepto()" data-testid="btn-guardar-concepto-pago">
                  <mat-icon>save</mat-icon> Guardar concepto
                </button>
                <button mat-stroked-button (click)="cancelarConcepto()" data-testid="btn-cancelar-concepto-pago">Cancelar</button>
              </div>
            </mat-card-content>
          </mat-card>
        }

      } @else {

      <!-- Barra de filtros (prototipo) -->
      <div class="sig-filter-bar">
        <mat-form-field appearance="outline" class="ff">
          <mat-label>Cliente</mat-label>
          <mat-select [(value)]="clienteFilter" (selectionChange)="onCliente()" data-testid="filtro-cliente">
            <mat-option [value]="null">Todos</mat-option>
            @for (c of clientes(); track c.id) { <mat-option [value]="c.id">{{ c.nombre }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" class="ff ff-wide">
          <mat-label>Acción / Servicio</mat-label>
          <mat-select [(value)]="serviceIdValue" (selectionChange)="onService($event.value)" data-testid="filtro-servicio">
            @for (s of serviciosFiltrados(); track s.id) { <mat-option [value]="s.id">{{ s.nombre }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" class="ff ff-narrow">
          <mat-label>Año</mat-label>
          <mat-select [(value)]="anioFilter" data-testid="filtro-anio">
            @for (a of anios; track a) { <mat-option [value]="a">{{ a }}</mat-option> }
          </mat-select>
        </mat-form-field>
      </div>

      <!-- KPIs (prototipo) -->
      @if (config(); as cfg) {
        <div class="kpi-row">
          <div class="sig-kpi-card">
            <div class="sig-kpi-card__label">Presupuesto acción</div>
            <div class="sig-kpi-card__value">{{ cfg.totalPresupuesto | number:'1.0-0' }} €</div>
          </div>
          <div class="sig-kpi-card">
            <div class="sig-kpi-card__label">Consumido</div>
            <div class="sig-kpi-card__value">{{ cfg.totalConsumido | number:'1.0-0' }} €</div>
          </div>
          <div class="sig-kpi-card">
            <div class="sig-kpi-card__label">Restante</div>
            <div class="sig-kpi-card__value sig-pos">{{ cfg.totalRestante | number:'1.0-0' }} €</div>
          </div>
          <div class="sig-kpi-card">
            <div class="sig-kpi-card__label">Margen operativo objetivo</div>
            <div class="sig-kpi-card__value sig-warn">{{ cfg.margenObjetivoPct != null ? (cfg.margenObjetivoPct | number:'1.0-0') + ' %' : '—' }}</div>
          </div>
        </div>
      }

      @if (loading()) {
        <sig-skeleton [count]="5" />
      } @else if (!serviceId()) {
        <mat-card><mat-card-content><p class="empty">Selecciona una acción para configurar su presupuesto.</p></mat-card-content></mat-card>
      } @else if (config(); as cfg) {
        <div class="layout">
          <div class="col-main">
            <mat-card>
              <mat-card-content>
                <div class="section-head">
                  <span class="dot"></span>
                  <h2>Partidas de presupuesto — {{ cfg.serviceNombre }}</h2>
                </div>
                <p class="manual-note">
                  <mat-icon>info</mat-icon>
                  <span><strong>Entrada manual.</strong> El presupuesto por partida no procede de ningún origen de datos: se carga a mano. Cada partida es <strong>Anual</strong> o <strong>Total acción</strong>. Importes ilustrativos hasta validar con SIG.</span>
                </p>
                @if (cfg.partidas.length === 0) {
                  <p class="empty">Esta acción aún no tiene partidas de presupuesto.</p>
                } @else {
                  <table class="sig-table par-table">
                    <thead>
                      <tr><th>Partida</th><th>Tipo</th><th class="num">Presupuesto</th><th class="num">Consumido</th><th class="num">Restante</th><th class="avance-col">Avance</th><th></th></tr>
                    </thead>
                    <tbody>
                      @for (p of cfg.partidas; track p.id) {
                        <tr [class.row-active]="editor()?.id === p.id">
                          <td><div class="cell-title">{{ p.nombre }}</div><div class="cell-sub">{{ p.descripcion }}</div></td>
                          <td><span class="sig-type-pill">{{ tipoLabel(p.tipo) }}</span></td>
                          <td class="num mono">{{ p.presupuesto | number:'1.0-0' }} €</td>
                          <td class="num mono">{{ p.consumido > 0 ? (p.consumido | number:'1.0-0') + ' €' : '—' }}</td>
                          <td class="num mono">{{ p.restante | number:'1.0-0' }} €</td>
                          <td class="avance-col">
                            <div class="avance">
                              <div class="bar"><div class="fill" [class]="avanceClass(p.avancePct)" [style.width.%]="clamp(p.avancePct)"></div></div>
                              <span class="pct">{{ p.avancePct | number:'1.0-0' }}%</span>
                            </div>
                          </td>
                          <td class="actions">
                            @if (canManage()) {
                              <button mat-button color="primary" (click)="editar(p)" data-testid="btn-editar">Editar</button>
                              <button mat-icon-button (click)="eliminar(p)" matTooltip="Eliminar" data-testid="btn-eliminar"><mat-icon>delete_outline</mat-icon></button>
                            }
                          </td>
                        </tr>
                      }
                      <tr class="row-total">
                        <td colspan="2"><strong>Total presupuesto acción</strong></td>
                        <td class="num mono"><strong>{{ cfg.totalPresupuesto | number:'1.0-0' }} €</strong></td>
                        <td class="num mono"><strong>{{ cfg.totalConsumido | number:'1.0-0' }} €</strong></td>
                        <td class="num mono"><strong>{{ cfg.totalRestante | number:'1.0-0' }} €</strong></td>
                        <td class="avance-col">
                          <div class="avance">
                            <div class="bar"><div class="fill" [class]="avanceClass(cfg.avancePct)" [style.width.%]="clamp(cfg.avancePct)"></div></div>
                            <span class="pct">{{ cfg.avancePct | number:'1.0-0' }}%</span>
                          </div>
                        </td>
                        <td></td>
                      </tr>
                    </tbody>
                  </table>
                }
              </mat-card-content>
            </mat-card>

            <!-- Editor de partida -->
            @if (editor(); as ed) {
              <mat-card class="editor-card">
                <mat-card-content>
                  <div class="section-head"><span class="dot dot-blue"></span><h2>{{ ed.id ? 'Editar partida' : 'Nueva partida' }}</h2></div>
                  <div class="form-grid">
                    <mat-form-field appearance="outline" class="full">
                      <mat-label>Nombre de la partida</mat-label>
                      <input matInput [(ngModel)]="ed.nombre" placeholder="Ej. Personal de campo" data-testid="input-nombre" />
                    </mat-form-field>
                    <mat-form-field appearance="outline">
                      <mat-label>Tipo</mat-label>
                      <mat-select [(value)]="ed.tipo" data-testid="input-tipo">
                        <mat-option value="Anual">Anual</mat-option>
                        <mat-option value="TotalAccion">Total acción</mat-option>
                      </mat-select>
                    </mat-form-field>
                    @if (ed.tipo === 'Anual') {
                      <mat-form-field appearance="outline">
                        <mat-label>Ejercicio</mat-label>
                        <input matInput type="number" [(ngModel)]="ed.anio" placeholder="2026" />
                      </mat-form-field>
                    }
                    <mat-form-field appearance="outline">
                      <mat-label>Presupuesto (€)</mat-label>
                      <input matInput type="number" [(ngModel)]="ed.presupuesto" data-testid="input-presupuesto" />
                    </mat-form-field>
                    <mat-form-field appearance="outline">
                      <mat-label>Consumido (€)</mat-label>
                      <input matInput type="number" [(ngModel)]="ed.consumido" />
                    </mat-form-field>
                    <mat-form-field appearance="outline" class="full">
                      <mat-label>Descripción</mat-label>
                      <input matInput [(ngModel)]="ed.descripcion" placeholder="Ej. Salario bruto + incentivos" />
                    </mat-form-field>
                  </div>
                  <div class="editor-actions">
                    <button mat-flat-button color="primary" [disabled]="!ed.nombre.trim() || saving()" (click)="guardar()" data-testid="btn-guardar">
                      <mat-icon>save</mat-icon> Guardar presupuesto
                    </button>
                    <button mat-stroked-button (click)="cancelar()" data-testid="btn-cancelar">Cancelar</button>
                  </div>
                </mat-card-content>
              </mat-card>
            }
          </div>

          <!-- Panel lateral: márgenes objetivo + vigencia -->
          <div class="col-side">
            <mat-card>
              <mat-card-content>
                <div class="section-head"><span class="dot dot-blue"></span><h2>Márgenes objetivo</h2></div>
                <mat-form-field appearance="outline" class="full">
                  <mat-label>Margen operativo objetivo (%)</mat-label>
                  <input matInput type="number" [(ngModel)]="margenObjetivo" [disabled]="!canManage()" data-testid="input-margen" />
                </mat-form-field>
                @if (canManage()) {
                  <button mat-stroked-button color="primary" class="full-btn" (click)="guardarMargen()" data-testid="btn-guardar-margen">Guardar objetivo</button>
                }
                <div class="kv"><span>Margen operativo real</span><strong>{{ cfg.margenRealPct != null ? (cfg.margenRealPct | number:'1.0-1') + ' %' : '—' }}</strong></div>
                <div class="kv"><span>Desviación vs objetivo</span>
                  <strong [class.neg]="(cfg.desviacionPp ?? 0) < 0" [class.pos]="(cfg.desviacionPp ?? 0) > 0">{{ cfg.desviacionPp != null ? (cfg.desviacionPp | number:'1.0-1') + ' pp' : '—' }}</strong>
                </div>
              </mat-card-content>
            </mat-card>
            <mat-card>
              <mat-card-content>
                <div class="section-head"><span class="dot dot-green"></span><h2>Vigencia y ámbito</h2></div>
                <div class="kv"><span>Acción</span><strong>{{ cfg.serviceNombre }}</strong></div>
                <div class="kv"><span>Cliente</span><strong>{{ cfg.clientNombre }}</strong></div>
                <div class="kv"><span>Partidas anuales</span><strong>{{ cfg.partidasAnuales }}</strong></div>
                <div class="kv"><span>Partidas total acción</span><strong>{{ cfg.partidasTotalAccion }}</strong></div>
              </mat-card-content>
            </mat-card>
          </div>
        </div>
      }
      }
    </div>
  `,
  styles: [`
    .title-icon { vertical-align: middle; margin-right: 6px; }
    .vista-toggle { margin-bottom: 18px; }
    .sig-filter-bar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; margin-bottom: 16px; }
    .ff { width: 200px; } .ff-wide { width: 300px; } .ff-narrow { width: 130px; }
    .kpi-row { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 20px; }
    @media (max-width: 900px) { .kpi-row { grid-template-columns: repeat(2, 1fr); } }
    .sig-kpi-card { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; padding: 18px 20px; }
    .sig-kpi-card__label { font-size: 11px; font-weight: 600; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); margin-bottom: 10px; }
    .sig-kpi-card__value { font-size: 28px; font-weight: 700; color: var(--sig-text-heading); font-family: 'Roboto Mono', monospace; line-height: 1; }
    .sig-pos { color: var(--sig-success); } .sig-warn { color: var(--sig-warning); }
    .layout { display: grid; grid-template-columns: 1fr 300px; gap: 16px; align-items: start; }
    @media (max-width: 1100px) { .layout { grid-template-columns: 1fr; } }
    .col-main, .col-side { display: flex; flex-direction: column; gap: 16px; }
    .section-head { display: flex; align-items: center; gap: 8px; margin-bottom: 12px; }
    .section-head h2 { font-size: 14px; font-weight: 600; margin: 0; color: var(--sig-text-heading); }
    .dot { width: 8px; height: 8px; border-radius: 50%; background: var(--sig-teal); }
    .dot-blue { background: var(--sig-blue-light); } .dot-green { background: var(--sig-success); }
    .manual-note { display: flex; gap: 8px; align-items: flex-start; background: var(--sig-warning-bg); border: 1px solid var(--sig-warning); border-radius: 8px; padding: 10px 12px; font-size: 12.5px; color: var(--sig-text-secondary); margin-bottom: 14px; }
    .manual-note mat-icon { color: var(--sig-warning); font-size: 18px; width: 18px; height: 18px; flex-shrink: 0; }
    .par-table { width: 100%; }
    .par-table th { font-size: 11px; text-transform: uppercase; letter-spacing: .05em; color: var(--sig-text-muted); text-align: left; padding: 8px; }
    .par-table td { padding: 10px 8px; vertical-align: middle; border-top: 1px solid var(--sig-border); }
    .par-table th.num, .par-table td.num { text-align: right; white-space: nowrap; }
    .par-table td.actions { text-align: right; white-space: nowrap; }
    .avance-col { width: 150px; }
    .mono { font-family: 'Roboto Mono', monospace; }
    .row-active { background: var(--sig-bg-hover); }
    .row-total td { border-top: 2px solid var(--sig-border-light); }
    .cell-title { font-weight: 600; color: var(--sig-text-primary); }
    .cell-sub { color: var(--sig-text-muted); font-size: 12px; }
    .sig-type-pill { background: var(--sig-bg-card-alt); border: 1px solid var(--sig-border); border-radius: 20px; padding: 2px 10px; font-size: 11px; color: var(--sig-text-secondary); }
    .avance { display: flex; align-items: center; gap: 8px; }
    .avance .bar { flex: 1; height: 6px; background: var(--sig-bg-card-alt); border-radius: 4px; overflow: hidden; }
    .avance .fill { height: 100%; border-radius: 4px; background: var(--sig-success); }
    .avance .fill.warn { background: var(--sig-warning); } .avance .fill.danger { background: var(--sig-danger); }
    .avance .pct { font-size: 11px; color: var(--sig-text-secondary); width: 34px; text-align: right; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 4px 16px; }
    .form-grid .full { grid-column: 1 / -1; }
    .editor-actions { display: flex; gap: 12px; margin-top: 4px; }
    .col-side .full { width: 100%; } .full-btn { width: 100%; margin-bottom: 8px; }
    .kv { display: flex; justify-content: space-between; gap: 8px; padding: 7px 0; font-size: 13px; border-bottom: 1px solid var(--sig-border); }
    .kv:last-child { border-bottom: none; }
    .kv span { color: var(--sig-text-muted); } .kv strong { color: var(--sig-text-primary); }
    .kv .neg { color: var(--sig-danger); } .kv .pos { color: var(--sig-success); }
    .empty { color: var(--sig-text-muted); padding: 16px 0; text-align: center; }
    .editor-card { margin-top: 16px; }
    .sig-form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 12px; }
    @media (max-width: 599px) { .sig-form-row { grid-template-columns: 1fr; } }
    .mono-num { font-family: 'Roboto Mono', monospace; }
    .sig-table-actions { display: flex; gap: 4px; align-items: center; }
    .cell-title { font-weight: 600; color: var(--sig-text-primary); }
  `],
})
export class ConfigPresupuestoComponent implements OnInit {
  private readonly svc = inject(ConfigPresupuestoService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly clientSvc = inject(ClientService);
  private readonly conceptSvc = inject(ConceptService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotifyService);

  // Pestaña activa: 'presupuesto' | 'forecast' | 'conceptos-pago'
  protected readonly vista = signal<'presupuesto' | 'forecast' | 'conceptos-pago'>('presupuesto');

  protected readonly servicios = signal<ServiceListItemDto[]>([]);
  protected readonly clientes = signal<ClientListItemDto[]>([]);
  protected readonly serviceId = signal<number | null>(null);
  protected serviceIdValue: number | null = null;
  protected clienteFilter: number | null = null;
  protected anioFilter: number = new Date().getFullYear();
  protected readonly anios = [new Date().getFullYear() - 1, new Date().getFullYear(), new Date().getFullYear() + 1];
  protected readonly config = signal<ConfigPresupuestoDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly editor = signal<EditorState | null>(null);
  protected margenObjetivo: number | null = null;

  // Conceptos de pago (3ª pestaña)
  protected readonly conceptosPago = signal<ConceptListItemDto[]>([]);
  protected readonly paginaConceptos = signal(1);
  protected readonly tamañoPaginaConceptos = signal(25);
  protected readonly totalConceptos = signal(0);
  protected readonly loadingConceptos = signal(false);
  protected readonly savingConcepto = signal(false);
  protected readonly editorConcepto = signal<EditorConceptoState | null>(null);

  protected readonly canManage = computed(() => (this.auth.currentUser()?.roles ?? []).includes('Administrator'));
  protected readonly serviciosFiltrados = computed(() => {
    const c = this.clienteFilter;
    return c == null ? this.servicios() : this.servicios().filter((s) => s.clientId === c);
  });

  ngOnInit(): void {
    this.clientSvc.list(1, 500).subscribe({ next: (r) => this.clientes.set(r.items), error: () => this.clientes.set([]) });
    this.serviceSvc.list(1, 500).subscribe({
      next: (r) => {
        this.servicios.set(r.items);
        if (r.items.length > 0) {
          this.serviceIdValue = r.items[0].id;
          this.serviceId.set(r.items[0].id);
          this.loadData();
        } else { this.loading.set(false); }
      },
      error: () => { this.servicios.set([]); this.loading.set(false); },
    });
  }

  protected onCliente(): void {
    const first = this.serviciosFiltrados()[0];
    if (first) { this.serviceIdValue = first.id; this.onService(first.id); }
  }

  protected onService(id: number): void {
    this.serviceId.set(id);
    this.editor.set(null);
    this.loadData();
  }

  private loadData(): void {
    const id = this.serviceId();
    if (!id) return;
    this.loading.set(true);
    this.svc.getConfig(id).subscribe({
      next: (cfg) => { this.config.set(cfg); this.margenObjetivo = cfg.margenObjetivoPct ?? null; this.loading.set(false); },
      error: () => { this.config.set(null); this.loading.set(false); },
    });
  }

  protected tipoLabel(t: TipoPartidaPresupuesto): string { return t === 'Anual' ? 'Anual' : 'Total acción'; }
  protected clamp(pct: number): number { return Math.max(0, Math.min(100, pct)); }
  protected avanceClass(pct: number): string { return pct > 100 ? 'danger' : pct >= 85 ? 'warn' : ''; }

  protected nueva(): void {
    this.editor.set({ nombre: '', tipo: 'Anual', anio: new Date().getFullYear(), presupuesto: 0, consumido: 0, descripcion: '' });
  }

  protected editar(p: PartidaPresupuestoDto): void {
    this.editor.set({ id: p.id, nombre: p.nombre, tipo: p.tipo, anio: p.anio ?? null, presupuesto: p.presupuesto, consumido: p.consumido, descripcion: p.descripcion ?? '' });
  }

  protected cancelar(): void { this.editor.set(null); }

  protected guardar(): void {
    const ed = this.editor();
    const id = this.serviceId();
    if (!ed || !id || !ed.nombre.trim()) return;
    this.saving.set(true);
    const req = {
      nombre: ed.nombre.trim(), tipo: ed.tipo,
      anio: ed.tipo === 'Anual' ? (ed.anio ?? null) : null,
      presupuesto: Number(ed.presupuesto) || 0, consumido: Number(ed.consumido) || 0,
      descripcion: ed.descripcion?.trim() || null,
    };
    const op = ed.id ? this.svc.updatePartida(id, ed.id, req) : this.svc.createPartida(id, req);
    op.subscribe({
      next: () => { this.notify.success(ed.id ? 'Partida actualizada' : 'Partida creada'); this.saving.set(false); this.editor.set(null); this.loadData(); },
      error: (err) => { this.saving.set(false); this.notify.error(err?.error?.detail ?? err?.error?.title ?? 'No se pudo guardar la partida'); },
    });
  }

  protected eliminar(p: PartidaPresupuestoDto): void {
    const id = this.serviceId();
    if (!id) return;
    this.svc.deletePartida(id, p.id).subscribe({
      next: () => { this.notify.success('Partida eliminada'); if (this.editor()?.id === p.id) this.editor.set(null); this.loadData(); },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar la partida'),
    });
  }

  protected guardarMargen(): void {
    const id = this.serviceId();
    if (!id) return;
    const val = this.margenObjetivo === null || this.margenObjetivo === undefined ? null : Number(this.margenObjetivo);
    this.svc.setMargenObjetivo(id, { margenObjetivoPct: val }).subscribe({
      next: (cfg) => { this.config.set(cfg); this.notify.success('Margen objetivo guardado'); },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo guardar el margen objetivo'),
    });
  }

  protected onCambioVista(v: 'presupuesto' | 'forecast' | 'conceptos-pago'): void {
    this.vista.set(v);
    if (v === 'conceptos-pago' && this.conceptosPago().length === 0) {
      this.loadConceptosPago();
    }
  }

  protected loadConceptosPago(): void {
    this.loadingConceptos.set(true);
    this.conceptSvc.list(this.paginaConceptos(), this.tamañoPaginaConceptos(), 'Pago', '').subscribe({
      next: (r) => { this.conceptosPago.set(r.items); this.totalConceptos.set(r.total); this.loadingConceptos.set(false); },
      error: () => { this.conceptosPago.set([]); this.totalConceptos.set(0); this.loadingConceptos.set(false); this.notify.error('No se pudieron cargar los conceptos de pago'); },
    });
  }

  protected nuevoConcepto(): void {
    const hoy = new Date().toISOString().split('T')[0];
    this.editorConcepto.set({ nombre: '', fechaDesde: hoy, fechaHasta: null });
  }

  protected editarConcepto(concepto: ConceptListItemDto): void {
    this.editorConcepto.set({ id: concepto.id, nombre: concepto.nombre, fechaDesde: concepto.fechaDesde, fechaHasta: concepto.fechaHasta });
  }

  protected cancelarConcepto(): void { this.editorConcepto.set(null); }

  protected guardarConcepto(): void {
    const ed = this.editorConcepto();
    if (!ed || !ed.nombre.trim() || !ed.fechaDesde) return;
    this.savingConcepto.set(true);
    const req = { nombre: ed.nombre.trim(), tipo: 'Pago' as const, fechaDesde: ed.fechaDesde, fechaHasta: ed.fechaHasta ?? undefined, formulaJson: '{"type":"Number","value":0}', userIds: [] };
    const op = ed.id ? this.conceptSvc.update(ed.id, req) : this.conceptSvc.create(req);
    op.subscribe({
      next: () => { this.notify.success(ed.id ? 'Concepto actualizado' : 'Concepto creado'); this.savingConcepto.set(false); this.editorConcepto.set(null); this.loadConceptosPago(); },
      error: (err) => { this.savingConcepto.set(false); this.notify.error(err?.error?.detail ?? err?.error?.title ?? 'No se pudo guardar el concepto'); },
    });
  }

  protected eliminarConcepto(concepto: ConceptListItemDto): void {
    if (!confirm(`¿Eliminar concepto "${concepto.nombre}"?`)) return;
    this.conceptSvc.delete(concepto.id).subscribe({
      next: () => { this.notify.success('Concepto eliminado'); if (this.editorConcepto()?.id === concepto.id) this.editorConcepto.set(null); this.loadConceptosPago(); },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar el concepto'),
    });
  }
}
