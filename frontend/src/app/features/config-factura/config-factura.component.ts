import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTableModule } from '@angular/material/table';
import { forkJoin } from 'rxjs';
import { ConfigFacturaService } from '../../core/api/config-factura.service';
import { ClientService } from '../../core/api/clients.service';
import { AuthService } from '../../core/auth/auth.service';
import { NotifyService } from '../../core/notify.service';
import { CategoriaFacturaDto, ClientListItemDto, ConceptoDisponibleDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';

interface EditorState { id?: number; nombre: string; conceptIds: number[]; }

// Configuración de Factura (prototipo 25/28): categorías que agrupan conceptos de facturación POR CLIENTE.
// Barra de filtros + KPIs + tabla de categorías + editor + panel de conceptos disponibles (todo con el
// design system --sig-*, como el resto de la app/prototipo).
@Component({
  selector: 'app-config-factura',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatTooltipModule, MatTableModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Configuración de Factura' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title"><mat-icon class="title-icon">request_quote</mat-icon> Configuración de Factura</h1>
      </div>

      @if (canManage() && clienteId()) {
          <div style="margin-bottom: 16px;">
            <button mat-flat-button color="primary" (click)="nueva()" data-testid="btn-nueva-categoria">
              <mat-icon>add</mat-icon> Nueva categoría
            </button>
          </div>
        }

        <!-- Barra de filtros (prototipo) -->
      <div class="sig-filter-bar">
        <mat-form-field appearance="outline" class="ff ff-wide">
          <mat-label>Cliente</mat-label>
          <mat-select [(value)]="clienteIdValue" (selectionChange)="onCliente($event.value)" data-testid="filtro-cliente">
            @for (c of clientes(); track c.id) { <mat-option [value]="c.id">{{ c.nombre }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" class="ff ff-wide">
          <mat-label>Buscar categoría…</mat-label>
          <mat-icon matPrefix>search</mat-icon>
          <input matInput [(ngModel)]="busqueda" data-testid="filtro-buscar" />
        </mat-form-field>
      </div>

      <!-- KPIs (prototipo) -->
      <div class="kpi-row">
        <div class="sig-kpi-card">
          <div class="sig-kpi-card__label">Categorías de factura</div>
          <div class="sig-kpi-card__value">{{ categorias().length }}</div>
        </div>
        <div class="sig-kpi-card">
          <div class="sig-kpi-card__label">Conceptos mapeados</div>
          <div class="sig-kpi-card__value">{{ mapeados() }}</div>
        </div>
        <div class="sig-kpi-card">
          <div class="sig-kpi-card__label">Cliente</div>
          <div class="sig-kpi-card__value sig-cliente">{{ clienteNombre() || '—' }}</div>
        </div>
        <div class="sig-kpi-card">
          <div class="sig-kpi-card__label">Conceptos sin asignar</div>
          <div class="sig-kpi-card__value" [class.sig-warn]="sinAsignar() > 0">{{ sinAsignar() }}</div>
        </div>
      </div>

      @if (loading()) {
        <sig-skeleton [count]="4" />
      } @else if (!clienteId()) {
        <mat-card><mat-card-content><p class="empty">Selecciona un cliente para configurar sus categorías de factura.</p></mat-card-content></mat-card>
      } @else {
        <div class="layout">
          <div class="col-main">
            <mat-card>
              <mat-card-content>
                <div class="section-head"><span class="dot"></span><h2>Categorías de factura — {{ clienteNombre() }}</h2></div>
                <p class="info-note">
                  <mat-icon>info</mat-icon>
                  <span>Una <strong>categoría de factura</strong> agrupa (suma) uno o varios conceptos para mostrarse como una sola línea en la factura del cliente. Se definen <strong>por cliente</strong>. Las categorías concretas están pendientes de validar con SIG.</span>
                </p>
                @if (categoriasFiltradas().length === 0) {
                  <p class="empty">{{ categorias().length === 0 ? 'Este cliente aún no tiene categorías de factura.' : 'Sin coincidencias para la búsqueda.' }}</p>
                } @else {
                  <table class="sig-table cat-table">
                    <thead>
                      <tr><th>Categoría de factura</th><th>Conceptos que suma</th><th class="num">Nº</th><th></th></tr>
                    </thead>
                    <tbody>
                      @for (cat of categoriasFiltradas(); track cat.id) {
                        <tr [class.row-active]="editor()?.id === cat.id">
                          <td class="cell-title">{{ cat.nombre }}</td>
                          <td>
                            @if (cat.conceptos.length === 0) { <span class="cell-sub">—</span> }
                            @for (c of cat.conceptos; track c.conceptId) { <span class="chip">{{ c.nombre }}</span> }
                          </td>
                          <td class="num"><span class="count-badge">{{ cat.conceptos.length }}</span></td>
                          <td class="actions">
                            @if (canManage()) {
                              <button mat-button color="primary" (click)="editar(cat)" data-testid="btn-editar">Editar</button>
                              <button mat-icon-button (click)="eliminar(cat)" matTooltip="Eliminar" data-testid="btn-eliminar"><mat-icon>delete_outline</mat-icon></button>
                            }
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                }
              </mat-card-content>
            </mat-card>

            <!-- Editor de categoría -->
            @if (editor(); as ed) {
              <mat-card class="editor-card">
                <mat-card-content>
                  <div class="section-head"><span class="dot dot-blue"></span><h2>{{ ed.id ? 'Editar categoría' : 'Nueva categoría' }}</h2></div>
                  <mat-form-field appearance="outline" class="full">
                    <mat-label>Nombre de la categoría</mat-label>
                    <input matInput [(ngModel)]="ed.nombre" placeholder="Ej. Gastos de personal" data-testid="input-nombre" />
                  </mat-form-field>

                  <div class="field-label">Conceptos que suma</div>
                  <div class="chips-row">
                    @if (ed.conceptIds.length === 0) { <span class="cell-sub">Sin conceptos asignados</span> }
                    @for (id of ed.conceptIds; track id) {
                      <span class="chip chip-removable">{{ nombreConcepto(id) }}
                        <button class="chip-x" (click)="quitarConcepto(id)" data-testid="btn-quitar-concepto"><mat-icon>close</mat-icon></button>
                      </span>
                    }
                  </div>

                  @if (addables().length > 0) {
                    <mat-form-field appearance="outline" class="full">
                      <mat-label>+ Añadir concepto</mat-label>
                      <mat-select [value]="null" (selectionChange)="anadirConcepto($event.value)" data-testid="select-anadir-concepto">
                        @for (c of addables(); track c.conceptId) { <mat-option [value]="c.conceptId">{{ c.nombre }}</mat-option> }
                      </mat-select>
                    </mat-form-field>
                  } @else {
                    <p class="cell-sub">No quedan conceptos de facturación disponibles para añadir.</p>
                  }

                  <div class="aparece">
                    <span class="field-label">Aparece en factura como</span>
                    <div><strong>{{ ed.nombre || '(sin nombre)' }}</strong> = suma de {{ ed.conceptIds.length }} concepto(s)</div>
                  </div>

                  <div class="editor-actions">
                    <button mat-flat-button color="primary" [disabled]="!ed.nombre.trim() || saving()" (click)="guardar()" data-testid="btn-guardar">
                      <mat-icon>save</mat-icon> Guardar configuración
                    </button>
                    <button mat-stroked-button (click)="cancelar()" data-testid="btn-cancelar">Cancelar</button>
                  </div>
                </mat-card-content>
              </mat-card>
            }
          </div>

          <!-- Panel: conceptos disponibles -->
          <mat-card class="col-side">
            <mat-card-content>
              <div class="section-head"><span class="dot dot-green"></span><h2>Conceptos disponibles del cliente</h2></div>
              @if (disponibles().length === 0) {
                <p class="cell-sub">El cliente no tiene conceptos de facturación.</p>
              } @else {
                @for (c of disponibles(); track c.conceptId) {
                  <div class="disp-row">
                    <span class="disp-name">{{ c.nombre }}</span>
                    @if (c.asignado) {
                      <span class="sig-badge sig-badge--active" [matTooltip]="c.categoriaNombre || ''">{{ c.categoriaNombre }}</span>
                    } @else {
                      <span class="sig-badge sig-badge--review">Sin asignar</span>
                    }
                  </div>
                }
              }
            </mat-card-content>
          </mat-card>
        </div>
      }
    </div>
  `,
  styles: [`
    .title-icon { vertical-align: middle; margin-right: 6px; }
    .sig-filter-bar { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; margin-bottom: 16px; }
    .ff { width: 220px; } .ff-wide { width: 300px; }
    .kpi-row { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 20px; }
    @media (max-width: 900px) { .kpi-row { grid-template-columns: repeat(2, 1fr); } }
    .sig-kpi-card { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; padding: 18px 20px; }
    .sig-kpi-card__label { font-size: 11px; font-weight: 600; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); margin-bottom: 10px; }
    .sig-kpi-card__value { font-size: 28px; font-weight: 700; color: var(--sig-text-heading); font-family: 'Roboto Mono', monospace; line-height: 1; }
    .sig-kpi-card__value.sig-cliente { font-size: 18px; font-family: inherit; }
    .sig-warn { color: var(--sig-warning); }
    .layout { display: grid; grid-template-columns: 1fr 320px; gap: 16px; align-items: start; }
    @media (max-width: 1100px) { .layout { grid-template-columns: 1fr; } }
    .col-main { display: flex; flex-direction: column; gap: 16px; }
    .section-head { display: flex; align-items: center; gap: 8px; margin-bottom: 12px; }
    .section-head h2 { font-size: 14px; font-weight: 600; margin: 0; color: var(--sig-text-heading); }
    .dot { width: 8px; height: 8px; border-radius: 50%; background: var(--sig-teal); }
    .dot-blue { background: var(--sig-blue-light); } .dot-green { background: var(--sig-success); }
    .info-note { display: flex; gap: 8px; align-items: flex-start; background: var(--sig-teal-bg); border: 1px solid var(--sig-teal); border-radius: 8px; padding: 10px 12px; font-size: 12.5px; color: var(--sig-text-secondary); margin-bottom: 14px; }
    .info-note mat-icon { color: var(--sig-teal); font-size: 18px; width: 18px; height: 18px; flex-shrink: 0; }
    .cat-table { width: 100%; }
    .cat-table th { font-size: 11px; text-transform: uppercase; letter-spacing: .05em; color: var(--sig-text-muted); text-align: left; padding: 8px; }
    .cat-table td { padding: 10px 8px; vertical-align: top; border-top: 1px solid var(--sig-border); }
    .cat-table th.num, .cat-table td.num { text-align: center; width: 3rem; }
    .cat-table td.actions { text-align: right; white-space: nowrap; }
    .row-active { background: var(--sig-bg-hover); }
    .cell-title { font-weight: 600; color: var(--sig-text-primary); }
    .cell-sub { color: var(--sig-text-muted); font-size: 12px; }
    .count-badge { display: inline-block; min-width: 22px; padding: 1px 7px; border-radius: 11px; background: var(--sig-bg-card-alt); border: 1px solid var(--sig-border); font-size: 12px; color: var(--sig-text-secondary); }
    .chip { display: inline-flex; align-items: center; background: var(--sig-bg-card-alt); border: 1px solid var(--sig-border); border-radius: 14px; padding: 3px 10px; margin: 2px 4px 2px 0; font-size: 12px; color: var(--sig-text-secondary); }
    .chip-removable { padding-right: 3px; }
    .chip-x { display: inline-flex; align-items: center; justify-content: center; border: none; background: transparent; color: var(--sig-text-muted); cursor: pointer; padding: 0; margin-left: 4px; }
    .chip-x mat-icon { font-size: 15px; width: 15px; height: 15px; }
    .full { width: 100%; }
    .field-label { font-size: 11px; color: var(--sig-text-muted); margin: 8px 0 4px; text-transform: uppercase; letter-spacing: .05em; }
    .chips-row { min-height: 2rem; margin-bottom: 8px; }
    .aparece { background: var(--sig-bg-card-alt); border-radius: 8px; padding: 10px 12px; margin: 4px 0 14px; font-size: 13px; color: var(--sig-text-secondary); }
    .editor-actions { display: flex; gap: 12px; }
    .disp-row { display: flex; align-items: center; justify-content: space-between; gap: 8px; padding: 8px 0; border-bottom: 1px solid var(--sig-border); }
    .disp-row:last-child { border-bottom: none; }
    .disp-name { font-size: 13px; color: var(--sig-text-primary); }
    .empty { color: var(--sig-text-muted); padding: 16px 0; text-align: center; }
    .vista-toggle { display: flex; gap: 12px; margin-bottom: 16px; }
    .sig-form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 12px; }
    @media (max-width: 599px) { .sig-form-row { grid-template-columns: 1fr; } }
    .mono-num { font-family: 'Roboto Mono', monospace; }
    .sig-table-actions { display: flex; gap: 4px; align-items: center; }
  `],
})
export class ConfigFacturaComponent implements OnInit {
  private readonly svc = inject(ConfigFacturaService);
  private readonly clientSvc = inject(ClientService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotifyService);

  protected readonly clientes = signal<ClientListItemDto[]>([]);
  protected readonly clienteId = signal<number | null>(null);
  protected clienteIdValue: number | null = null;
  protected busqueda = '';
  protected readonly categorias = signal<CategoriaFacturaDto[]>([]);
  protected readonly disponibles = signal<ConceptoDisponibleDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly editor = signal<EditorState | null>(null);

  protected readonly canManage = computed(() => (this.auth.currentUser()?.roles ?? []).includes('Administrator'));
  protected readonly clienteNombre = computed(() => this.clientes().find((c) => c.id === this.clienteId())?.nombre ?? '');
  protected readonly mapeados = computed(() => this.categorias().reduce((acc, c) => acc + c.conceptos.length, 0));
  protected readonly sinAsignar = computed(() => this.disponibles().filter((d) => !d.asignado).length);
  protected readonly categoriasFiltradas = computed(() => {
    const q = this.busqueda.trim().toLowerCase();
    return q ? this.categorias().filter((c) => c.nombre.toLowerCase().includes(q)) : this.categorias();
  });

  protected readonly addables = computed(() => {
    const ed = this.editor();
    if (!ed) return [];
    const yaEn = new Set(ed.conceptIds);
    return this.disponibles().filter((d) => !yaEn.has(d.conceptId) && (!d.asignado || d.categoriaFacturaId === ed.id));
  });

  ngOnInit(): void {
    this.clientSvc.list(1, 500).subscribe({
      next: (r) => {
        this.clientes.set(r.items);
        if (r.items.length > 0) {
          this.clienteIdValue = r.items[0].id;
          this.clienteId.set(r.items[0].id);
          this.loadData();
        } else { this.loading.set(false); }
      },
      error: () => { this.clientes.set([]); this.loading.set(false); },
    });
  }

  protected onCliente(id: number): void {
    this.clienteId.set(id);
    this.editor.set(null);
    this.loadData();
  }

  private loadData(): void {
    const id = this.clienteId();
    if (!id) return;
    this.loading.set(true);
    forkJoin({ categorias: this.svc.list(id), disponibles: this.svc.conceptosDisponibles(id) }).subscribe({
      next: (r) => { this.categorias.set(r.categorias); this.disponibles.set(r.disponibles); this.loading.set(false); },
      error: () => { this.categorias.set([]); this.disponibles.set([]); this.loading.set(false); },
    });
  }

  protected nombreConcepto(id: number): string {
    return this.disponibles().find((d) => d.conceptId === id)?.nombre
      ?? this.categorias().flatMap((c) => c.conceptos).find((c) => c.conceptId === id)?.nombre
      ?? `#${id}`;
  }

  protected nueva(): void { this.editor.set({ nombre: '', conceptIds: [] }); }
  protected editar(cat: CategoriaFacturaDto): void {
    this.editor.set({ id: cat.id, nombre: cat.nombre, conceptIds: cat.conceptos.map((c) => c.conceptId) });
  }
  protected cancelar(): void { this.editor.set(null); }

  protected anadirConcepto(conceptId: number): void {
    const ed = this.editor();
    if (!ed || conceptId == null || ed.conceptIds.includes(conceptId)) return;
    this.editor.set({ ...ed, conceptIds: [...ed.conceptIds, conceptId] });
  }
  protected quitarConcepto(conceptId: number): void {
    const ed = this.editor();
    if (!ed) return;
    this.editor.set({ ...ed, conceptIds: ed.conceptIds.filter((id) => id !== conceptId) });
  }

  protected guardar(): void {
    const ed = this.editor();
    const clientId = this.clienteId();
    if (!ed || !clientId || !ed.nombre.trim()) return;
    this.saving.set(true);
    const req = { nombre: ed.nombre.trim(), conceptIds: ed.conceptIds };
    const op = ed.id ? this.svc.update(clientId, ed.id, req) : this.svc.create(clientId, req);
    op.subscribe({
      next: () => { this.notify.success(ed.id ? 'Categoría actualizada' : 'Categoría creada'); this.saving.set(false); this.editor.set(null); this.loadData(); },
      error: (err) => { this.saving.set(false); this.notify.error(err?.error?.detail ?? err?.error?.title ?? 'No se pudo guardar la categoría'); },
    });
  }

  protected eliminar(cat: CategoriaFacturaDto): void {
    const clientId = this.clienteId();
    if (!clientId) return;
    this.svc.delete(clientId, cat.id).subscribe({
      next: () => { this.notify.success('Categoría eliminada'); if (this.editor()?.id === cat.id) this.editor.set(null); this.loadData(); },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar la categoría'),
    });
  }
}
