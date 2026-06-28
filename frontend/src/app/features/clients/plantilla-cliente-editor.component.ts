import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ClientService } from '../../core/api/clients.service';
import { ConceptService } from '../../core/api/concepts.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { PlantillaClienteConceptoDto } from '../../models/dtos';

@Component({
  selector: 'app-plantilla-cliente-editor',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatDatepickerModule, MatNativeDateModule, MatIconModule, MatTabsModule, MatTooltipModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[
        { label: 'Inicio', route: '/dashboard' },
        { label: 'Clientes', route: '/clients' },
        { label: clientNombre() ?? '...', route: clientId() ? '/clients/' + clientId() : '/clients' },
        { label: 'Plantillas' }
      ]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">Customización de conceptos para {{ clientNombre() ?? 'cargando...' }}</h1>
      </div>

      @if (loading()) {
        <mat-card><mat-card-content><sig-skeleton [count]="4" /></mat-card-content></mat-card>
      } @else {
        <mat-card style="margin-bottom: 16px;">
          <mat-card-content>
            <p style="color: var(--mat-sys-on-surface-variant);">
              <mat-icon style="vertical-align: middle; font-size: 18px; width: 18px; height: 18px;">info</mat-icon>
              Aquí puedes personalizar fórmulas y configuración de conceptos específicos para este cliente.
              Deja los campos vacíos para usar los valores globales.
            </p>
          </mat-card-content>
        </mat-card>

        <mat-tab-group>
          <mat-tab label="Plantillas registradas">
            @if (plantillas().length > 0) {
              <mat-card style="margin-top: 16px;">
                <mat-card-content>
                  <div style="overflow-x: auto;">
                    <table style="width: 100%; border-collapse: collapse;">
                      <thead style="background: var(--mat-sys-surface-variant);">
                        <tr>
                          <th style="padding: 12px; text-align: left; font-weight: 600;">Concepto</th>
                          <th style="padding: 12px; text-align: left; font-weight: 600;">Activo</th>
                          <th style="padding: 12px; text-align: left; font-weight: 600;">Vigencia</th>
                          <th style="padding: 12px; text-align: center; font-weight: 600;">Acciones</th>
                        </tr>
                      </thead>
                      <tbody>
                        @for (p of plantillas(); track p.id) {
                          <tr style="border-bottom: 1px solid var(--mat-sys-outline-variant);">
                            <td style="padding: 12px;">{{ p.conceptNombre }}</td>
                            <td style="padding: 12px;">
                              <mat-icon [style.color]="p.activo ? 'var(--sig-success)' : 'var(--sig-warning)'">
                                {{ p.activo ? 'check_circle' : 'cancel' }}
                              </mat-icon>
                            </td>
                            <td style="padding: 12px; font-size: 12px; color: var(--mat-sys-on-surface-variant);">
                              {{ p.fechaDesde }} → {{ p.fechaHasta ?? '∞' }}
                            </td>
                            <td style="padding: 12px; text-align: center;">
                              <button mat-icon-button (click)="editPlantilla(p)" [matTooltip]="'Editar'">
                                <mat-icon>edit</mat-icon>
                              </button>
                              <button mat-icon-button (click)="deletePlantilla(p.id)" color="warn" [matTooltip]="'Eliminar'">
                                <mat-icon>delete</mat-icon>
                              </button>
                            </td>
                          </tr>
                        }
                      </tbody>
                    </table>
                  </div>
                </mat-card-content>
              </mat-card>
            } @else {
              <div style="padding: 40px; text-align: center; color: var(--mat-sys-on-surface-variant);">
                <mat-icon style="font-size: 48px; width: 48px; height: 48px; opacity: 0.5;">folder_open</mat-icon>
                <p>No hay plantillas registradas</p>
              </div>
            }
          </mat-tab>

          <mat-tab label="Nueva plantilla">
            <mat-card style="margin-top: 16px;">
              <mat-card-content>
                <form>
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Concepto</mat-label>
                    <mat-select [(ngModel)]="newForm.conceptId" name="conceptId" required>
                      @for (c of conceptos(); track c.id) {
                        <mat-option [value]="c.id">{{ c.nombre }}</mat-option>
                      }
                    </mat-select>
                  </mat-form-field>

                  <!-- FASE 5: UI Visual para Fórmula -->
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Fórmula</mat-label>
                    <mat-select [ngModel]="formulaMode()" (ngModelChange)="formulaMode.set($event)" name="formulaMode">
                      <mat-option value="usar-global">Usar fórmula global del concepto</mat-option>
                      <mat-option value="personalizar">Personalizar para este cliente</mat-option>
                    </mat-select>
                  </mat-form-field>

                  @if (formulaMode() === 'personalizar') {
                    <div style="background: var(--mat-sys-surface-variant); padding: 16px; border-radius: 8px; margin-bottom: 12px;">
                      <p style="margin: 0 0 12px 0; font-weight: 600; font-size: 13px;">Tipo de cálculo</p>

                      <mat-form-field appearance="outline" class="full-width">
                        <mat-label>Selecciona el tipo</mat-label>
                        <mat-select [ngModel]="formulaTipo()" (ngModelChange)="formulaTipo.set($event)" name="formulaTipo">
                          <mat-option [value]="null">— Ninguno —</mat-option>
                          <mat-option value="number">Número fijo</mat-option>
                          <mat-option value="variable">Variable</mat-option>
                          <mat-option value="tarifa">Tarifa</mat-option>
                          <mat-option value="agregado">Agregado (Suma)</mat-option>
                        </mat-select>
                      </mat-form-field>

                      @if (formulaTipo() === 'number') {
                        <mat-form-field appearance="outline" class="full-width">
                          <mat-label>Valor</mat-label>
                          <input matInput type="number" [ngModel]="formulaNumber()" (ngModelChange)="formulaNumber.set($event)" name="formulaNumber" step="0.01" />
                        </mat-form-field>
                      }

                      @if (formulaTipo() === 'variable') {
                        <mat-form-field appearance="outline" class="full-width">
                          <mat-label>Variable</mat-label>
                          <mat-select [ngModel]="formulaVariable()" (ngModelChange)="formulaVariable.set($event)" name="formulaVariable">
                            @for (v of variables(); track v.id) {
                              <mat-option [value]="v.id">{{ v.nombre }}</mat-option>
                            }
                          </mat-select>
                        </mat-form-field>
                      }

                      @if (formulaTipo() === 'tarifa') {
                        <mat-form-field appearance="outline" class="full-width">
                          <mat-label>Nivel de tarifa</mat-label>
                          <mat-select [ngModel]="formulaTarifaNivel()" (ngModelChange)="formulaTarifaNivel.set($event)" name="formulaTarifaNivel">
                            <mat-option value="Global">Global (para todos)</mat-option>
                            <mat-option value="Cliente">Por cliente</mat-option>
                            <mat-option value="Servicio">Por servicio</mat-option>
                          </mat-select>
                        </mat-form-field>
                      }

                      @if (formulaTipo() === 'agregado') {
                        <mat-form-field appearance="outline" class="full-width">
                          <mat-label>Tipo de agregado</mat-label>
                          <mat-select [ngModel]="formulaAgregado()" (ngModelChange)="formulaAgregado.set($event)" name="formulaAgregado">
                            <mat-option value="Sum">Suma</mat-option>
                            <mat-option value="Count">Cuenta</mat-option>
                            <mat-option value="Min">Mínimo</mat-option>
                            <mat-option value="Max">Máximo</mat-option>
                          </mat-select>
                        </mat-form-field>
                      }
                    </div>
                  }

                  <!-- FASE 5: UI Visual para Configuración -->
                  <div style="background: var(--mat-sys-surface-variant); padding: 16px; border-radius: 8px; margin-bottom: 12px;">
                    <p style="margin: 0 0 12px 0; font-weight: 600; font-size: 13px;">Configuración (opcional)</p>

                    <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 12px;">
                      <input type="checkbox" [checked]="configHabilitarMargen()" (change)="configHabilitarMargen.set($event.target.checked)" name="configHabilitarMargen" />
                      <label>Margen mínimo (%)</label>
                    </div>
                    @if (configHabilitarMargen()) {
                      <mat-form-field appearance="outline" class="full-width">
                        <mat-label>Margen mínimo</mat-label>
                        <input matInput type="number" [ngModel]="configMargenMinimo()" (ngModelChange)="configMargenMinimo.set($event)" name="configMargenMinimo" step="0.1" min="0" />
                      </mat-form-field>
                    }

                    <div style="display: flex; align-items: center; gap: 8px; margin-top: 12px; margin-bottom: 12px;">
                      <input type="checkbox" [checked]="configHabilitarDescuento()" (change)="configHabilitarDescuento.set($event.target.checked)" name="configHabilitarDescuento" />
                      <label>Descuento máximo (%)</label>
                    </div>
                    @if (configHabilitarDescuento()) {
                      <mat-form-field appearance="outline" class="full-width">
                        <mat-label>Descuento máximo</mat-label>
                        <input matInput type="number" [ngModel]="configDescuentoMaximo()" (ngModelChange)="configDescuentoMaximo.set($event)" name="configDescuentoMaximo" step="0.1" min="0" />
                      </mat-form-field>
                    }
                  </div>

                  <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px;">
                    <mat-form-field appearance="outline">
                      <mat-label>Vigente desde</mat-label>
                      <input matInput [matDatepicker]="dpDesde" [(ngModel)]="newForm.fechaDesde" name="fechaDesde" required>
                      <mat-datepicker-toggle matSuffix [for]="dpDesde"></mat-datepicker-toggle>
                      <mat-datepicker #dpDesde></mat-datepicker>
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                      <mat-label>Vigente hasta (opcional)</mat-label>
                      <input matInput [matDatepicker]="dpHasta" [(ngModel)]="newForm.fechaHasta" name="fechaHasta">
                      <mat-datepicker-toggle matSuffix [for]="dpHasta"></mat-datepicker-toggle>
                      <mat-datepicker #dpHasta></mat-datepicker>
                    </mat-form-field>
                  </div>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Activo</mat-label>
                    <mat-select [(ngModel)]="newForm.activo" name="activo">
                      <mat-option [value]="true">Sí</mat-option>
                      <mat-option [value]="false">No</mat-option>
                    </mat-select>
                  </mat-form-field>

                  @if (error()) {
                    <div style="color: var(--sig-warning); font-size: 14px; margin: 12px 0; padding: 8px; background: color-mix(in srgb, var(--sig-warning) 14%, transparent); border-radius: 4px;">
                      <mat-icon style="font-size: 16px; width: 16px; height: 16px; vertical-align: middle;">error</mat-icon>
                      {{ error() }}
                    </div>
                  }

                  <div style="display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px;">
                    <button mat-stroked-button type="button" (click)="resetForm()">Limpiar</button>
                    <button mat-flat-button color="primary" type="button" (click)="savePlantilla()" [disabled]="saving()">
                      <mat-icon>save</mat-icon> Guardar
                    </button>
                  </div>
                </form>
              </mat-card-content>
            </mat-card>
          </mat-tab>
        </mat-tab-group>
      }
    </div>
  `,
  styles: [`
    .full-width { width: 100%; margin-bottom: 12px; }
  `],
})
export class PlantillaClienteEditorComponent implements OnInit {
  private readonly clientSvc = inject(ClientService);
  private readonly conceptSvc = inject(ConceptService);
  private readonly notify = inject(NotifyService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly clientId = signal<number | null>(null);
  protected readonly clientNombre = signal('');
  protected readonly plantillas = signal<PlantillaClienteConceptoDto[]>([]);
  protected readonly conceptos = signal<any[]>([]);
  protected readonly variables = signal<any[]>([]);

  protected newForm = {
    conceptId: 0,
    formulaJsonOverride: '',
    configuracionJson: '',
    fechaDesde: new Date().toISOString().split('T')[0],
    fechaHasta: null as string | null,
    activo: true,
  };

  // FASE 5: UI amigable - Configuración visual
  protected formulaMode = signal<'usar-global' | 'personalizar'>('usar-global');
  protected formulaTipo = signal<'number' | 'variable' | 'tarifa' | 'agregado' | null>(null);
  protected formulaNumber = signal(0);
  protected formulaVariable = signal(0);
  protected formulaTarifaNivel = signal<'Global' | 'Cliente' | 'Servicio'>('Global');
  protected formulaAgregado = signal<'Sum' | 'Count' | 'Min' | 'Max'>('Sum');

  protected configMargenMinimo = signal(0);
  protected configDescuentoMaximo = signal(0);
  protected configHabilitarMargen = signal(false);
  protected configHabilitarDescuento = signal(false);

  ngOnInit(): void {
    const clientId = Number(this.route.snapshot.paramMap.get('clientId'));
    this.clientId.set(clientId);
    this.loadClient();
    this.loadPlantillas();
    this.loadConceptos();
    this.loadVariables();
  }

  private loadClient(): void {
    const id = this.clientId();
    if (!id) return;
    this.clientSvc.getById(id).subscribe({
      next: (c: any) => this.clientNombre.set(c.nombre),
      error: () => this.notify.error('No se pudo cargar el cliente'),
    });
  }

  private loadPlantillas(): void {
    const id = this.clientId();
    if (!id) return;
    this.clientSvc.getPlantillasClienteConcepto(id).subscribe({
      next: (plantillas: PlantillaClienteConceptoDto[]) => {
        this.plantillas.set(plantillas);
        this.loading.set(false);
      },
      error: () => {
        this.notify.error('No se pudieron cargar las plantillas');
        this.loading.set(false);
      },
    });
  }

  private loadConceptos(): void {
    this.conceptSvc.list(1, 1000, undefined, undefined).subscribe({
      next: (result) => this.conceptos.set(result.items || []),
      error: () => this.notify.error('No se pudieron cargar los conceptos'),
    });
  }

  private loadVariables(): void {
    this.conceptSvc.getVariables().subscribe({
      next: (result: any) => this.variables.set(result.items || []),
      error: () => this.notify.error('No se pudieron cargar las variables'),
    });
  }

  // FASE 5: Generar JSON automáticamente desde UI visual
  private generarFormulaJson(): string | null {
    if (this.formulaMode() === 'usar-global') return null;

    const tipo = this.formulaTipo();
    if (!tipo) return null;

    let formula: any = {};

    switch (tipo) {
      case 'number':
        formula = { type: 'Number', value: this.formulaNumber() };
        break;
      case 'variable':
        formula = { type: 'Variable', variableId: this.formulaVariable() };
        break;
      case 'tarifa':
        formula = { type: 'TarifaRef', nivel: this.formulaTarifaNivel() };
        break;
      case 'agregado':
        formula = {
          type: 'Aggregate',
          op: this.formulaAgregado(),
          source: { type: 'Source', entity: 'VisitasCelero', filters: [] },
          field: null,
          distinct: null
        };
        break;
    }

    return JSON.stringify(formula);
  }

  private generarConfiguracionJson(): string | null {
    if (!this.configHabilitarMargen() && !this.configHabilitarDescuento()) {
      return null;
    }

    const config: any = {};
    if (this.configHabilitarMargen()) config.margenMinimo = this.configMargenMinimo();
    if (this.configHabilitarDescuento()) config.descuentoMaximo = this.configDescuentoMaximo();

    return Object.keys(config).length > 0 ? JSON.stringify(config) : null;
  }

  protected savePlantilla(): void {
    if (!this.newForm.conceptId) {
      this.error.set('Debes seleccionar un concepto');
      return;
    }
    this.saving.set(true);

    const req = {
      conceptId: this.newForm.conceptId,
      formulaJsonOverride: this.generarFormulaJson(),
      configuracionJson: this.generarConfiguracionJson(),
      activo: this.newForm.activo,
      fechaDesde: this.newForm.fechaDesde,
      fechaHasta: this.newForm.fechaHasta,
    };

    this.clientSvc.createPlantillaClienteConcepto(this.clientId()!, req).subscribe({
      next: () => {
        this.notify.success('Plantilla creada');
        this.resetForm();
        this.loadPlantillas();
        this.saving.set(false);
      },
      error: (err: any) => {
        this.error.set(err?.error?.title ?? 'Error al crear');
        this.saving.set(false);
      },
    });
  }

  protected editPlantilla(plantilla: PlantillaClienteConceptoDto): void {
    this.newForm = {
      conceptId: plantilla.conceptId,
      formulaJsonOverride: plantilla.formulaJsonOverride || '',
      configuracionJson: plantilla.configuracionJson || '',
      fechaDesde: plantilla.fechaDesde,
      fechaHasta: plantilla.fechaHasta ?? null,
      activo: plantilla.activo,
    };
  }

  protected deletePlantilla(id: number): void {
    if (!confirm('¿Eliminar esta plantilla?')) return;
    this.clientSvc.deletePlantillaClienteConcepto(this.clientId()!, id).subscribe({
      next: () => {
        this.notify.success('Plantilla eliminada');
        this.loadPlantillas();
      },
      error: (err: any) => this.notify.error('No se pudo eliminar la plantilla'),
    });
  }

  protected resetForm(): void {
    this.newForm = {
      conceptId: 0,
      formulaJsonOverride: '',
      configuracionJson: '',
      fechaDesde: new Date().toISOString().split('T')[0],
      fechaHasta: null,
      activo: true,
    };
    this.error.set('');
  }
}
