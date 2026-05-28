import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { VariableService } from '../../core/api/misc.service';
import { VariableDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';
import { exportCSV } from '../../core/api/api.helpers';

@Component({
  selector: 'app-variables-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatDialogModule, BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Variables' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Variables</h1>
        <a mat-flat-button color="primary" routerLink="/variables/nueva" data-testid="btn-nuevo"><mat-icon>add</mat-icon> Nueva Variable</a>
      </div>
      <mat-card><mat-card-content>
        @if (loading()) { <sig-skeleton [count]="4" /> }
        @else if (items().length === 0) {
          <sig-empty-state icon="data_object" title="No hay variables todavía"
            description="Las variables mapean respuestas de Celero a valores numéricos."
            ctaLabel="Crear primera variable" (ctaClick)="router.navigate(['/variables/nueva'])" />
        } @else {
          <div style="display: flex; justify-content: flex-end; margin-bottom: 12px;">
            <button mat-stroked-button (click)="onExportCSV()" data-testid="btn-exportar-csv"><mat-icon>download</mat-icon> Exportar CSV</button>
          </div>
          <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-variables">
            <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>Nombre</th><td mat-cell *matCellDef="let row">{{ row.nombre }}</td></ng-container>
            <ng-container matColumnDef="questionId"><th mat-header-cell *matHeaderCellDef>Question ID</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.questionIdExterno }}</td></ng-container>
            <ng-container matColumnDef="mapeos"><th mat-header-cell *matHeaderCellDef>Mapeos</th><td mat-cell *matCellDef="let row">{{ countMapeos(row) }}</td></ng-container>
            <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef style="text-align: right;">Acciones</th>
              <td mat-cell *matCellDef="let row">
                <div class="sig-table-actions">
                  <a mat-icon-button [routerLink]="['/variables', row.id, 'editar']" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                  <button mat-icon-button (click)="onDelete(row)" [attr.data-testid]="'btn-eliminar-' + row.id" aria-label="Eliminar"><mat-icon>delete</mat-icon></button>
                </div>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="['nombre', 'questionId', 'mapeos', 'acciones']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['nombre', 'questionId', 'mapeos', 'acciones']" data-testid="row-variable"></tr>
          </table>
        }
      </mat-card-content></mat-card>
    </div>
  `,
})
export class VariablesListComponent implements OnInit {
  private readonly variableSvc = inject(VariableService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  protected readonly router = inject(Router);

  protected readonly items = signal<VariableDto[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void { this.load(); }

  protected countMapeos(v: VariableDto): number {
    try { const arr = JSON.parse(v.mapeoValoresJson); return Array.isArray(arr) ? arr.length : 0; } catch { return 0; }
  }
  protected onDelete(row: VariableDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Variable', message: 'Acción irreversible.', entityName: row.nombre, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.variableSvc.delete(row.id).subscribe({
        next: () => { this.notify.success('Variable eliminada'); this.load(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }
  protected onExportCSV(): void { exportCSV('variables.csv', this.items().map((v) => ({ Id: v.id, Nombre: v.nombre, QuestionId: v.questionIdExterno, Mapeos: v.mapeoValoresJson }))); }
  private load(): void {
    this.loading.set(true);
    this.variableSvc.list().subscribe({
      next: (vs) => { this.items.set(vs); this.loading.set(false); },
      error: () => { this.items.set([]); this.loading.set(false); },
    });
  }
}
