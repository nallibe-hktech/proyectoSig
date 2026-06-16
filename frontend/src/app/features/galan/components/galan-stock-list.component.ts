import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { GalanService } from '../services/galan.service';

interface GalanStock {
  codigoArticulo: string;
  descripcion: string;
  stock: number;
  stockA: number;
  stockB: number;
  almacen: string;
  codigoCelda: string;
  familia: string;
  subFamilia: string;
}

@Component({
  selector: 'app-galan-stock-list',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatProgressBarModule, MatPaginatorModule],
  template: `
    <div class="list-container">
      <mat-progress-bar *ngIf="loading" mode="indeterminate"></mat-progress-bar>

      <table mat-table [dataSource]="items" class="data-table">
        <ng-container matColumnDef="codigoArticulo">
          <th mat-header-cell *matHeaderCellDef>Código</th>
          <td mat-cell *matCellDef="let element">{{ element.codigoArticulo }}</td>
        </ng-container>

        <ng-container matColumnDef="descripcion">
          <th mat-header-cell *matHeaderCellDef>Descripción</th>
          <td mat-cell *matCellDef="let element">{{ element.descripcion }}</td>
        </ng-container>

        <ng-container matColumnDef="stock">
          <th mat-header-cell *matHeaderCellDef>Stock</th>
          <td mat-cell *matCellDef="let element">{{ element.stock }}</td>
        </ng-container>

        <ng-container matColumnDef="stockA">
          <th mat-header-cell *matHeaderCellDef>Stock A</th>
          <td mat-cell *matCellDef="let element">{{ element.stockA }}</td>
        </ng-container>

        <ng-container matColumnDef="stockB">
          <th mat-header-cell *matHeaderCellDef>Stock B</th>
          <td mat-cell *matCellDef="let element">{{ element.stockB }}</td>
        </ng-container>

        <ng-container matColumnDef="almacen">
          <th mat-header-cell *matHeaderCellDef>Almacén</th>
          <td mat-cell *matCellDef="let element">{{ element.almacen }}</td>
        </ng-container>

        <ng-container matColumnDef="codigoCelda">
          <th mat-header-cell *matHeaderCellDef>Celda</th>
          <td mat-cell *matCellDef="let element">{{ element.codigoCelda }}</td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
      </table>

      <mat-paginator
        [pageSizeOptions]="[25, 50, 100]"
        [pageSize]="pageSize"
        [length]="total"
        (page)="onPageChange($event)">
      </mat-paginator>
    </div>
  `,
  styles: [`
    .list-container {
      padding: 16px;
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
export class GalanStockListComponent implements OnInit {
  items: GalanStock[] = [];
  loading = false;
  total = 0;
  pageSize = 25;
  currentPage = 1;
  displayedColumns = ['codigoArticulo', 'descripcion', 'stock', 'stockA', 'stockB', 'almacen', 'codigoCelda'];

  constructor(private galanService: GalanService) { }

  ngOnInit(): void {
    this.loadStock();
  }

  loadStock(): void {
    this.loading = true;
    this.galanService.getStock(this.currentPage, this.pageSize)
      .subscribe({
        next: (response) => {
          this.items = response.items;
          this.total = response.total;
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading stock', err);
          this.loading = false;
        }
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex + 1;
    this.loadStock();
  }
}
