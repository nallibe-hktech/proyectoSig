import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
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
  imports: [CommonModule, MatTableModule, MatProgressBarModule],
  template: `
    <div class="list-container">
      <mat-progress-bar *ngIf="loading" mode="indeterminate"></mat-progress-bar>

      <mat-table [dataSource]="items" class="data-table">
        <ng-container matColumnDef="codigoArticulo">
          <mat-header-cell *matHeaderCellDef>Código</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.codigoArticulo }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="descripcion">
          <mat-header-cell *matHeaderCellDef>Descripción</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.descripcion }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="stock">
          <mat-header-cell *matHeaderCellDef>Stock</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.stock }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="stockA">
          <mat-header-cell *matHeaderCellDef>Stock A</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.stockA }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="stockB">
          <mat-header-cell *matHeaderCellDef>Stock B</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.stockB }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="almacen">
          <mat-header-cell *matHeaderCellDef>Almacén</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.almacen }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="codigoCelda">
          <mat-header-cell *matHeaderCellDef>Celda</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.codigoCelda }}</mat-cell>
        </ng-container>

        <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
        <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
      </mat-table>
    </div>
  `,
  styles: [`
    .list-container {
      padding: 16px;
    }

    .data-table {
      width: 100%;
    }

    mat-header-cell {
      font-weight: 600;
      background-color: #f5f5f5;
    }

    mat-row:hover {
      background-color: #fafafa;
    }
  `]
})
export class GalanStockListComponent implements OnInit {
  items: GalanStock[] = [];
  loading = false;
  displayedColumns = ['codigoArticulo', 'descripcion', 'stock', 'stockA', 'stockB', 'almacen', 'codigoCelda'];

  constructor(private galanService: GalanService) { }

  ngOnInit(): void {
    this.loadStock();
  }

  loadStock(): void {
    this.loading = true;
    this.galanService.getStock()
      .subscribe({
        next: (items) => {
          this.items = items;
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading stock', err);
          this.loading = false;
        }
      });
  }
}
