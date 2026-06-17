import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ClosureLine, CalculationDetailDto } from '../../models/dtos';

@Component({
  selector: 'app-closure-lines-detail',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DecimalPipe, MatIconModule, MatButtonModule],
  template: `
    <div class="sig-lines-detail">
      <table class="sig-lines-table">
        <thead>
          <tr>
            <th style="width:32px"></th>
            <th>CONCEPTO</th>
            <th>TIPO</th>
            <th>EMPLEADO</th>
            <th>IMPORTE</th>
            <th>INCIDENCIAS</th>
            <th>ACCIONES</th>
          </tr>
        </thead>
        <tbody>
          @for (line of lineas; track line.id) {
            <tr [class.expanded]="expandido() === line.id">
              <td>
                <button
                  class="sig-expand-btn"
                  (click)="toggle(line.id)"
                  [class.expanded]="expandido() === line.id"
                >
                  <mat-icon>expand_more</mat-icon>
                </button>
              </td>
              <td class="sig-concepto">{{ line.conceptNombre }}</td>
              <td><span class="sig-badge sig-badge-tipo">{{ line.tipo }}</span></td>
              <td>{{ line.userNombre || '—' }}</td>
              <td class="sig-mono">€ {{ line.importe | number:'1.0-0' }}</td>
              <td>
                @if (line.tieneIncidencia) {
                  <span class="sig-incidencia-badge">
                    <mat-icon>warning_amber</mat-icon>
                    Incidencia
                  </span>
                }
              </td>
              <td>
                <button class="sig-btn-icon" title="Editar" (click)="editar(line)">
                  <mat-icon>edit</mat-icon>
                </button>
              </td>
            </tr>

            <!-- Expandable Row: Detalle de Cálculo -->
            @if (expandido() === line.id && detalle(line.id)) {
              <tr class="sig-detail-row">
                <td colspan="7">
                  <div class="sig-calc-detail">
                    <div class="sig-calc-section">
                      <h4>Fórmula</h4>
                      <code class="sig-formula">{{ detalle(line.id)?.formulaSnapshotJson || 'N/A' }}</code>
                    </div>
                    <div class="sig-calc-section">
                      <h4>Inputs</h4>
                      <div class="sig-inputs-grid">
                        @if (detalle(line.id)?.inputsJson) {
                          @for (item of parseInputs(detalle(line.id)!.inputsJson); track item.key) {
                            <div class="sig-input-item">
                              <span class="sig-input-key">{{ item.key }}</span>
                              <span class="sig-input-value">{{ item.value }}</span>
                            </div>
                          }
                        }
                      </div>
                    </div>
                    <div class="sig-calc-section">
                      <h4>Resultado</h4>
                      <div class="sig-resultado">
                        <span class="sig-resultado-valor">€ {{ detalle(line.id)?.resultado | currency }}</span>
                        <span class="sig-resultado-origen">Origen: {{ detalle(line.id)?.sistemaOrigen || '—' }}</span>
                      </div>
                    </div>
                    @if (detalle(line.id)?.incidencias) {
                      <div class="sig-calc-section sig-incidencias-section">
                        <h4>
                          <mat-icon>warning_amber</mat-icon>
                          Incidencias
                        </h4>
                        <p class="sig-incidencia-text">{{ detalle(line.id)?.incidencias }}</p>
                        <button class="sig-btn-override" (click)="abrirOverride(line)">
                          <mat-icon>edit</mat-icon>
                          Ajustar Manualmente
                        </button>
                      </div>
                    }
                    <div class="sig-calc-meta">
                      <span class="sig-meta-timestamp">{{ detalle(line.id)?.timestamp | date:'dd/MM/yyyy HH:mm' }}</span>
                    </div>
                  </div>
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    :host { display: block; }

    .sig-lines-detail { }
    .sig-lines-table {
      width: 100%;
      border-collapse: collapse;
      background: var(--sig-bg-card);
    }

    .sig-lines-table thead {
      background: var(--sig-bg-header);
      border-bottom: 1px solid var(--sig-border);
    }

    .sig-lines-table th {
      padding: 10px 12px;
      font-size: 10px;
      font-weight: 700;
      letter-spacing: .08em;
      text-transform: uppercase;
      color: var(--sig-text-muted);
      text-align: left;
    }

    .sig-lines-table td {
      padding: 12px;
      font-size: 13px;
      color: var(--sig-text-primary);
      border-bottom: 1px solid var(--sig-border);
      vertical-align: middle;
    }

    .sig-lines-table tbody tr {
      transition: background 150ms;
      &:hover { background: var(--sig-bg-hover); }
      &.expanded { background: rgba(59,130,246,.06); }
    }

    .sig-expand-btn {
      width: 32px;
      height: 32px;
      border-radius: 6px;
      border: 1px solid var(--sig-border);
      background: transparent;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--sig-text-secondary);
      transition: all 150ms;
      mat-icon { font-size: 18px; }
      &:hover { background: var(--sig-bg-card-alt); }
      &.expanded { background: var(--sig-blue); color: white; transform: rotate(180deg); }
    }

    .sig-concepto { font-weight: 500; }
    .sig-mono { font-family: 'Roboto Mono', monospace; font-weight: 600; }
    .sig-badge-tipo { font-size: 10px; padding: 3px 6px; background: rgba(59,130,246,.15); color: #3b82f6; }
    .sig-incidencia-badge { display: inline-flex; align-items: center; gap: 4px; font-size: 11px; font-weight: 600; color: #f59e0b; }

    .sig-btn-icon {
      width: 30px;
      height: 30px;
      border-radius: 6px;
      border: 1px solid var(--sig-border);
      background: transparent;
      cursor: pointer;
      color: var(--sig-text-secondary);
      &:hover { background: var(--sig-bg-card-alt); }
    }

    /* Detail row */
    .sig-detail-row td { padding: 0; background: rgba(59,130,246,.02); }

    .sig-calc-detail {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 16px;
      padding: 16px;
    }

    .sig-calc-section {
      background: var(--sig-bg-card-alt);
      border: 1px solid var(--sig-border);
      border-radius: 8px;
      padding: 12px;
      h4 {
        margin: 0 0 8px;
        font-size: 12px;
        font-weight: 700;
        color: var(--sig-text-heading);
        display: flex;
        align-items: center;
        gap: 4px;
      }
    }

    .sig-formula {
      display: block;
      background: var(--sig-bg-app);
      border: 1px solid var(--sig-border);
      border-radius: 4px;
      padding: 8px;
      font-size: 11px;
      font-family: 'Roboto Mono', monospace;
      color: var(--sig-text-muted);
      overflow-x: auto;
      white-space: pre-wrap;
      word-break: break-all;
    }

    .sig-inputs-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
      gap: 8px;
    }

    .sig-input-item {
      display: flex;
      flex-direction: column;
      gap: 2px;
      padding: 6px;
      background: var(--sig-bg-app);
      border-radius: 4px;
    }

    .sig-input-key {
      font-size: 10px;
      font-weight: 700;
      color: var(--sig-text-muted);
      text-transform: uppercase;
    }

    .sig-input-value {
      font-size: 12px;
      font-weight: 600;
      font-family: 'Roboto Mono', monospace;
      color: var(--sig-text-primary);
    }

    .sig-resultado {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .sig-resultado-valor {
      font-size: 18px;
      font-weight: 700;
      font-family: 'Roboto Mono', monospace;
      color: #22c55e;
    }

    .sig-resultado-origen {
      font-size: 11px;
      color: var(--sig-text-muted);
    }

    .sig-incidencias-section {
      border-color: rgba(245,158,11,.3) !important;
      h4 { color: #f59e0b; }
    }

    .sig-incidencia-text {
      margin: 0 0 10px;
      font-size: 12px;
      color: var(--sig-text-primary);
      line-height: 1.4;
    }

    .sig-btn-override {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 6px 12px;
      border-radius: 6px;
      border: 1px solid rgba(245,158,11,.3);
      background: rgba(245,158,11,.1);
      color: #f59e0b;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
      &:hover { background: rgba(245,158,11,.18); }
    }

    .sig-calc-meta {
      grid-column: 1 / -1;
      text-align: right;
      padding-top: 8px;
      border-top: 1px solid var(--sig-border);
    }

    .sig-meta-timestamp {
      font-size: 10px;
      color: var(--sig-text-muted);
    }
  `]
})
export class ClosureLinesDetailComponent {
  @Input() lineas: ClosureLine[] = [];
  @Input() detallesCalculos: Map<number, CalculationDetailDto> = new Map();
  @Output() editarLinea = new EventEmitter<ClosureLine>();
  @Output() overrideLinea = new EventEmitter<ClosureLine>();

  protected expandido = signal<number | null>(null);

  protected detalle = (lineId: number) => this.detallesCalculos.get(lineId);

  protected toggle(lineId: number) {
    this.expandido.set(this.expandido() === lineId ? null : lineId);
  }

  protected editar(line: ClosureLine) {
    this.editarLinea.emit(line);
  }

  protected abrirOverride(line: ClosureLine) {
    this.overrideLinea.emit(line);
  }

  protected parseInputs(inputsJson: string): { key: string; value: string }[] {
    try {
      const obj = JSON.parse(inputsJson);
      return Object.entries(obj).map(([key, value]) => ({
        key,
        value: String(value)
      }));
    } catch {
      return [];
    }
  }
}
