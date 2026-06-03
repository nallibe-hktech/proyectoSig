import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { ConceptService } from '../../core/api/concepts.service';
import { VariableService } from '../../core/api/misc.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import {
  FormulaNode, FormulaFilter, AggregateOp, BinaryOpKind, SourceEntity, FilterOp, VariableDto, ConceptDetailDto,
} from '../../models/dtos';

/**
 * Editor visual de fórmula (CRÍTICO). Renderiza el AST como cajas anidadas.
 * Cada nodo se edita inline. Los nodos contenedor (Aggregate / BinaryOp) tienen
 * slots hijos que pueden ser cualquier otro tipo de nodo.
 */
@Component({
  selector: 'app-formula-editor',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatChipsModule, MatTooltipModule, MatDividerModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Concepts', route: '/concepts' }, { label: concept()?.nombre ?? '...', route: concept() ? '/concepts/' + concept()!.id : '/concepts' }, { label: 'Editor de Fórmula' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Editor de fórmula: {{ concept()?.nombre ?? 'cargando...' }}</h1>
      </div>

      @if (loading()) {
        <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card>
      } @else {
        <mat-card style="margin-bottom: 16px;">
          <mat-card-content>
            <h3 class="sig-section-label">Expresión actual</h3>
            <div class="sig-expression" data-testid="formula-expression">{{ expression() }}</div>
            <h3 class="sig-section-label" style="margin-top: 16px;">Validación</h3>
            @if (root()) {
              <div [class.sig-valid]="isValid()" [class.sig-invalid]="!isValid()" data-testid="formula-validity">
                <mat-icon style="vertical-align: middle; font-size: 18px; width: 18px; height: 18px;">{{ isValid() ? 'check_circle' : 'error' }}</mat-icon>
                {{ isValid() ? 'Fórmula válida' : 'Fórmula incompleta: ' + invalidReason() }}
              </div>
            } @else {
              <div class="sig-invalid">Sin fórmula. Añade un nodo raíz.</div>
            }
          </mat-card-content>
        </mat-card>

        <div class="sig-editor-grid">
          <!-- Paleta de primitivas -->
          <mat-card class="sig-palette" data-testid="palette">
            <mat-card-header><mat-card-title>Primitivas</mat-card-title></mat-card-header>
            <mat-card-content>
              <button mat-stroked-button class="sig-prim-btn" (click)="setRoot(createNumber())" data-testid="prim-number"><mat-icon>numbers</mat-icon> Número</button>
              <button mat-stroked-button class="sig-prim-btn" (click)="setRoot(createVariable())" data-testid="prim-variable"><mat-icon>data_object</mat-icon> Variable</button>
              <button mat-stroked-button class="sig-prim-btn" (click)="setRoot(createBinaryOp())" data-testid="prim-binaryop"><mat-icon>functions</mat-icon> Operación</button>
              <button mat-stroked-button class="sig-prim-btn" (click)="setRoot(createAggregate())" data-testid="prim-aggregate"><mat-icon>calculate</mat-icon> Agregado</button>
              <button mat-stroked-button class="sig-prim-btn" (click)="setRoot(createSource())" data-testid="prim-source"><mat-icon>storage</mat-icon> Entidad</button>
            </mat-card-content>
          </mat-card>

          <!-- Canvas -->
          <mat-card class="sig-canvas" data-testid="canvas">
            <mat-card-header><mat-card-title>Canvas de fórmula</mat-card-title></mat-card-header>
            <mat-card-content>
              @if (root()) {
                <ng-container [ngTemplateOutlet]="nodeTpl" [ngTemplateOutletContext]="{ node: root(), parent: null, key: 'root' }"></ng-container>
              } @else {
                <p style="color: var(--mat-sys-on-surface-variant);">Selecciona una primitiva de la izquierda para empezar.</p>
              }
            </mat-card-content>
          </mat-card>
        </div>

        <div class="sig-form-actions">
          <a mat-stroked-button [routerLink]="['/concepts', conceptId]" data-testid="btn-cancelar">Cancelar</a>
          <button mat-flat-button color="primary" type="button" [disabled]="!isValid() || saving()" (click)="save()" data-testid="btn-guardar-formula">
            <mat-icon>save</mat-icon> Guardar fórmula
          </button>
        </div>
      }

      <!-- ng-template recursivo para nodos -->
      <ng-template #nodeTpl let-node="node" let-parent="parent" let-key="key">
        <div [class]="'sig-node sig-node--' + node.type" [attr.data-testid]="'node-' + node.type">
          <div class="sig-node-header">
            <mat-icon class="sig-node-icon" aria-hidden="true">{{ iconFor(node.type) }}</mat-icon>
            <span class="sig-node-title">{{ labelFor(node.type) }}</span>
            <span style="flex: 1;"></span>
            @if (parent !== null || key === 'root') {
              <button mat-icon-button (click)="removeNode(parent, key)" aria-label="Quitar nodo" [attr.data-testid]="'btn-remove-' + node.type">
                <mat-icon>close</mat-icon>
              </button>
            }
          </div>
          <div class="sig-node-body">
            <!-- Number -->
            @if (node.type === 'Number') {
              <mat-form-field appearance="outline" class="sig-inline-field">
                <mat-label>Valor</mat-label>
                <input matInput type="number" [(ngModel)]="node.value" [attr.data-testid]="'input-number-value'" />
              </mat-form-field>
            }
            <!-- Variable -->
            @if (node.type === 'Variable') {
              <mat-form-field appearance="outline" class="sig-inline-field">
                <mat-label>Variable</mat-label>
                <mat-select [(ngModel)]="node.variableId" data-testid="select-variable">
                  @for (v of variables(); track v.id) {
                    <mat-option [value]="v.id">{{ v.nombre }} ({{ v.questionIdExterno }})</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            }
            <!-- BinaryOp -->
            @if (node.type === 'BinaryOp') {
              <div class="sig-binop">
                <mat-form-field appearance="outline" class="sig-op-field">
                  <mat-label>Operador</mat-label>
                  <mat-select [(ngModel)]="node.op" data-testid="select-operator">
                    <mat-option value="Add">+ Sumar</mat-option>
                    <mat-option value="Sub">− Restar</mat-option>
                    <mat-option value="Mul">× Multiplicar</mat-option>
                    <mat-option value="Div">÷ Dividir</mat-option>
                    <mat-option value="Pct">% Porcentaje</mat-option>
                  </mat-select>
                </mat-form-field>
                <div class="sig-slot">
                  <div class="sig-slot-label">Operando izquierdo</div>
                  @if (node.left) {
                    <ng-container [ngTemplateOutlet]="nodeTpl" [ngTemplateOutletContext]="{ node: node.left, parent: node, key: 'left' }"></ng-container>
                  } @else {
                    <div class="sig-empty-slot">
                      <button mat-stroked-button (click)="setSlot(node, 'left', createNumber())">+ Número</button>
                      <button mat-stroked-button (click)="setSlot(node, 'left', createAggregate())">+ Agregado</button>
                      <button mat-stroked-button (click)="setSlot(node, 'left', createVariable())">+ Variable</button>
                      <button mat-stroked-button (click)="setSlot(node, 'left', createBinaryOp())">+ Operación</button>
                    </div>
                  }
                </div>
                <div class="sig-slot">
                  <div class="sig-slot-label">Operando derecho</div>
                  @if (node.right) {
                    <ng-container [ngTemplateOutlet]="nodeTpl" [ngTemplateOutletContext]="{ node: node.right, parent: node, key: 'right' }"></ng-container>
                  } @else {
                    <div class="sig-empty-slot">
                      <button mat-stroked-button (click)="setSlot(node, 'right', createNumber())">+ Número</button>
                      <button mat-stroked-button (click)="setSlot(node, 'right', createAggregate())">+ Agregado</button>
                      <button mat-stroked-button (click)="setSlot(node, 'right', createVariable())">+ Variable</button>
                    </div>
                  }
                </div>
              </div>
            }
            <!-- Aggregate -->
            @if (node.type === 'Aggregate') {
              <div class="sig-aggregate">
                <mat-form-field appearance="outline" class="sig-op-field">
                  <mat-label>Operador</mat-label>
                  <mat-select [(ngModel)]="node.op" data-testid="select-aggregate">
                    <mat-option value="Sum">Suma</mat-option>
                    <mat-option value="Count">Cuenta</mat-option>
                    <mat-option value="Min">Mínimo</mat-option>
                    <mat-option value="Max">Máximo</mat-option>
                  </mat-select>
                </mat-form-field>
                <mat-form-field appearance="outline" class="sig-op-field">
                  <mat-label>Campo (opcional)</mat-label>
                  <mat-select [(ngModel)]="node.field">
                    <mat-option [value]="null">— Sin campo —</mat-option>
                    <mat-option value="Importe">Importe</mat-option>
                    <mat-option value="Horas">Horas</mat-option>
                  </mat-select>
                </mat-form-field>
                <div class="sig-slot">
                  <div class="sig-slot-label">Entidad origen</div>
                  @if (node.source && node.source.type === 'Source') {
                    <ng-container [ngTemplateOutlet]="nodeTpl" [ngTemplateOutletContext]="{ node: node.source, parent: node, key: 'source' }"></ng-container>
                  } @else {
                    <button mat-stroked-button (click)="setSlot(node, 'source', createSource())">+ Añadir entidad</button>
                  }
                </div>
              </div>
            }
            <!-- Source -->
            @if (node.type === 'Source') {
              <div class="sig-source">
                <mat-form-field appearance="outline" class="sig-op-field">
                  <mat-label>Entidad</mat-label>
                  <mat-select [(ngModel)]="node.entity" data-testid="select-source-entity">
                    <mat-option value="VisitasCelero">VisitasCelero</mat-option>
                    <mat-option value="GastosPayHawk">GastosPayHawk</mat-option>
                    <mat-option value="HorasBizneo">HorasBizneo</mat-option>
                    <mat-option value="HorasIntratime">HorasIntratime</mat-option>
                  </mat-select>
                </mat-form-field>
                <div class="sig-filters-section">
                  <strong style="font-size: 13px;">Filtros</strong>
                  @for (f of node.filters; track $index; let i = $index) {
                    <div class="sig-filter-row">
                      <mat-form-field appearance="outline" class="sig-filter-small">
                        <mat-label>Campo</mat-label>
                        <input matInput [(ngModel)]="f.field" />
                      </mat-form-field>
                      <mat-form-field appearance="outline" class="sig-filter-small">
                        <mat-label>Operador</mat-label>
                        <mat-select [(ngModel)]="f.op">
                          <mat-option value="Eq">=</mat-option>
                          <mat-option value="Neq">≠</mat-option>
                          <mat-option value="Gt">&gt;</mat-option>
                          <mat-option value="Gte">≥</mat-option>
                          <mat-option value="Lt">&lt;</mat-option>
                          <mat-option value="Lte">≤</mat-option>
                        </mat-select>
                      </mat-form-field>
                      <mat-form-field appearance="outline" class="sig-filter-small">
                        <mat-label>Valor</mat-label>
                        <input matInput [(ngModel)]="f.value" />
                      </mat-form-field>
                      <button mat-icon-button (click)="removeFilter(node, i)" aria-label="Quitar filtro"><mat-icon>close</mat-icon></button>
                    </div>
                  }
                  <button mat-stroked-button (click)="addFilter(node)" data-testid="btn-add-filter"><mat-icon>add</mat-icon> Añadir filtro</button>
                </div>
              </div>
            }
          </div>
        </div>
      </ng-template>
    </div>
  `,
  styles: [`
    .sig-editor-grid { display: grid; grid-template-columns: 260px 1fr; gap: 16px; }
    @media (max-width: 959px) { .sig-editor-grid { grid-template-columns: 1fr; } }
    /* Ensure palettes and canvas follow the current theme tokens */
    .sig-palette, .sig-canvas { background: var(--mat-sys-surface); color: var(--mat-sys-on-surface); }
    .sig-palette mat-card-content { display: flex; flex-direction: column; gap: 8px; }
    .sig-prim-btn { justify-content: flex-start; }
    .sig-canvas mat-card-content { min-height: 200px; }
    .sig-section-label { font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.08em; color: var(--mat-sys-on-surface-variant); margin: 0 0 8px; }
    .sig-expression { font-family: 'Roboto Mono', monospace; font-size: 15px; padding: 12px; background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface); border-radius: 8px; }
    .sig-valid { color: var(--sig-success); display: flex; align-items: center; gap: 6px; font-size: 14px; }
    .sig-invalid { color: var(--sig-warning); display: flex; align-items: center; gap: 6px; font-size: 14px; }
    .sig-node { border: 2px solid var(--mat-sys-outline-variant); border-radius: 8px; padding: 12px; margin: 8px 0; background: var(--mat-sys-surface); color: var(--mat-sys-on-surface); }
    .sig-node--Number { border-color: var(--sig-blue); }
    .sig-node--Variable { border-color: var(--sig-teal); }
    .sig-node--BinaryOp { border-color: #f59e0b; }
    .sig-node--Source { border-color: var(--sig-success); }
    .sig-node--Aggregate { border-color: var(--sig-warning); }
    .sig-node-header { display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
    .sig-node-title { font-weight: 600; font-size: 14px; color: var(--mat-sys-on-surface); }
    .sig-node-icon { color: inherit; }
    .sig-inline-field, .sig-op-field { width: 200px; max-width: 100%; }
    .sig-binop, .sig-aggregate, .sig-source { display: flex; flex-direction: column; gap: 8px; }
    .sig-slot { padding-left: 16px; border-left: 3px solid var(--mat-sys-outline-variant); margin: 8px 0; }
    .sig-slot-label { font-size: 12px; font-weight: 600; color: var(--mat-sys-on-surface-variant); margin-bottom: 4px; }
    .sig-empty-slot { display: flex; gap: 4px; flex-wrap: wrap; }
    .sig-filters-section { display: flex; flex-direction: column; gap: 4px; padding: 8px; background: var(--mat-sys-surface); color: var(--mat-sys-on-surface); border-radius: 4px; }
    .sig-filter-row { display: flex; gap: 4px; align-items: center; flex-wrap: wrap; }
    .sig-filter-small { width: 140px; }
    .sig-form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
  `],
})
export class FormulaEditorComponent implements OnInit {
  private readonly conceptSvc = inject(ConceptService);
  private readonly variableSvc = inject(VariableService);
  private readonly notify = inject(NotifyService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly concept = signal<ConceptDetailDto | null>(null);
  protected readonly variables = signal<VariableDto[]>([]);
  protected conceptId = 0;

  // Para forzar re-evaluación visual del AST que mutamos in-place
  private readonly tick = signal(0);
  protected readonly root = signal<FormulaNode | null>(null);

  protected readonly expression = computed(() => {
    this.tick(); // dependency
    const r = this.root(); return r ? this.serializeExpression(r) : '(vacío)';
  });
  protected readonly isValid = computed(() => {
    this.tick();
    const r = this.root(); return r ? this.validateNode(r).ok : false;
  });
  protected readonly invalidReason = computed(() => {
    this.tick();
    const r = this.root(); return r ? this.validateNode(r).reason : 'Sin fórmula';
  });

  ngOnInit(): void {
    this.conceptId = Number(this.route.snapshot.paramMap.get('id'));
    this.variableSvc.list().subscribe({
      next: (v) => this.variables.set(v),
      error: () => this.variables.set([]),
    });
    this.conceptSvc.getById(this.conceptId).subscribe({
      next: (c) => {
        this.concept.set(c);
        try {
          const parsed = JSON.parse(c.formulaJson) as FormulaNode;
          this.root.set(parsed && (parsed as { type?: string }).type ? parsed : null);
        } catch {
          this.root.set(null);
        }
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el concept'); },
    });
  }

  // ---------- Factories ----------
  protected createNumber(): FormulaNode { return { type: 'Number', value: 0 }; }
  protected createVariable(): FormulaNode { return { type: 'Variable', variableId: this.variables()[0]?.id ?? 0 }; }
  protected createBinaryOp(): FormulaNode { return { type: 'BinaryOp', op: 'Mul', left: this.createAggregate(), right: this.createNumber() }; }
  protected createAggregate(): FormulaNode { return { type: 'Aggregate', op: 'Sum', source: this.createSource(), field: null }; }
  protected createSource(): FormulaNode { return { type: 'Source', entity: 'VisitasCelero', field: null, filters: [] }; }

  // ---------- Mutations ----------
  protected setRoot(node: FormulaNode): void { this.root.set(node); this.bump(); }
  protected setSlot(parent: FormulaNode, key: 'left' | 'right' | 'source', node: FormulaNode): void {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (parent as any)[key] = node;
    this.bump();
  }
  protected removeNode(parent: FormulaNode | null, key: string): void {
    if (key === 'root' || parent === null) { this.root.set(null); }
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    else { (parent as any)[key] = null; }
    this.bump();
  }
  protected addFilter(node: FormulaNode): void {
    if (node.type !== 'Source') return;
    node.filters.push({ field: '', op: 'Eq', value: '' });
    this.bump();
  }
  protected removeFilter(node: FormulaNode, idx: number): void {
    if (node.type !== 'Source') return;
    node.filters.splice(idx, 1);
    this.bump();
  }
  private bump(): void { this.tick.update((v) => v + 1); }

  // ---------- Visual helpers ----------
  protected iconFor(type: string): string {
    return { Number: 'numbers', Variable: 'data_object', BinaryOp: 'functions', Aggregate: 'calculate', Source: 'storage' }[type] ?? 'help';
  }
  protected labelFor(type: string): string {
    return { Number: 'Número', Variable: 'Variable', BinaryOp: 'Operación', Aggregate: 'Agregado', Source: 'Entidad' }[type] ?? type;
  }

  private serializeExpression(node: FormulaNode): string {
    switch (node.type) {
      case 'Number': return String(node.value);
      case 'Variable': {
        const v = this.variables().find((x) => x.id === node.variableId);
        return v ? `Variable.${v.nombre}` : `Variable(${node.variableId})`;
      }
      case 'Source': {
        const filters = node.filters.length > 0 ? `, ${node.filters.map((f) => `${f.field}${this.opStr(f.op)}${f.value}`).join(', ')}` : '';
        return `${node.entity}${filters}`;
      }
      case 'Aggregate': {
        const opLabel = { Sum: 'Suma', Count: 'Cuenta', Min: 'Mínimo', Max: 'Máximo' }[node.op];
        const inner = node.source ? this.serializeExpression(node.source) : '(?)';
        const field = node.field ? '.' + node.field : '';
        return `${opLabel}(${inner})${field}`;
      }
      case 'BinaryOp': {
        const opSym = { Add: '+', Sub: '−', Mul: '×', Div: '÷', Pct: '%' }[node.op];
        const l = node.left ? this.serializeExpression(node.left) : '(?)';
        const r = node.right ? this.serializeExpression(node.right) : '(?)';
        return `(${l} ${opSym} ${r})`;
      }
    }
  }
  private opStr(op: FilterOp): string {
    return { Eq: '=', Neq: '≠', Gt: '>', Gte: '≥', Lt: '<', Lte: '≤', In: '∈' }[op] ?? '=';
  }

  private validateNode(node: FormulaNode | null | undefined): { ok: boolean; reason: string } {
    if (!node) return { ok: false, reason: 'Falta un nodo' };
    switch (node.type) {
      case 'Number': return Number.isFinite(node.value) ? { ok: true, reason: '' } : { ok: false, reason: 'Número inválido' };
      case 'Variable': return node.variableId > 0 ? { ok: true, reason: '' } : { ok: false, reason: 'Variable no seleccionada' };
      case 'Source': return !!node.entity ? { ok: true, reason: '' } : { ok: false, reason: 'Entidad sin seleccionar' };
      case 'Aggregate': {
        if (!node.op) return { ok: false, reason: 'Operador de agregado sin seleccionar' };
        return this.validateNode(node.source);
      }
      case 'BinaryOp': {
        const l = this.validateNode(node.left);
        if (!l.ok) return l;
        const r = this.validateNode(node.right);
        if (!r.ok) return r;
        return { ok: true, reason: '' };
      }
    }
  }

  protected save(): void {
    const c = this.concept(); const r = this.root();
    if (!c || !r) return;
    this.saving.set(true);
    const payload = {
      nombre: c.nombre, tipo: c.tipo,
      fechaDesde: c.fechaDesde, fechaHasta: c.fechaHasta ?? null,
      formulaJson: JSON.stringify(r),
      actionIds: c.actionIds, userIds: c.userIds,
    };
    this.conceptSvc.update(c.id, payload).subscribe({
      next: () => { this.saving.set(false); this.notify.success('Fórmula guardada'); void this.router.navigate(['/concepts', c.id]); },
      error: (err) => { this.saving.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar la fórmula'); },
    });
  }
}
