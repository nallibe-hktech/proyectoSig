import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { ConceptService } from '../../core/api/concepts.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { PresupuestoConceptoDto } from '../../models/dtos';
import { PresupuestosFormDialogComponent } from './presupuestos-form.dialog';

@Component({
  selector: 'app-presupuestos-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule,
    MatTableModule, MatPaginatorModule, MatTooltipModule, MatDialogModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[
        { label: 'Inicio', route: '/dashboard' },
        { label: 'Conceptos', route: '/concepts' },
        { label: concept()?.nombre ?? '...', route: concept() ? '/concepts/' + concept()!.id : '/concepts' },
        { label: 'Presupuestos' }
      ]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">Presupuestos de {{ concept()?.nombre ?? 'cargando...' }}</h1>
        <button mat-flat-button color="primary" (click)="openCreateDialog()" [disabled]="loading()">
          <mat-icon>add</mat-icon> Nuevo Presupuesto
        </button>
      </div>

      @if (loading()) {
        <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card>
      } @else {
        <mat-card>
          <mat-card-content>
            <div class="sig-search-bar">
              <mat-form-field appearance="outline" class="sig-search-field">
                <mat-label>Buscar por cliente o servicio</mat-label>
                <input matInput [(ngModel)]="searchTerm" (ngModelChange)="onSearch()">
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>
            </div>

            @if (presupuestos().length > 0) {
              <div class="mat-elevation-z1">
                <table mat-table [dataSource]="presupuestos()" class="sig-table">
                  <!-- Cliente Column -->
                  <ng-container matColumnDef="clientNombre">
                    <th mat-header-cell *matHeaderCellDef>Cliente</th>
                    <td mat-cell *matCellDef="let e">{{ e.clientNombre ?? '(Global)' }}</td>
                  </ng-container>

                  <!-- Servicio Column -->
                  <ng-container matColumnDef="serviceNombre">
                    <th mat-header-cell *matHeaderCellDef>Servicio</th>
                    <td mat-cell *matCellDef="let e">{{ e.serviceNombre ?? '(Todos)' }}</td>
                  </ng-container>

                  <!-- Período Column -->
                  <ng-container matColumnDef="periodNombre">
                    <th mat-header-cell *matHeaderCellDef>Período</th>
                    <td mat-cell *matCellDef="let e">{{ e.periodNombre ?? '(Anual)' }}</td>
                  </ng-container>

                  <!-- Tipo Column -->
                  <ng-container matColumnDef="tipo">
                    <th mat-header-cell *matHeaderCellDef>Tipo</th>
                    <td mat-cell *matCellDef="let e">{{ e.tipo }}</td>
                  </ng-container>

                  <!-- Importe Column -->
                  <ng-container matColumnDef="importe">
                    <th mat-header-cell *matHeaderCellDef style="text-align: right;">Importe</th>
                    <td mat-cell *matCellDef="let e" style="text-align: right;">
                      € {{ e.importe.toFixed(2) }}
                    </td>
                  </ng-container>

                  <!-- Actions Column -->
                  <ng-container matColumnDef="acciones">
                    <th mat-header-cell *matHeaderCellDef style="text-align: center;">Acciones</th>
                    <td mat-cell *matCellDef="let e" style="text-align: center;">
                      <button mat-icon-button [matTooltip]="'Editar'" (click)="openEditDialog(e)">
                        <mat-icon>edit</mat-icon>
                      </button>
                      <button mat-icon-button [matTooltip]="'Eliminar'" (click)="delete(e.id)" color="warn">
                        <mat-icon>delete</mat-icon>
                      </button>
                    </td>
                  </ng-container>

                  <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
                  <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
                </table>
              </div>

              <mat-paginator
                [length]="total()"
                [pageSize]="pageSize()"
                [pageIndex]="page() - 1"
                [pageSizeOptions]="[10, 25, 50, 100]"
                showFirstLastButtons
                (page)="onPageChange($event)">
              </mat-paginator>
            } @else {
              <div style="padding: 40px; text-align: center; color: var(--mat-sys-on-surface-variant);">
                <mat-icon style="font-size: 48px; width: 48px; height: 48px; opacity: 0.5;">budget</mat-icon>
                <p>No hay presupuestos registrados</p>
              </div>
            }
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .sig-search-bar { margin-bottom: 16px; display: flex; gap: 12px; }
    .sig-search-field { flex: 1; max-width: 400px; }
    .sig-table { width: 100%; }
    .mat-column-clientNombre { width: 18%; }
    .mat-column-serviceNombre { width: 18%; }
    .mat-column-periodNombre { width: 14%; }
    .mat-column-tipo { width: 12%; }
    .mat-column-importe { width: 14%; }
    .mat-column-acciones { width: 10%; }
  `],
})
export class PresupuestosListComponent implements OnInit {
  private readonly conceptSvc = inject(ConceptService);
  private readonly notify = inject(NotifyService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  protected readonly loading = signal(true);
  protected readonly presupuestos = signal<PresupuestoConceptoDto[]>([]);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly total = signal(0);
  protected readonly searchTerm = signal('');
  protected readonly concept = signal<any>(null);
  protected readonly displayedColumns = ['clientNombre', 'serviceNombre', 'periodNombre', 'tipo', 'importe', 'acciones'];

  private conceptId = 0;

  ngOnInit(): void {
    this.conceptId = Number(this.route.snapshot.paramMap.get('conceptId'));
    this.loadConcept();
    this.loadPresupuestos();
  }

  private loadConcept(): void {
    this.conceptSvc.getById(this.conceptId).subscribe({
      next: (c) => this.concept.set(c),
      error: () => this.notify.error('No se pudo cargar el concepto'),
    });
  }

  private loadPresupuestos(): void {
    this.loading.set(true);
    this.conceptSvc.getPresupuestosPaginated(this.conceptId, this.page(), this.pageSize(), this.searchTerm()).subscribe({
      next: (result) => {
        this.presupuestos.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: () => {
        this.notify.error('No se pudieron cargar los presupuestos');
        this.loading.set(false);
      },
    });
  }

  protected onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.loadPresupuestos();
  }

  protected onSearch(): void {
    this.page.set(1);
    this.loadPresupuestos();
  }

  protected openCreateDialog(): void {
    const dialogRef = this.dialog.open(PresupuestosFormDialogComponent, {
      width: '600px',
      data: { conceptId: this.conceptId, presupuesto: null },
    });
    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.notify.success('Presupuesto creado');
        this.loadPresupuestos();
      }
    });
  }

  protected openEditDialog(presupuesto: PresupuestoConceptoDto): void {
    const dialogRef = this.dialog.open(PresupuestosFormDialogComponent, {
      width: '600px',
      data: { conceptId: this.conceptId, presupuesto },
    });
    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.notify.success('Presupuesto actualizado');
        this.loadPresupuestos();
      }
    });
  }

  protected delete(id: number): void {
    if (!confirm('¿Eliminar este presupuesto?')) return;
    this.conceptSvc.deletePresupuesto(this.conceptId, id).subscribe({
      next: () => {
        this.notify.success('Presupuesto eliminado');
        this.loadPresupuestos();
      },
      error: () => this.notify.error('No se pudo eliminar el presupuesto'),
    });
  }
}
