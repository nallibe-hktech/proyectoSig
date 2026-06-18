import { Component, inject, Input, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ForecastService } from '../../../core/api/forecast.service';
import { NotifyService } from '../../../core/notify.service';
import { ForecastDto } from '../../../models/dtos';

interface MesRow {
  mes: number;
  nombre: string;
  ventas: number | null;
  margen: number | null;
  personas: number | null;
  cerrado: boolean;
  saving: boolean;
}

const MESES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

@Component({
  selector: 'app-forecast-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatTableModule, MatButtonModule, MatIconModule, MatCardModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatProgressSpinnerModule, MatTooltipModule,
  ],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Forecast</mat-card-title>
        <span class="spacer"></span>
        <mat-form-field appearance="outline" class="year-select" subscriptSizing="dynamic">
          <mat-label>Año</mat-label>
          <mat-select [(ngModel)]="anio" (ngModelChange)="onYearChange()" data-testid="forecast-anio">
            @for (y of anios; track y) { <mat-option [value]="y">{{ y }}</mat-option> }
          </mat-select>
        </mat-form-field>
      </mat-card-header>
      <mat-card-content>
        <p class="hint">Previsión mensual de ventas, margen y nº de personas de campo (GPP). Los meses cerrados (anteriores al mes actual) no son editables.</p>
        @if (loading()) {
          <div class="center"><mat-spinner diameter="40"></mat-spinner></div>
        } @else {
          <table mat-table [dataSource]="rows()" class="full-width">
            <ng-container matColumnDef="mes">
              <th mat-header-cell *matHeaderCellDef>Mes</th>
              <td mat-cell *matCellDef="let r">
                {{ r.nombre }}
                @if (r.cerrado) { <mat-icon class="lock" matTooltip="Mes cerrado">lock</mat-icon> }
              </td>
            </ng-container>

            <ng-container matColumnDef="ventas">
              <th mat-header-cell *matHeaderCellDef class="text-right">Ventas (€)</th>
              <td mat-cell *matCellDef="let r" class="text-right">
                <input class="cell-input" type="number" [(ngModel)]="r.ventas" [disabled]="r.cerrado"
                       [attr.data-testid]="'ventas-' + r.mes" />
              </td>
            </ng-container>

            <ng-container matColumnDef="margen">
              <th mat-header-cell *matHeaderCellDef class="text-right">Margen (€)</th>
              <td mat-cell *matCellDef="let r" class="text-right">
                <input class="cell-input" type="number" [(ngModel)]="r.margen" [disabled]="r.cerrado"
                       [attr.data-testid]="'margen-' + r.mes" />
              </td>
            </ng-container>

            <ng-container matColumnDef="personas">
              <th mat-header-cell *matHeaderCellDef class="text-right">Nº personas</th>
              <td mat-cell *matCellDef="let r" class="text-right">
                <input class="cell-input" type="number" [(ngModel)]="r.personas" [disabled]="r.cerrado"
                       [attr.data-testid]="'personas-' + r.mes" />
              </td>
            </ng-container>

            <ng-container matColumnDef="acciones">
              <th mat-header-cell *matHeaderCellDef class="text-center" style="width: 110px;">Acción</th>
              <td mat-cell *matCellDef="let r" class="text-center">
                <button mat-stroked-button color="primary" (click)="save(r)" [disabled]="r.cerrado || r.saving"
                        [attr.data-testid]="'guardar-' + r.mes">
                  <mat-icon>save</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="cols"></tr>
            <tr mat-row *matRowDef="let r; columns: cols;" [class.row-cerrado]="r.cerrado"></tr>
          </table>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    :host { display: block; }
    mat-card-header { display: flex; align-items: center; gap: 16px; }
    mat-card-title { margin: 0; }
    .spacer { flex: 1; }
    .year-select { width: 120px; }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 13px; margin: 0 0 12px; }
    .full-width { width: 100%; }
    .text-right { text-align: right; }
    .text-center { text-align: center; }
    .cell-input { width: 110px; text-align: right; padding: 6px 8px; border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 6px; background: var(--mat-sys-surface); color: var(--mat-sys-on-surface); font-variant-numeric: tabular-nums; }
    .cell-input:disabled { opacity: .5; }
    .lock { font-size: 16px; height: 16px; width: 16px; vertical-align: middle; color: var(--mat-sys-on-surface-variant); }
    .row-cerrado { opacity: .7; }
    .center { display: flex; justify-content: center; padding: 32px; }
  `],
})
export class ForecastListComponent implements OnInit {
  @Input() serviceId!: number;
  @Input() set id(value: number | string) { this.serviceId = Number(value); }

  private readonly forecastSvc = inject(ForecastService);
  private readonly notify = inject(NotifyService);
  private readonly route = inject(ActivatedRoute);

  private readonly now = new Date();
  protected readonly anios: number[] = [this.now.getFullYear() - 1, this.now.getFullYear(), this.now.getFullYear() + 1, this.now.getFullYear() + 2];
  protected anio = this.now.getFullYear();

  protected readonly rows = signal<MesRow[]>([]);
  protected readonly loading = signal(true);
  protected readonly cols = ['mes', 'ventas', 'margen', 'personas', 'acciones'];

  ngOnInit(): void {
    if (this.serviceId == null) {
      const param = this.route.snapshot.paramMap.get('id');
      if (param) this.serviceId = Number(param);
    }
    this.load();
  }

  protected onYearChange(): void { this.load(); }

  private esMesCerrado(mes: number): boolean {
    return this.anio < this.now.getFullYear() || (this.anio === this.now.getFullYear() && mes < this.now.getMonth() + 1);
  }

  private load(): void {
    this.loading.set(true);
    this.forecastSvc.listByService(this.serviceId, this.anio).subscribe({
      next: (data) => { this.rows.set(this.buildRows(data)); this.loading.set(false); },
      error: () => { this.notify.error('No se pudo cargar el forecast'); this.rows.set(this.buildRows([])); this.loading.set(false); },
    });
  }

  private buildRows(data: ForecastDto[]): MesRow[] {
    const byMonth = new Map(data.map((f) => [f.mes, f]));
    return MESES.map((nombre, idx) => {
      const mes = idx + 1;
      const f = byMonth.get(mes);
      return {
        mes, nombre,
        ventas: f?.ventasPrevistas ?? null,
        margen: f?.margenPrevisto ?? null,
        personas: f?.personasCampo ?? null,
        cerrado: this.esMesCerrado(mes),
        saving: false,
      };
    });
  }

  protected save(r: MesRow): void {
    if (r.cerrado) return;
    r.saving = true;
    this.forecastSvc.upsert(this.serviceId, {
      anio: this.anio,
      mes: r.mes,
      ventasPrevistas: r.ventas ?? 0,
      margenPrevisto: r.margen,
      personasCampo: r.personas,
    }).subscribe({
      next: () => { r.saving = false; this.notify.success(`Forecast de ${r.nombre} guardado`); },
      error: (err) => { r.saving = false; this.notify.error(err?.error?.title ?? 'No se pudo guardar'); },
    });
  }
}
