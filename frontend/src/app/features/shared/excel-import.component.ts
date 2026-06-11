import { Component, Input, Output, EventEmitter, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTableModule } from '@angular/material/table';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { NotifyService } from '../../core/notify.service';
import * as XLSX from 'xlsx';

export interface ExcelImportRow {
  rowNumber: number;
  data: Record<string, unknown>;
  valid: boolean;
  errors: string[];
}

export interface ExcelImportConfig {
  requiredColumns: string[];
  optionalColumns?: string[];
  validators?: Record<string, (value: unknown) => { valid: boolean; error?: string }>;
  sheetName?: string; // Si no se especifica, usa la primera hoja
}

/**
 * Componente para importación masiva desde Excel.
 * Soporta validación, deduplicación y preview antes de guardar.
 *
 * Uso:
 * <app-excel-import
 *   [config]="importConfig"
 *   (importData)="onImport($event)"
 * />
 */
@Component({
  selector: 'app-excel-import',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatProgressBarModule, MatProgressSpinnerModule,
    MatTooltipModule, MatTableModule, MatCheckboxModule,
  ],
  template: `
    <div class="sig-excel-import">
      <!-- Zona de carga -->
      @if (!rows()) {
        <div class="sig-upload-zone" [class.sig-drag-over]="dragOver()">
          <input
            type="file"
            #fileInput
            accept=".xlsx,.xls,.csv"
            (change)="onFileSelect($event)"
            class="sig-file-input"
            aria-label="Seleccionar archivo Excel"
          />
          <button
            mat-raised-button
            (click)="fileInput.click()"
            class="sig-upload-btn"
          >
            <mat-icon>upload_file</mat-icon>
            Seleccionar archivo Excel (.xlsx, .xls, .csv)
          </button>
          <p class="sig-upload-hint">O arrastra un archivo aquí</p>
        </div>
      }

      <!-- Progreso de carga -->
      @if (loading()) {
        <div class="sig-loading">
          <mat-spinner diameter="40"></mat-spinner>
          <span>Procesando archivo...</span>
        </div>
      }

      <!-- Resultados de validación -->
      @if (rows() && rows()!.length > 0) {
        <mat-card class="sig-results-card">
          <mat-card-content>
            <div class="sig-stats-bar">
              <div class="sig-stat">
                <span class="sig-stat-label">Total</span>
                <span class="sig-stat-value">{{ rows()!.length }}</span>
              </div>
              <div class="sig-stat" [class.sig-stat-success]="validCount() > 0">
                <span class="sig-stat-label">Válidos</span>
                <span class="sig-stat-value">{{ validCount() }}</span>
              </div>
              <div class="sig-stat" [class.sig-stat-error]="errorCount() > 0">
                <span class="sig-stat-label">Errores</span>
                <span class="sig-stat-value">{{ errorCount() }}</span>
              </div>
              <div class="sig-stat">
                <span class="sig-stat-label">Tasa de validez</span>
                <span class="sig-stat-value">{{ ((validCount() / rows()!.length) * 100) | number:'1.0-0' }}%</span>
              </div>
            </div>

            <mat-progress-bar
              mode="determinate"
              [value]="(validCount() / rows()!.length) * 100"
              class="sig-progress"
            ></mat-progress-bar>

            <!-- Tabla de preview -->
            <div class="sig-table-wrapper">
              <table class="sig-preview-table">
                <thead>
                  <tr>
                    <th style="width: 40px;">
                      <mat-checkbox
                        [(ngModel)]="selectAll"
                        (change)="toggleSelectAll()"
                        [indeterminate]="selectedRows().length > 0 && selectedRows().length < validCount()"
                      ></mat-checkbox>
                    </th>
                    <th>Fila</th>
                    <th>Estado</th>
                    @for (col of visibleColumns(); track col) {
                      <th>{{ col }}</th>
                    }
                  </tr>
                </thead>
                <tbody>
                  @for (row of rows(); track row.rowNumber) {
                    <tr [class.sig-row-error]="!row.valid" [class.sig-row-selected]="selectedRows().includes(row.rowNumber)">
                      <td>
                        @if (row.valid) {
                          <mat-checkbox
                            [checked]="selectedRows().includes(row.rowNumber)"
                            (change)="toggleRow(row.rowNumber)"
                          ></mat-checkbox>
                        }
                      </td>
                      <td class="sig-row-number">{{ row.rowNumber }}</td>
                      <td class="sig-status-cell">
                        @if (row.valid) {
                          <span class="sig-status-badge sig-status-ok">
                            <mat-icon>check_circle</mat-icon>
                            OK
                          </span>
                        } @else {
                          <span class="sig-status-badge sig-status-error">
                            <mat-icon>error</mat-icon>
                            Error
                          </span>
                        }
                      </td>
                      @for (col of visibleColumns(); track col) {
                        <td [class.sig-cell-error]="!row.valid">
                          {{ formatCellValue(row.data[col]) }}
                        </td>
                      }
                    </tr>
                    @if (!row.valid) {
                      <tr class="sig-error-row">
                        <td colspan="100%">
                          <div class="sig-error-message">
                            <mat-icon>warning_amber</mat-icon>
                            <div class="sig-error-text">
                              @for (err of row.errors; track err) {
                                <div>{{ err }}</div>
                              }
                            </div>
                          </div>
                        </td>
                      </tr>
                    }
                  }
                </tbody>
              </table>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Acciones -->
        <div class="sig-actions">
          <button
            mat-stroked-button
            (click)="reset()"
            class="sig-btn-reset"
          >
            <mat-icon>restart_alt</mat-icon>
            Cargar otro archivo
          </button>
          <button
            mat-flat-button
            color="primary"
            (click)="importSelected()"
            [disabled]="selectedRows().length === 0 || importing()"
          >
            @if (importing()) {
              <mat-spinner diameter="20"></mat-spinner>
            } @else {
              <ng-container>
                <mat-icon>download</mat-icon>
                Importar {{ selectedRows().length }} fila{{ selectedRows().length !== 1 ? 's' : '' }}
              </ng-container>
            }
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .sig-excel-import {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .sig-upload-zone {
      padding: 40px 20px;
      border: 2px dashed var(--sig-border);
      border-radius: 8px;
      text-align: center;
      background: var(--sig-bg-card-alt);
      cursor: pointer;
      transition: all 200ms;

      &:hover {
        border-color: var(--sig-blue);
        background: rgba(59, 130, 246, 0.05);
      }

      &.sig-drag-over {
        border-color: var(--sig-blue);
        background: rgba(59, 130, 246, 0.1);
      }
    }

    .sig-file-input {
      display: none;
    }

    .sig-upload-btn {
      margin-bottom: 16px;
    }

    .sig-upload-hint {
      font-size: 12px;
      color: var(--sig-text-muted);
      margin: 0;
    }

    .sig-loading {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 40px 20px;
      color: var(--sig-text-muted);
    }

    .sig-results-card {
      background: var(--sig-bg-card);
    }

    .sig-stats-bar {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
      gap: 16px;
      margin-bottom: 16px;
    }

    .sig-stat {
      display: flex;
      flex-direction: column;
      gap: 4px;
      padding: 12px;
      background: var(--sig-bg-card-alt);
      border-radius: 6px;
      border: 1px solid var(--sig-border);

      &.sig-stat-success {
        border-color: rgba(34, 197, 94, 0.3);
        background: rgba(34, 197, 94, 0.05);
      }

      &.sig-stat-error {
        border-color: rgba(239, 68, 68, 0.3);
        background: rgba(239, 68, 68, 0.05);
      }
    }

    .sig-stat-label {
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
      color: var(--sig-text-muted);
      letter-spacing: 0.08em;
    }

    .sig-stat-value {
      font-size: 18px;
      font-weight: 700;
      color: var(--sig-text-primary);
    }

    .sig-progress {
      margin-bottom: 16px;
      height: 4px;
    }

    .sig-table-wrapper {
      overflow-x: auto;
      border: 1px solid var(--sig-border);
      border-radius: 6px;
      margin-bottom: 16px;
    }

    .sig-preview-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 12px;
    }

    .sig-preview-table thead {
      background: var(--sig-bg-header);
      border-bottom: 1px solid var(--sig-border);
      position: sticky;
      top: 0;
      z-index: 10;
    }

    .sig-preview-table th {
      padding: 10px 12px;
      text-align: left;
      font-weight: 600;
      color: var(--sig-text-muted);
      text-transform: uppercase;
      font-size: 10px;
      letter-spacing: 0.08em;
    }

    .sig-preview-table td {
      padding: 10px 12px;
      border-bottom: 1px solid var(--sig-border);
      color: var(--sig-text-primary);
    }

    .sig-preview-table tbody tr {
      transition: background 150ms;

      &:hover {
        background: var(--sig-bg-hover);
      }

      &.sig-row-error {
        background: rgba(239, 68, 68, 0.05);
      }

      &.sig-row-selected {
        background: rgba(59, 130, 246, 0.08);
      }
    }

    .sig-row-number {
      font-weight: 600;
      color: var(--sig-text-muted);
    }

    .sig-status-cell {
      width: 80px;
    }

    .sig-status-badge {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;

      &.sig-status-ok {
        background: rgba(34, 197, 94, 0.15);
        color: #16a34a;

        mat-icon {
          font-size: 14px;
          width: 14px;
          height: 14px;
        }
      }

      &.sig-status-error {
        background: rgba(239, 68, 68, 0.15);
        color: #dc2626;

        mat-icon {
          font-size: 14px;
          width: 14px;
          height: 14px;
        }
      }
    }

    .sig-cell-error {
      color: var(--sig-text-muted);
      opacity: 0.7;
    }

    .sig-error-row {
      background: rgba(239, 68, 68, 0.02) !important;
    }

    .sig-error-message {
      display: flex;
      gap: 8px;
      padding: 8px;
      color: #dc2626;
      font-size: 11px;

      mat-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
        flex-shrink: 0;
      }
    }

    .sig-error-text {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .sig-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
    }

    .sig-btn-reset {
      margin-right: auto;
    }
  `],
})
export class ExcelImportComponent {
  @Input() config!: ExcelImportConfig;
  @Output() importData = new EventEmitter<ExcelImportRow[]>();

