import { Component, Input, Output, EventEmitter, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';

/**
 * Componente de búsqueda full-text para listados/tablas.
 * Soporta múltiples campos de búsqueda, filtros de columna y estadísticas en tiempo real.
 *
 * Uso:
 * <app-fulltext-search
 *   [items]="datos"
 *   [searchFields]="['nombre', 'email', 'ciudad']"
 *   placeholder="Buscar por nombre, email..."
 *   (resultsChange)="onResults($event)"
 * />
 */
@Component({
  selector: 'app-fulltext-search',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatFormFieldModule, MatInputModule, MatIconModule, MatButtonModule, MatChipsModule, MatTooltipModule,
  ],
  template: `
    <div class="sig-search-container">
      <mat-form-field appearance="outline" class="sig-search-field">
        <mat-label>{{ placeholder || 'Buscar...' }}</mat-label>
        <input
          matInput
          [(ngModel)]="searchText"
          (input)="onSearchInput()"
          [attr.aria-label]="placeholder || 'Buscar'"
          class="sig-search-input"
        />
        @if (searchText()) {
          <button
            matIconSuffix
            mat-icon-button
            (click)="clearSearch()"
            aria-label="Limpiar búsqueda"
            class="sig-clear-btn"
          >
            <mat-icon>close</mat-icon>
          </button>
        } @else {
          <mat-icon matSuffix class="sig-search-icon">search</mat-icon>
        }
      </mat-form-field>

      <!-- Estadísticas -->
      <div class="sig-search-stats">
        <span class="sig-stat-item">
          <mat-icon class="sig-stat-icon">{{ matchCount() > 0 ? 'check_circle' : 'cancel' }}</mat-icon>
          {{ matchCount() > 0 ? matchCount() + ' coincidencia' + (matchCount() !== 1 ? 's' : '') : 'Sin coincidencias' }}
        </span>
        @if (searchText()) {
          <span class="sig-stat-divider">•</span>
          <span class="sig-stat-item sig-stat-secondary">
            {{ ((matchCount() / totalItems()) * 100) | number:'1.0-0' }}% de {{ totalItems() }}
          </span>
        }
      </div>

      <!-- Filtros de columna (opcional) -->
      @if (searchFields.length > 1) {
        <div class="sig-column-filters">
          <span class="sig-filter-label">Buscar en:</span>
          <mat-chip-set aria-label="Columnas de búsqueda">
            @for (field of searchFields; track field) {
              <mat-chip
                [class.selected]="selectedFields().includes(field)"
                (click)="toggleField(field)"
                class="sig-field-chip"
              >
                {{ field }}
              </mat-chip>
            }
          </mat-chip-set>
        </div>
      }

      <!-- Sugerencias de búsqueda avanzada -->
      @if (searchText() && !hasResults() && suggestions().length > 0) {
        <div class="sig-suggestions">
          <div class="sig-suggestions-label">¿Intentaste buscar por?</div>
          @for (suggestion of suggestions(); track suggestion) {
            <button
              mat-stroked-button
              class="sig-suggestion-btn"
              (click)="applySuggestion(suggestion)"
            >
              {{ suggestion }}
            </button>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .sig-search-container {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 16px;
      background: var(--sig-bg-card);
      border-radius: 8px;
      border: 1px solid var(--sig-border);
    }

    .sig-search-field {
      width: 100%;
    }

    .sig-search-input {
      font-size: 14px;
    }

    .sig-clear-btn {
      color: var(--sig-text-muted) !important;
    }

    .sig-search-icon {
      color: var(--sig-text-muted);
      font-size: 20px;
      width: 20px;
      height: 20px;
    }

    .sig-search-stats {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 12px;
      color: var(--sig-text-muted);
    }

    .sig-stat-item {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    .sig-stat-icon {
      font-size: 14px;
      width: 14px;
      height: 14px;
    }

    .sig-stat-secondary {
      font-weight: 600;
      color: var(--sig-text-primary);
    }

    .sig-stat-divider {
      opacity: 0.3;
    }

    .sig-column-filters {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-wrap: wrap;
    }

    .sig-filter-label {
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      color: var(--sig-text-muted);
      letter-spacing: 0.08em;
    }

    .sig-field-chip {
      font-size: 12px;
      height: 32px !important;
    }

    .sig-suggestions {
      padding: 12px;
      background: rgba(59, 130, 246, 0.05);
      border: 1px solid rgba(59, 130, 246, 0.2);
      border-radius: 6px;
    }

    .sig-suggestions-label {
      font-size: 11px;
      font-weight: 600;
      color: var(--sig-text-muted);
      text-transform: uppercase;
      margin-bottom: 8px;
      letter-spacing: 0.08em;
    }

    .sig-suggestion-btn {
      font-size: 11px;
      height: 28px;
      margin-right: 4px;
      margin-bottom: 4px;
    }
  `],
})
export class FulltextSearchComponent {
  @Input() items: Record<string, unknown>[] = [];
  @Input() searchFields: string[] = [];
  @Input() placeholder: string = 'Buscar...';
  @Output() resultsChange = new EventEmitter<Record<string, unknown>[]>();

