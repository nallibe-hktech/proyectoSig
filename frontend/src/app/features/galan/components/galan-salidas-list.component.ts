import { Component, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { PageEvent, MatPaginatorModule } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { GalanService } from '../services/galan.service';

interface GalanSalida {
  albaran: string;
  numeroPedidoTercero: string;
  codigoArticulo: string;
  descripcion: string;
  unidades: number;
  fecha: string;
  destinatario: string;
  almacen: string;
  celda: string;
}

@Component({
  selector: 'app-galan-salidas-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatTableModule, MatPaginatorModule, MatFormFieldModule, MatInputModule, MatIconModule, MatProgressBarModule],
  template: `
    <div class="list-container">
      <mat-form-field class="search-field" appearance="outline">
        <mat-label>Buscar</mat-label>
        <input matInput [formControl]="searchControl" placeholder="Código, descripción, destinatario..." />
        <mat-icon matSuffix>search</mat-icon>
      </mat-form-field>

      <mat-progress-bar *ngIf="loading" mode="indeterminate"></mat-progress-bar>

      <table mat-table [dataSource]="items" class="data-table">
        <ng-container matColumnDef="albaran">
          <th mat-header-cell *matHeaderCellDef>Albarán</th>
          <td mat-cell *matCellDef="let element">{{ element.albaran }}</td>
        </ng-container>

        <ng-container matColumnDef="codigoArticulo">
          <th mat-header-cell *matHeaderCellDef>Código</th>
          <td mat-cell *matCellDef="let element">{{ element.codigoArticulo }}</td>
        </ng-container>

        <ng-container matColumnDef="descripcion">
          <th mat-header-cell *matHeaderCellDef>Descripción</th>
          <td mat-cell *matCellDef="let element">{{ element.descripcion }}</td>
        </ng-container>

        <ng-container matColumnDef="unidades">
          <th mat-header-cell *matHeaderCellDef>Unidades</th>
          <td mat-cell *matCellDef="let element">{{ element.unidades }}</td>
        </ng-container>

        <ng-container matColumnDef="destinatario">
          <th mat-header-cell *matHeaderCellDef>Destinatario</th>
          <td mat-cell *matCellDef="let element">{{ element.destinatario }}</td>
        </ng-container>

        <ng-container matColumnDef="fecha">
          <th mat-header-cell *matHeaderCellDef>Fecha</th>
          <td mat-cell *matCellDef="let element">{{ element.fecha | date: 'short' }}</td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
      </table>

      <mat-paginator
        [pageSizeOptions]="[25, 50, 100]"
        [pageSize]="pageSize"
        [length]="total"
        (page)="onPageChange($event)"
      ></mat-paginator>
    </div>
  `,
  styles: [`
    .list-container {
      padding: 16px;
    }

    .search-field {
      width: 100%;
      margin-bottom: 16px;
    }

    .data-table {
      width: 100%;
    }

    th {
      font-weight: 600;
      background-color: var(--sig-bg-header);
      color: var(--sig-text-muted);
      border-bottom: 1px solid var(--sig-border);
    }

    td {
      border-bottom: 1px solid var(--sig-border);
    }

    tr:hover {
      background-color: var(--sig-bg-hover);
    }
  `]
})
export class GalanSalidasListComponent implements OnInit {
  items: GalanSalida[] = [];
  loading = false;
  total = 0;
  pageSize = 25;
  currentPage = 1;
  searchControl = new FormControl('');
  displayedColumns = ['albaran', 'codigoArticulo', 'descripcion', 'unidades', 'destinatario', 'fecha'];

  constructor(private galanService: GalanService) { }

  ngOnInit(): void {
    this.loadSalidas();
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.currentPage = 1;
        this.loadSalidas();
      });
  }

  loadSalidas(): void {
    this.loading = true;
    this.galanService.getSalidas(this.currentPage, this.pageSize, this.searchControl.value || '')
      .subscribe({
        next: (result) => {
          this.items = result.items;
          this.total = result.total;
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading salidas', err);
          this.loading = false;
        }
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex + 1;
    this.loadSalidas();
  }
}
