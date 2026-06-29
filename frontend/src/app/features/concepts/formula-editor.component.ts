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
        <!-- Modo guiado: plantillas de "tipo de concepto" (catálogo doc SIG). Arman la fórmula por debajo. -->
        <mat-card style="margin-bottom: 16px;" data-testid="formula-recetas">
          <mat-card-content>
            <h3 class="sig-section-label">Modo guiado — elige un tipo de cálculo</h3>
            <p class="sig-receta-intro">Elige una plantilla y rellena los datos: la fórmula se arma sola. Después puedes afinarla abajo en el modo avanzado.</p>
            <div class="sig-receta-row">
              <mat-form-field appearance="outline" class="sig-receta-tipo">
                <mat-label>Tipo de cálculo</mat-label>
                <mat-select [(ngModel)]="recetaSel" data-testid="receta-tipo">
                  @for (r of recetas; track r.id) { <mat-option [value]="r.id">{{ r.label }}</mat-option> }
                </mat-select>
              </mat-form-field>
              @if (recetaActual(); as r) {
                @if (r.valorLabel) {
                  <mat-form-field appearance="outline" class="sig-receta-valor">
                    <mat-label>{{ r.valorLabel }}</mat-label>
                    <input matInput type="number" [(ngModel)]="recetaValor" data-testid="receta-valor" />
                  </mat-form-field>
                }
                @if (r.filtro) {
                  <mat-form-field appearance="outline" class="sig-receta-filtro">
                    <mat-label>Filtrar por (opcional): campo</mat-label>
                    <input matInput [(ngModel)]="recetaFiltroCampo" placeholder="p.ej. TipoVisita" data-testid="receta-filtro-campo" />
                  </mat-form-field>
                  <mat-form-field appearance="outline" class="sig-receta-filtro">
                    <mat-label>= valor</mat-label>
                    <input matInput [(ngModel)]="recetaFiltroValor" placeholder="p.ej. Premium" data-testid="receta-filtro-valor" />
                  </mat-form-field>
                }
                @if (r.nivel) {
                  <mat-form-field appearance="outline" class="sig-receta-valor">
                    <mat-label>Nivel de tarifa</mat-label>
                    <mat-select [(ngModel)]="recetaNivel" data-testid="receta-nivel">
                      <mat-option value="Global">Global</mat-option>
                      <mat-option value="Cliente">Por cliente</mat-option>
                      <mat-option value="Servicio">Por servicio</mat-option>
                    </mat-select>
                  </mat-form-field>
                }
                @if (r.variable) {
                  <mat-form-field appearance="outline" class="sig-receta-valor">
                    <mat-label>Variable</mat-label>
                    <mat-select [(ngModel)]="recetaVariableId" data-testid="receta-variable">
                      @for (vv of variables(); track vv.id) { <mat-option [value]="vv.id">{{ vv.nombre }}</mat-option> }
                    </mat-select>
                  </mat-form-field>
                }
                @if (r.base) {
                  <mat-form-field appearance="outline" class="sig-receta-valor">
                    <mat-label>Sobre</mat-label>
                    <mat-select [(ngModel)]="recetaBase" data-testid="receta-base">
                      <mat-option value="gastos">Gastos (PayHawk)</mat-option>
                      <mat-option value="horas">Horas (Bizneo)</mat-option>
                      <mat-option value="km">Km (PayHawk)</mat-option>
                      <mat-option value="visitas">Nº de visitas (Celero)</mat-option>
                    </mat-select>
                  </mat-form-field>
                }
              }
              <button mat-flat-button color="primary" (click)="generarReceta()" [disabled]="!recetaSel" data-testid="btn-generar-receta">
                <mat-icon>auto_fix_high</mat-icon> Generar fórmula
              </button>
            </div>
            @if (recetaActual(); as r) {
              <p class="sig-receta-hint"><mat-icon aria-hidden="true">info</mat-icon> {{ r.hint }}</p>
            }
          </mat-card-content>
        </mat-card>

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

        <!-- Preview del cálculo aplicado + jerarquía (penpot: Conceptos de Pago) -->
        <mat-card style="margin-bottom: 16px;" data-testid="formula-preview-aplicado">
          <mat-card-content>
            <div class="sig-preview-head">
              <h3 class="sig-section-label" style="margin:0;">Preview — Cálculo Aplicado <span class="sig-preview-ctx">(Juan Pérez · Mayo 2026)</span></h3>
              <span class="sig-preview-demo">ejemplo ilustrativo</span>
            </div>
            <div class="sig-preview-grid">
              <div class="sig-preview-amount">€ 1.250</div>
              <div class="sig-preview-trace">
                <div><strong>Origen:</strong> Payhawk API <span class="sig-preview-sep">·</span> <strong>Importado:</strong> 31/05/2026 08:00</div>
                <div>Suma de Gasto Payhawk: <strong>1.250,00 €</strong> <span class="sig-preview-muted">(registros: 15/01, 22/01, 07/02…)</span></div>
              </div>
            </div>

            <h3 class="sig-section-label" style="margin:16px 0 8px;">Jerarquía de Aplicación</h3>
            <div class="sig-jerarquia">
              <span class="sig-jer-chip">Global</span>
              <mat-icon class="sig-jer-arrow" aria-hidden="true">chevron_right</mat-icon>
              <span class="sig-jer-chip">Servicio</span>
              <mat-icon class="sig-jer-arrow" aria-hidden="true">chevron_right</mat-icon>
              <span class="sig-jer-chip">Empleado</span>
            </div>
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
              <button mat-stroked-button class="sig-prim-btn" (click)="setRoot(createModifier())" data-testid="prim-modifier"><mat-icon>tune</mat-icon> Modificador</button>
              <button mat-stroked-button class="sig-prim-btn" (click)="setRoot(createTramos())" data-testid="prim-tramos"><mat-icon>stairs</mat-icon> Tramos</button>
              <button mat-stroked-button class="sig-prim-btn" (click)="setRoot(createConceptRef())" data-testid="prim-conceptref"><mat-icon>percent</mat-icon> Fee s/conceptos</button>
              <button mat-stroked-button class="sig-prim-btn" (click)="setRoot(createTarifaRef())" data-testid="prim-tarifaref"><mat-icon>local_offer</mat-icon> Tarifa</button>
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
                    <mat-option value="Km">Km</mat-option>
                  </mat-select>
                </mat-form-field>
                @if (node.op === 'Count') {
                  <mat-form-field appearance="outline" class="sig-op-field">
                    <mat-label>Distinto por (días con actividad)</mat-label>
                    <mat-select [(ngModel)]="node.distinct" data-testid="select-distinct">
                      <mat-option [value]="null">— Contar todas —</mat-option>
                      <mat-option value="Fecha">Fecha (días únicos)</mat-option>
                      <mat-option value="UserId">Recurso (recursos únicos)</mat-option>
                    </mat-select>
                  </mat-form-field>
                }
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
                    <mat-option value="VisitasSgpv">VisitasSgpv</mat-option>
                    <mat-option value="TarifasServicio">TarifasServicio</mat-option>
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
                          <mat-option value="In">∈ (en lista)</mat-option>
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
            <!-- Modifier -->
            @if (node.type === 'Modifier') {
              <div class="sig-binop">
                <mat-form-field appearance="outline" class="sig-op-field">
                  <mat-label>Tipo</mat-label>
                  <mat-select [(ngModel)]="node.kind" data-testid="select-modifier-kind">
                    <mat-option value="Min">Mínimo (suelo: si &lt; X → X)</mat-option>
                    <mat-option value="Max">Máximo (techo: si &gt; X → X)</mat-option>
                    <mat-option value="FloorZero">Umbral (si &lt; X → 0)</mat-option>
                    <mat-option value="Franquicia">Franquicia (resta X, mín. 0)</mat-option>
                  </mat-select>
                </mat-form-field>
                <mat-form-field appearance="outline" class="sig-inline-field">
                  <mat-label>Valor X</mat-label>
                  <input matInput type="number" [(ngModel)]="node.threshold" data-testid="input-modifier-threshold" />
                </mat-form-field>
                <div class="sig-slot">
                  <div class="sig-slot-label">Expresión interior</div>
                  @if (node.inner) {
                    <ng-container [ngTemplateOutlet]="nodeTpl" [ngTemplateOutletContext]="{ node: node.inner, parent: node, key: 'inner' }"></ng-container>
                  } @else {
                    <div class="sig-empty-slot">
                      <button mat-stroked-button (click)="setSlot(node, 'inner', createAggregate())">+ Agregado</button>
                      <button mat-stroked-button (click)="setSlot(node, 'inner', createBinaryOp())">+ Operación</button>
                      <button mat-stroked-button (click)="setSlot(node, 'inner', createNumber())">+ Número</button>
                    </div>
                  }
                </div>
              </div>
            }
            <!-- Tramos -->
            @if (node.type === 'Tramos') {
              <div class="sig-aggregate">
                <div class="sig-slot">
                  <div class="sig-slot-label">Cantidad (horas / unidades)</div>
                  @if (node.cantidad) {
                    <ng-container [ngTemplateOutlet]="nodeTpl" [ngTemplateOutletContext]="{ node: node.cantidad, parent: node, key: 'cantidad' }"></ng-container>
                  } @else {
                    <div class="sig-empty-slot">
                      <button mat-stroked-button (click)="setSlot(node, 'cantidad', createAggregate())">+ Agregado</button>
                      <button mat-stroked-button (click)="setSlot(node, 'cantidad', createNumber())">+ Número</button>
                    </div>
                  }
                </div>
                <div class="sig-filters-section">
                  <strong style="font-size: 13px;">Tramos (precio por unidad acumulado)</strong>
                  @for (t of node.tramos; track $index; let i = $index) {
                    <div class="sig-filter-row">
                      <mat-form-field appearance="outline" class="sig-filter-small">
                        <mat-label>Hasta (vacío = resto)</mat-label>
                        <input matInput type="number" [(ngModel)]="t.hasta" />
                      </mat-form-field>
                      <mat-form-field appearance="outline" class="sig-filter-small">
                        <mat-label>Precio/unidad</mat-label>
                        <input matInput type="number" [(ngModel)]="t.precio" />
                      </mat-form-field>
                      <button mat-icon-button (click)="removeTramo(node, i)" aria-label="Quitar tramo"><mat-icon>close</mat-icon></button>
                    </div>
                  }
                  <button mat-stroked-button (click)="addTramo(node)" data-testid="btn-add-tramo"><mat-icon>add</mat-icon> Añadir tramo</button>
                </div>
              </div>
            }
            <!-- ConceptRef -->
            @if (node.type === 'ConceptRef') {
              <div class="sig-source">
                <p style="margin: 0; font-size: 13px; color: var(--mat-sys-on-surface-variant);">
                  Suma de los importes de otros conceptos del mismo cierre. Déjalo vacío para sumar <strong>todos</strong> los conceptos base.
                </p>
                <mat-form-field appearance="outline" style="width: 100%;">
                  <mat-label>IDs de concepto (separados por comas, opcional)</mat-label>
                  <input matInput [ngModel]="conceptIdsText(node)" (ngModelChange)="setConceptIds(node, $event)" placeholder="p.ej. 3, 5, 7" data-testid="input-conceptref-ids" />
                </mat-form-field>
              </div>
            }
            <!-- TarifaRef -->
            @if (node.type === 'TarifaRef') {
              <div class="sig-source">
                <p style="margin: 0; font-size: 13px; color: var(--mat-sys-on-surface-variant);">
                  Usar una tarifa definida. Especifica el nivel de aplicación (Global/Cliente/Servicio).
                </p>
                <mat-form-field appearance="outline" class="sig-op-field">
                  <mat-label>Nivel de tarifa</mat-label>
                  <mat-select [(ngModel)]="node.nivel" data-testid="select-tarifa-nivel">
                    <mat-option value="Global">Global (para todos)</mat-option>
                    <mat-option value="Cliente">Por cliente</mat-option>
                    <mat-option value="Servicio">Por servicio</mat-option>
                  </mat-select>
                </mat-form-field>
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
    .sig-receta-intro { color: var(--mat-sys-on-surface-variant); font-size: 13px; margin: 0 0 12px; }
    .sig-receta-row { display: flex; gap: 12px; align-items: flex-start; flex-wrap: wrap; }
    .sig-receta-tipo { min-width: 280px; } .sig-receta-valor { width: 180px; } .sig-receta-filtro { width: 200px; }
    .sig-receta-row button { margin-top: 6px; }
    .sig-receta-hint { display: flex; align-items: center; gap: 6px; color: var(--mat-sys-on-surface-variant); font-size: 13px; margin: 4px 0 0; }
    .sig-receta-hint mat-icon { font-size: 18px; width: 18px; height: 18px; }
    .sig-expression { font-family: 'Roboto Mono', monospace; font-size: 15px; padding: 12px; background: var(--mat-sys-surface-variant); color: var(--mat-sys-on-surface); border-radius: 8px; }
    .sig-valid { color: var(--sig-success); display: flex; align-items: center; gap: 6px; font-size: 14px; }
    .sig-invalid { color: var(--sig-warning); display: flex; align-items: center; gap: 6px; font-size: 14px; }
    .sig-node { border: 2px solid var(--mat-sys-outline-variant); border-radius: 8px; padding: 12px; margin: 8px 0; background: var(--mat-sys-surface); color: var(--mat-sys-on-surface); }
    .sig-node--Number { border-color: var(--sig-blue); }
    .sig-node--Variable { border-color: var(--sig-teal); }
    .sig-node--BinaryOp { border-color: #f59e0b; }
    .sig-node--Source { border-color: var(--sig-success); }
    .sig-node--Aggregate { border-color: var(--sig-warning); }
    .sig-node--TarifaRef { border-color: #8b5cf6; }
    .sig-node--ConceptRef { border-color: #06b6d4; }
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
    /* Preview cálculo aplicado */
    .sig-preview-head { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
    .sig-preview-ctx { font-weight: 400; text-transform: none; letter-spacing: 0; color: var(--mat-sys-on-surface-variant); }
    .sig-preview-demo { font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-warning); background: color-mix(in srgb, var(--sig-warning) 14%, transparent); border: 1px solid color-mix(in srgb, var(--sig-warning) 35%, transparent); padding: 2px 8px; border-radius: 10px; }
    .sig-preview-grid { display: flex; align-items: center; gap: 20px; flex-wrap: wrap; margin-top: 10px; padding: 14px 16px; background: var(--mat-sys-surface-variant); border-radius: 10px; }
    .sig-preview-amount { font-size: 30px; font-weight: 800; color: var(--sig-success); line-height: 1; }
    .sig-preview-trace { font-size: 13px; color: var(--mat-sys-on-surface); display: flex; flex-direction: column; gap: 4px; }
    .sig-preview-sep { opacity: .5; margin: 0 4px; }
    .sig-preview-muted { color: var(--mat-sys-on-surface-variant); }
    .sig-jerarquia { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
    .sig-jer-chip { font-size: 13px; font-weight: 600; padding: 5px 14px; border-radius: 8px; background: color-mix(in srgb, var(--sig-blue) 12%, transparent); color: var(--sig-blue); border: 1px solid color-mix(in srgb, var(--sig-blue) 30%, transparent); }
    .sig-jer-arrow { color: var(--mat-sys-on-surface-variant); font-size: 18px; width: 18px; height: 18px; }
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
  protected createAggregate(): FormulaNode { return { type: 'Aggregate', op: 'Sum', source: this.createSource(), field: null, distinct: null }; }
  protected createSource(): FormulaNode { return { type: 'Source', entity: 'VisitasCelero', field: null, filters: [] }; }
  protected createModifier(): FormulaNode { return { type: 'Modifier', kind: 'Min', threshold: 0, inner: this.createAggregate() }; }
  protected createTramos(): FormulaNode { return { type: 'Tramos', cantidad: this.createAggregate(), tramos: [{ hasta: 1, precio: 0 }, { hasta: null, precio: 0 }] }; }
  protected createConceptRef(): FormulaNode { return { type: 'ConceptRef', conceptIds: [] }; }
  protected createTarifaRef(): FormulaNode { return { type: 'TarifaRef', nivel: 'Global' }; }

  // ---------- Modo guiado (plantillas / "tipos de concepto" del catálogo SIG) ----------
  protected recetaSel: string | null = null;
  protected recetaValor: number | null = null;
  protected recetaFiltroCampo = '';
  protected recetaFiltroValor = '';
  protected recetaNivel: 'Global' | 'Cliente' | 'Servicio' = 'Servicio';
  protected recetaVariableId: number | null = null;
  protected recetaBase: 'gastos' | 'horas' | 'km' | 'visitas' = 'gastos';

  protected readonly recetas: { id: string; label: string; valorLabel?: string | null; filtro?: boolean; nivel?: boolean; variable?: boolean; base?: boolean; hint: string }[] = [
    { id: 'cuota_fija',     label: 'Cuota fija mensual',                    valorLabel: 'Importe (€/mes)',         filtro: false, hint: 'Un importe fijo cada mes.' },
    { id: 'por_visita',     label: 'Pago por visita (nº visitas × tarifa)', valorLabel: 'Tarifa por visita (€)',   filtro: true,  hint: 'Cuenta las visitas (con filtro opcional) y las multiplica por la tarifa.' },
    { id: 'por_dia',        label: 'Pago por día trabajado',                valorLabel: 'Importe por día (€)',     filtro: false, hint: 'Cuenta los días con actividad (fechas únicas) y los multiplica por el importe.' },
    { id: 'kilometraje',    label: 'Kilometraje (km × coste)',              valorLabel: 'Coste por km (€)',        filtro: false, hint: 'Suma los km y los multiplica por el coste por km.' },
    { id: 'gastos',         label: 'Gastos (suma de importes)',             valorLabel: null,                      filtro: true,  hint: 'Suma los importes de gastos (con filtro opcional).' },
    { id: 'por_horas',      label: 'Pago por horas (horas × tarifa)',       valorLabel: 'Tarifa por hora (€)',     filtro: false, hint: 'Suma las horas trabajadas y las multiplica por la tarifa por hora.' },
    { id: 'fee_conceptos',  label: 'Fee % sobre otros conceptos',           valorLabel: 'Porcentaje (%)',          filtro: false, hint: 'Aplica un porcentaje sobre la suma de los demás conceptos del cierre.' },
    { id: 'tarifa_config',  label: 'Tarifa configurada',                    nivel: true,                           hint: 'Usa la tarifa ya configurada (global, por cliente o por servicio) en vez de teclear el importe.' },
    { id: 'valor_variable', label: 'Valor de una variable',                 variable: true,                        hint: 'Usa el valor de una variable (p. ej. una pregunta de Celero).' },
    { id: 'pct_cantidad',   label: '% de una cantidad',                     valorLabel: 'Porcentaje (%)', base: true, hint: 'Un porcentaje sobre una cantidad (gastos, horas, km o nº de visitas).' },
  ];

  protected recetaActual(): { id: string; label: string; valorLabel?: string | null; filtro?: boolean; nivel?: boolean; variable?: boolean; base?: boolean; hint: string } | null {
    return this.recetas.find((r) => r.id === this.recetaSel) ?? null;
  }

  // Cantidad base para "% de una cantidad" (clave amigable → nodo Aggregate).
  private baseCantidad(key: 'gastos' | 'horas' | 'km' | 'visitas'): FormulaNode {
    switch (key) {
      case 'horas':   return { type: 'Aggregate', op: 'Sum',   source: { type: 'Source', entity: 'HorasBizneo',   field: null, filters: [] }, field: 'Horas',   distinct: null };
      case 'km':      return { type: 'Aggregate', op: 'Sum',   source: { type: 'Source', entity: 'GastosPayHawk', field: null, filters: [] }, field: 'Km',      distinct: null };
      case 'visitas': return { type: 'Aggregate', op: 'Count', source: { type: 'Source', entity: 'VisitasCelero', field: null, filters: [] }, field: null,      distinct: null };
      default:        return { type: 'Aggregate', op: 'Sum',   source: { type: 'Source', entity: 'GastosPayHawk', field: null, filters: [] }, field: 'Importe', distinct: null };
    }
  }

  protected generarReceta(): void {
    if (!this.recetaSel) return;
    const v = Number(this.recetaValor) || 0;
    const filtros: FormulaFilter[] = (this.recetaFiltroCampo.trim() && String(this.recetaFiltroValor ?? '').trim())
      ? [{ field: this.recetaFiltroCampo.trim(), op: 'Eq', value: String(this.recetaFiltroValor).trim() }]
      : [];
    const visitas = (f: FormulaFilter[]): FormulaNode => ({ type: 'Source', entity: 'VisitasCelero', field: null, filters: f });
    let node: FormulaNode;
    switch (this.recetaSel) {
      case 'cuota_fija':
        node = { type: 'Number', value: v }; break;
      case 'por_visita':
        node = { type: 'BinaryOp', op: 'Mul',
          left: { type: 'Aggregate', op: 'Count', source: visitas(filtros), field: null, distinct: null },
          right: { type: 'Number', value: v } }; break;
      case 'por_dia':
        node = { type: 'BinaryOp', op: 'Mul',
          left: { type: 'Aggregate', op: 'Count', source: visitas([]), field: null, distinct: 'Fecha' },
          right: { type: 'Number', value: v } }; break;
      case 'kilometraje':
        node = { type: 'BinaryOp', op: 'Mul',
          left: { type: 'Aggregate', op: 'Sum', source: { type: 'Source', entity: 'GastosPayHawk', field: null, filters: [] }, field: 'Km', distinct: null },
          right: { type: 'Number', value: v } }; break;
      case 'gastos':
        node = { type: 'Aggregate', op: 'Sum', source: { type: 'Source', entity: 'GastosPayHawk', field: null, filters: filtros }, field: 'Importe', distinct: null }; break;
      case 'por_horas':
        node = { type: 'BinaryOp', op: 'Mul',
          left: { type: 'Aggregate', op: 'Sum', source: { type: 'Source', entity: 'HorasBizneo', field: null, filters: [] }, field: 'Horas', distinct: null },
          right: { type: 'Number', value: v } }; break;
      case 'fee_conceptos':
        node = { type: 'BinaryOp', op: 'Pct', left: { type: 'Number', value: v }, right: { type: 'ConceptRef', conceptIds: [] } }; break;
      case 'tarifa_config':
        node = { type: 'TarifaRef', nivel: this.recetaNivel }; break;
      case 'valor_variable':
        node = { type: 'Variable', variableId: this.recetaVariableId ?? (this.variables()[0]?.id ?? 0) }; break;
      case 'pct_cantidad':
        node = { type: 'BinaryOp', op: 'Pct', left: { type: 'Number', value: v }, right: this.baseCantidad(this.recetaBase) }; break;
      default:
        return;
    }
    this.setRoot(node);
    this.notify.success('Fórmula generada. Puedes ajustarla abajo en el modo avanzado.');
  }

  // ---------- Mutations ----------
  protected setRoot(node: FormulaNode): void { this.root.set(node); this.bump(); }
  protected setSlot(parent: FormulaNode, key: 'left' | 'right' | 'source' | 'inner' | 'cantidad', node: FormulaNode): void {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (parent as any)[key] = node;
    this.bump();
  }
  protected addTramo(node: FormulaNode): void {
    if (node.type !== 'Tramos') return;
    node.tramos.push({ hasta: null, precio: 0 });
    this.bump();
  }
  protected removeTramo(node: FormulaNode, idx: number): void {
    if (node.type !== 'Tramos') return;
    node.tramos.splice(idx, 1);
    this.bump();
  }
  protected conceptIdsText(node: FormulaNode): string {
    return node.type === 'ConceptRef' ? node.conceptIds.join(', ') : '';
  }
  protected setConceptIds(node: FormulaNode, text: string): void {
    if (node.type !== 'ConceptRef') return;
    node.conceptIds = text.split(',').map((s) => Number(s.trim())).filter((n) => Number.isFinite(n) && n > 0);
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
    return { Number: 'numbers', Variable: 'data_object', BinaryOp: 'functions', Aggregate: 'calculate', Source: 'storage', Modifier: 'tune', Tramos: 'stairs', ConceptRef: 'percent', TarifaRef: 'local_offer' }[type] ?? 'help';
  }
  protected labelFor(type: string): string {
    return { Number: 'Número', Variable: 'Variable', BinaryOp: 'Operación', Aggregate: 'Agregado', Source: 'Entidad', Modifier: 'Modificador', Tramos: 'Tramos', ConceptRef: 'Fee s/conceptos', TarifaRef: 'Tarifa' }[type] ?? type;
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
        const distinct = node.distinct ? ` distinto por ${node.distinct}` : '';
        return `${opLabel}(${inner})${field}${distinct}`;
      }
      case 'BinaryOp': {
        const opSym = { Add: '+', Sub: '−', Mul: '×', Div: '÷', Pct: '%' }[node.op];
        const l = node.left ? this.serializeExpression(node.left) : '(?)';
        const r = node.right ? this.serializeExpression(node.right) : '(?)';
        return `(${l} ${opSym} ${r})`;
      }
      case 'Modifier': {
        const kindLabel = { Min: 'mín', Max: 'máx', FloorZero: 'umbral', Franquicia: 'franquicia' }[node.kind];
        const inner = node.inner ? this.serializeExpression(node.inner) : '(?)';
        return `${kindLabel}[${node.threshold}](${inner})`;
      }
      case 'Tramos': {
        const cant = node.cantidad ? this.serializeExpression(node.cantidad) : '(?)';
        const tramos = node.tramos.map((t) => `${t.hasta ?? '∞'}:${t.precio}`).join(', ');
        return `Tramos(${cant}; ${tramos})`;
      }
      case 'ConceptRef':
        return node.conceptIds.length > 0 ? `Conceptos(${node.conceptIds.join(', ')})` : 'Conceptos(todos)';
      case 'TarifaRef':
        return `Tarifa(${node.nivel})`;
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
      case 'Modifier': {
        if (!node.kind) return { ok: false, reason: 'Tipo de modificador sin seleccionar' };
        if (!Number.isFinite(node.threshold)) return { ok: false, reason: 'Valor X inválido' };
        return this.validateNode(node.inner);
      }
      case 'Tramos': {
        if (!node.tramos || node.tramos.length === 0) return { ok: false, reason: 'Define al menos un tramo' };
        if (node.tramos.some((t) => !Number.isFinite(t.precio))) return { ok: false, reason: 'Precio de tramo inválido' };
        return this.validateNode(node.cantidad);
      }
      case 'ConceptRef':
        return { ok: true, reason: '' };
      case 'TarifaRef':
        return node.nivel ? { ok: true, reason: '' } : { ok: false, reason: 'Nivel de tarifa sin seleccionar' };
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
      serviceId: c.serviceId, userIds: c.userIds,
    };
    this.conceptSvc.update(c.id, payload).subscribe({
      next: () => { this.saving.set(false); this.notify.success('Fórmula guardada'); void this.router.navigate(['/concepts', c.id]); },
      error: (err) => { this.saving.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar la fórmula'); },
    });
  }
}