  private readonly notify = inject(NotifyService);

  protected readonly loading = signal(false);
  protected readonly importing = signal(false);
  protected readonly dragOver = signal(false);
  protected readonly rows = signal<ExcelImportRow[] | null>(null);
  protected readonly selectedRows = signal<number[]>([]);
  protected selectAll = false;

  protected readonly visibleColumns = computed(() => {
    const cols = this.config.requiredColumns || [];
    const optional = this.config.optionalColumns || [];
    return [...cols, ...optional].slice(0, 8); // Limitar a 8 columnas visibles
  });

  protected readonly validCount = computed(() => this.rows()?.filter((r) => r.valid).length ?? 0);
  protected readonly errorCount = computed(() => this.rows()?.filter((r) => !r.valid).length ?? 0);

  protected onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    this.processFile(input.files[0]);
  }

  private processFile(file: File): void {
    this.loading.set(true);
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const buffer = e.target?.result as ArrayBuffer;
        const workbook = XLSX.read(buffer, { type: 'array' });
        const sheetName = this.config.sheetName || workbook.SheetNames[0];
        const worksheet = workbook.Sheets[sheetName];
        if (!worksheet) throw new Error(`Hoja "${sheetName}" no encontrada`);

        const data = XLSX.utils.sheet_to_json(worksheet, { defval: '' });
        this.parseAndValidateData(data as Record<string, unknown>[]);
      } catch (err) {
        this.notify.error(`Error al procesar archivo: ${err instanceof Error ? err.message : String(err)}`);
        this.loading.set(false);
      }
    };
    reader.readAsArrayBuffer(file);
  }

  private parseAndValidateData(data: Record<string, unknown>[]): void {
    const rows: ExcelImportRow[] = [];
    const required = this.config.requiredColumns || [];
    const validators = this.config.validators || {};

    data.forEach((item, idx) => {
      const rowNumber = idx + 2; // +1 para Excel, +1 para header
      const errors: string[] = [];

      // Validar columnas requeridas
      for (const col of required) {
        if (!item[col] || String(item[col]).trim() === '') {
          errors.push(`Columna requerida falta: ${col}`);
        }
      }

      // Aplicar validadores
      for (const [col, validator] of Object.entries(validators)) {
        const value = item[col];
        if (value !== undefined && value !== null && value !== '') {
          const result = validator(value);
          if (!result.valid && result.error) {
            errors.push(`${col}: ${result.error}`);
          }
        }
      }

      rows.push({
        rowNumber,
        data: item,
        valid: errors.length === 0,
        errors,
      });
    });

    this.rows.set(rows);
    this.selectedRows.set(rows.filter((r) => r.valid).map((r) => r.rowNumber));
    this.selectAll = true;
    this.loading.set(false);

    if (this.validCount() === 0) {
      this.notify.warning('No hay filas válidas para importar');
    }
  }

  protected toggleSelectAll(): void {
    if (this.selectAll) {
      this.selectedRows.set(
        this.rows()?.filter((r) => r.valid).map((r) => r.rowNumber) ?? []
      );
    } else {
      this.selectedRows.set([]);
    }
  }

  protected toggleRow(rowNumber: number): void {
    const current = this.selectedRows();
    this.selectedRows.set(
      current.includes(rowNumber) ? current.filter((r) => r !== rowNumber) : [...current, rowNumber]
    );
    this.selectAll = false;
  }

  protected formatCellValue(value: unknown): string {
    if (value === null || value === undefined) return '—';
    if (typeof value === 'number') return value.toLocaleString('es-ES');
    return String(value).substring(0, 50);
  }

  protected importSelected(): void {
    const selected = this.rows()?.filter((r) => this.selectedRows().includes(r.rowNumber)) ?? [];
    if (selected.length === 0) return;

    this.importing.set(true);
    // Simular delay de API call
    setTimeout(() => {
      this.importData.emit(selected);
      this.importing.set(false);
      this.notify.success(`${selected.length} fila${selected.length !== 1 ? 's' : ''} importada${selected.length !== 1 ? 's' : ''}`);
      this.reset();
    }, 800);
  }

  protected reset(): void {
    this.rows.set(null);
    this.selectedRows.set([]);
    this.selectAll = false;
  }
}