  protected readonly searchText = signal('');
  protected readonly selectedFields = signal<string[]>([]);
  protected readonly totalItems = computed(() => this.items.length);

  protected readonly filteredResults = computed(() => {
    const text = this.searchText().toLowerCase().trim();
    if (!text) return this.items;

    const fields = this.selectedFields().length > 0 ? this.selectedFields() : this.searchFields;
    if (fields.length === 0) return this.items;

    return this.items.filter((item) =>
      fields.some((field) => {
        const value = item[field];
        if (value === null || value === undefined) return false;
        return String(value).toLowerCase().includes(text);
      })
    );
  });

  protected readonly matchCount = computed(() => this.filteredResults().length);
  protected readonly hasResults = computed(() => this.matchCount() > 0);

  protected readonly suggestions = computed(() => {
    const text = this.searchText().toLowerCase().trim();
    if (!text || this.hasResults()) return [];

    const fields = this.selectedFields().length > 0 ? this.selectedFields() : this.searchFields;
    const uniqueValues = new Set<string>();

    this.items.forEach((item) => {
      fields.forEach((field) => {
        const value = item[field];
        if (value) uniqueValues.add(String(value).toLowerCase());
      });
    });

    return Array.from(uniqueValues)
      .filter((val) => val.includes(text) || this.levenshteinDistance(val, text) <= 2)
      .slice(0, 3);
  });

  protected onSearchInput(): void {
    if (this.selectedFields().length === 0 && this.searchFields.length > 0) {
      this.selectedFields.set([...this.searchFields]);
    }
    this.resultsChange.emit(this.filteredResults());
  }

  protected clearSearch(): void {
    this.searchText.set('');
    this.resultsChange.emit(this.items);
  }

  protected toggleField(field: string): void {
    const current = this.selectedFields();
    const updated = current.includes(field) ? current.filter((f) => f !== field) : [...current, field];
    this.selectedFields.set(updated.length === 0 ? [...this.searchFields] : updated);
    this.onSearchInput();
  }

  protected applySuggestion(suggestion: string): void {
    this.searchText.set(suggestion);
    this.onSearchInput();
  }

  /**
   * Distancia de Levenshtein para sugerencias fuzzy.
   * Útil para detectar typos y variaciones.
   */
  private levenshteinDistance(a: string, b: string): number {
    const matrix: number[][] = [];
    for (let i = 0; i <= b.length; i++) {
      matrix[i] = [i];
    }
    for (let j = 0; j <= a.length; j++) {
      matrix[0][j] = j;
    }
    for (let i = 1; i <= b.length; i++) {
      for (let j = 1; j <= a.length; j++) {
        if (b.charAt(i - 1) === a.charAt(j - 1)) {
          matrix[i][j] = matrix[i - 1][j - 1];
        } else {
          matrix[i][j] = Math.min(
            matrix[i - 1][j - 1] + 1,
            matrix[i][j - 1] + 1,
            matrix[i - 1][j] + 1
          );
        }
      }
    }
    return matrix[b.length][a.length];
  }
}
