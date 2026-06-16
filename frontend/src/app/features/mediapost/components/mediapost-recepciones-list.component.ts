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
import { MediapostService } from '../services/mediapost.service';

interface MediapostRecepcion {
  recepcionId: string;
  referenciaRecepcion: string;
  codigoArticulo: string;
  fechaRecepcion: string;
  cantidad: number;
  cantidadDañada: number;
  estado: string;
  almacen: string;
  observaciones: string;
}

@Component({
  selector: 'app-mediapost-recepciones-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatTableModule, MatPaginatorModule, MatFormFieldModule, MatInputModule, MatIconModule, MatProgressBarModule],
  template: `
    <div class="list-container">
      <mat-form-field class="search-field" appearance="outline">
        <mat-label>Buscar</mat-label>
        <input matInput [formControl]="searchControl" placeholder="Recepción, referencia, código..." />
        <mat-icon matSuffix>search</mat-icon>
      </mat-form-field>

      <mat-progress-bar *ngIf="loading" mode="indeterminate"></mat-progress-bar>

      <table mat-table [dataSource]="items" class="data-table">
        <ng-container matColumnDef="recepcionId">
          <th mat-header-cell *matHeaderCellDef>Recepción ID</th>
          <td mat-cell *matCellDef="let element">{{ element.recepcionId }}</td>
        </ng-container>

        <ng-container matColumnDef="codigoArticulo">
          <th mat-header-cell *matHeaderCellDef>Código</th>
          <td mat-cell *matCellDef="let element">{{ element.codigoArticulo }}</td>
        </ng-container>

        <ng-container matColumnDef="cantidad">
          <th mat-header-cell *matHeaderCellDef>Cantidad</th>
          <td mat-cell *matCellDef="let element">{{ element.cantidad }}</td>
        </ng-container>

        <ng-container matColumnDef="cantidadDanada">
          <th mat-header-cell *matHeaderCellDef>Dañada</th>
          <td mat-cell *matCellDef="let element" [ngClass]="element['cantidadDañada'] > 0 ? 'damaged-alert' : ''">
            {{ element['cantidadDañada'] }}
          </td>
        </ng-container>

        <ng-container matColumnDef="estado">
          <th mat-header-cell *matHeaderCellDef>Estado</th>
          <td mat-cell *matCellDef="let element">{{ element.estado }}</td>
        </ng-container>

        <ng-container matColumnDef="almacen">
          <th mat-header-cell *matHeaderCellDef>Almacén</th>
          <td mat-cell *matCellDef="let element">{{ element.almacen }}</td>
        </ng-container>

        <ng-container matColumnDef="fechaRecepcion">
          <th mat-header-cell *matHeaderCellDef>Fecha</th>
          <td mat-cell *matCellDef="let element">{{ element.fechaRecepcion | date: 'short' }}</td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
      </table>

      <mat-paginator
        [pageSizeOptions]="[10, 25, 50]"
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

    .damaged-alert {
      color: #F44336;
      font-weight: 600;
    }
  `]
})
export class MediapostRecepcionesListComponent implements OnInit {
  items: MediapostRecepcion[] = [];
  loading = false;
  total = 0;
  pageSize = 25;
  currentPage = 1;
  searchControl = new FormControl('');
  displayedColumns = ['recepcionId', 'codigoArticulo', 'cantidad', 'cantidadDanada', 'estado', 'almacen', 'fechaRecepcion'];

  constructor(private mediapostService: MediapostService) { }

  ngOnInit(): void {
    this.loadRecepciones();
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.currentPage = 1;
        this.loadRecepciones();
      });
  }

  loadRecepciones(): void {
    this.loading = true;
    this.mediapostService.getRecepciones(this.currentPage, this.pageSize, this.searchControl.value || '')
      .subscribe({
        next: (result) => {
          this.items = result.items;
          this.total = result.total;
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading recepciones', err);
          this.loading = false;
        }
      });
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex + 1;
    this.loadRecepciones();
  }
}
