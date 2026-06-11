import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PaletteItem } from '../models/formula.model';

/**
 * Componente Paleta: panel izquierdo con items draggables
 * Organiza items en secciones: Números, Variables, Operaciones
 */
@Component({
  selector: 'app-formula-palette',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatTooltipModule],
  template: `
    <div class="palette-panel" data-testid="formula-palette">
      <div class="palette-scroll">
        <!-- Sección NÚMEROS -->
        <div class="palette-section" [attr.data-testid]="'formula-palette-section-Números'">
          <div class="palette-section__header">Números</div>
          <div class="palette-items">
            <div
              *ngFor="let item of getItemsByCategory('Números')"
              class="palette-item"
              draggable="true"
              (dragstart)="onItemDragStart($event, item)"
              (dragend)="onItemDragEnd()"
              [attr.data-testid]="'formula-palette-item-' + item.id"
              [attr.aria-label]="item.name + ' - ' + item.description"
              role="button"
              tabindex="0"
              [matTooltip]="item.description"
            >
              <mat-icon class="palette-item__icon">{{ item.icon }}</mat-icon>
              <span class="palette-item__name">{{ item.name }}</span>
            </div>
          </div>
        </div>

        <!-- Sección VARIABLES -->
        <div class="palette-section" [attr.data-testid]="'formula-palette-section-Variables'">
          <div class="palette-section__header">Variables</div>
          <div class="palette-items">
            <div
              *ngFor="let item of getItemsByCategory('Variables')"
              class="palette-item"
              draggable="true"
              (dragstart)="onItemDragStart($event, item)"
              (dragend)="onItemDragEnd()"
              [attr.data-testid]="'formula-palette-item-' + item.id"
              [attr.aria-label]="item.name + ' - ' + item.description"
              role="button"
              tabindex="0"
              [matTooltip]="item.description"
            >
              <mat-icon class="palette-item__icon">{{ item.icon }}</mat-icon>
              <span class="palette-item__name">{{ item.name }}</span>
            </div>
          </div>
        </div>

        <!-- Sección OPERACIONES -->
        <div class="palette-section" [attr.data-testid]="'formula-palette-section-Operaciones'">
          <div class="palette-section__header">Operaciones</div>
          <div class="palette-items">
            <div
              *ngFor="let item of getItemsByCategory('Operaciones')"
              class="palette-item"
              draggable="true"
              (dragstart)="onItemDragStart($event, item)"
              (dragend)="onItemDragEnd()"
              [attr.data-testid]="'formula-palette-item-' + item.id"
              [attr.aria-label]="item.name + ' - ' + item.description"
              role="button"
              tabindex="0"
              [matTooltip]="item.description"
            >
              <mat-icon class="palette-item__icon">{{ item.icon }}</mat-icon>
              <span class="palette-item__name">{{ item.name }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .palette-panel {
        width: 200px;
        background-color: #f5f7fa;
        border-right: 1px solid #e8edf5;
        flex-shrink: 0;
        overflow: hidden;
        display: flex;
        flex-direction: column;
      }

      .palette-scroll {
        flex: 1;
        overflow-y: auto;
        padding: 16px 0;

        &::-webkit-scrollbar {
          width: 6px;
        }

        &::-webkit-scrollbar-track {
          background: transparent;
        }

        &::-webkit-scrollbar-thumb {
          background: #d0d0d0;
          border-radius: 3px;

          &:hover {
            background: #2e5c8a;
          }
        }
      }

      .palette-section {
        margin-bottom: 24px;

        .palette-section__header {
          font-size: 12px;
          font-weight: 600;
          color: #1a1a1a;
          padding: 0 16px 8px;
          text-transform: uppercase;
          letter-spacing: 0.5px;
        }
      }

      .palette-items {
        display: flex;
        flex-direction: column;
        gap: 4px;
        padding: 0 8px;
      }

      .palette-item {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 12px 16px;
        background-color: #ffffff;
        border: 1px solid transparent;
        border-radius: 6px;
        cursor: grab;
        user-select: none;
        transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);
        font-size: 14px;
        color: #1a1a1a;

        &:hover {
          background-color: #f0f4f8;
          box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
          cursor: grab;
        }

        &:active {
          cursor: grabbing;
          opacity: 0.8;
        }

        &:focus-visible {
          outline: 2px solid #1f4e78;
          outline-offset: 2px;
        }

        .palette-item__icon {
          font-size: 18px;
          width: 18px;
          height: 18px;
          flex-shrink: 0;
          color: #1f4e78;
        }

        .palette-item__name {
          white-space: nowrap;
          overflow: hidden;
          text-overflow: ellipsis;
          font-weight: 400;
        }
      }
    `,
  ],
})
export class FormulaPaletteComponent {
  @Input() items: PaletteItem[] = [];

  @Output() itemDragStart = new EventEmitter<{ item: PaletteItem; event: DragEvent }>();
  @Output() itemDragEnd = new EventEmitter<void>();

  getItemsByCategory(category: 'Números' | 'Variables' | 'Operaciones'): PaletteItem[] {
    return this.items.filter((item) => item.category === category);
  }

  onItemDragStart(event: DragEvent, item: PaletteItem): void {
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'copy';
      event.dataTransfer.setData('application/json', JSON.stringify(item.template));
    }
    this.itemDragStart.emit({ item, event });
  }

  onItemDragEnd(): void {
    this.itemDragEnd.emit();
  }
}
