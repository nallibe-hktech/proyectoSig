import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ContratoService } from '../../core/api/contratos.service';
import { ContratoUnDiaDto } from '../../models/dtos';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';

// Ola 2 (#2): contratos de un día (FechaInicio == FechaFin). Se señalan y se pueden marcar "a ignorar" con motivo.
@Component({
  selector: 'app-contratos-un-dia',
  standalone: true,
  imports: [
    CommonModule, DatePipe, DecimalPipe, FormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Contratos de un día' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">Contratos de un día</h1></div>
      <mat-card><mat-card-content>
        @if (loading()) { <sig-skeleton [count]="5" /> }
        @else if (items().length === 0) {
          <sig-empty-state icon="description" title="No hay contratos de un día" />
        } @else {
          <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-contratos-un-dia">
            <ng-container matColumnDef="contrato"><th mat-header-cell *matHeaderCellDef>Contrato</th><td mat-cell *matCellDef="let row">{{ row.contratoIdExterno }}</td></ng-container>
            <ng-container matColumnDef="nif"><th mat-header-cell *matHeaderCellDef>NIF</th><td mat-cell *matCellDef="let row">{{ row.nif }}</td></ng-container>
            <ng-container matColumnDef="empleado"><th mat-header-cell *matHeaderCellDef>Empleado</th><td mat-cell *matCellDef="let row">{{ row.userNombre || '—' }}</td></ng-container>
            <ng-container matColumnDef="fecha"><th mat-header-cell *matHeaderCellDef>Fecha</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaInicio | date:'dd/MM/yyyy' }}</td></ng-container>
            <ng-container matColumnDef="importe"><th mat-header-cell *matHeaderCellDef>Importe bruto</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.importeBruto | number:'1.0-2' }} €</td></ng-container>
            <ng-container matColumnDef="estado"><th mat-header-cell *matHeaderCellDef>Estado</th>
              <td mat-cell *matCellDef="let row">
                @if (row.ignoradoEnCierre) {
                  <span class="sig-badge sig-badge--rejected" data-testid="badge-ignorado"><mat-icon style="font-size:14px;width:14px;height:14px;">block</mat-icon> Ignorado</span>
                } @else {
                  <span class="sig-badge sig-badge--approved">Activo en cierre</span>
                }
                @if (row.ignoradoEnCierre && row.motivoIgnorar) { <div style="font-size:11px;color:var(--sig-text-muted);margin-top:4px;">{{ row.motivoIgnorar }}</div> }
              </td>
            </ng-container>
            <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef style="text-align:right;">Acciones</th>
              <td mat-cell *matCellDef="let row">
                <div class="sig-table-actions" style="display:flex;align-items:center;gap:8px;justify-content:flex-end;">
                  @if (!row.ignoradoEnCierre) {
                    <mat-form-field appearance="outline" style="width:220px;" subscriptSizing="dynamic">
                      <mat-label>Motivo</mat-label>
                      <input matInput [(ngModel)]="motivos[row.id]" [attr.data-testid]="'input-motivo-' + row.id" placeholder="Motivo de exclusión" />
                    </mat-form-field>
                    <button mat-stroked-button (click)="ignorar(row)" [disabled]="!motivos[row.id]" [attr.data-testid]="'btn-ignorar-' + row.id"><mat-icon>block</mat-icon> Ignorar</button>
                  } @else {
                    <button mat-stroked-button (click)="reactivar(row)" [attr.data-testid]="'btn-reactivar-' + row.id"><mat-icon>undo</mat-icon> Reactivar</button>
                  }
                </div>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="cols"></tr>
            <tr mat-row *matRowDef="let row; columns: cols" data-testid="row-contrato"></tr>
          </table>
        }
      </mat-card-content></mat-card>
    </div>
  `,
})
export class ContratosUnDiaComponent implements OnInit {
  private readonly contratoSvc = inject(ContratoService);
  private readonly notify = inject(NotifyService);

  protected readonly items = signal<ContratoUnDiaDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly cols = ['contrato', 'nif', 'empleado', 'fecha', 'importe', 'estado', 'acciones'];
  protected motivos: Record<number, string> = {};

  ngOnInit(): void { this.load(); }

  protected ignorar(row: ContratoUnDiaDto): void {
    const motivo = this.motivos[row.id];
    if (!motivo) return;
    this.contratoSvc.marcarIgnorar(row.id, { ignorar: true, motivo }).subscribe({
      next: () => { this.notify.success('Contrato marcado a ignorar'); this.load(); },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo marcar el contrato'),
    });
  }

  protected reactivar(row: ContratoUnDiaDto): void {
    this.contratoSvc.marcarIgnorar(row.id, { ignorar: false, motivo: null }).subscribe({
      next: () => { this.notify.success('Contrato reactivado'); this.load(); },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo reactivar el contrato'),
    });
  }

  private load(): void {
    this.loading.set(true);
    this.contratoSvc.listUnDia().subscribe({
      next: (list) => { this.items.set(list ?? []); this.motivos = {}; this.loading.set(false); },
      error: () => { this.items.set([]); this.loading.set(false); },
    });
  }
}
