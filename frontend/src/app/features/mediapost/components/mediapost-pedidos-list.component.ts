import { Component, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { PageEvent, MatPaginatorModule } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MediapostService } from '../services/mediapost.service';

interface MediapostPedido {
  pedidoId: string;
  referenciaPedido: string;
  codigoArticulo: string;
  fechaPedido: string;
  cantidad: number;
  estado: string;
  destinatarioNombre: string;
  direccionEntrega: string;
  codigoPostal: string;
  ciudad: string;
  provincia: string;
}

@Component({
  selector: 'app-mediapost-pedidos-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatTableModule, MatPaginatorModule, MatFormFieldModule, MatInputModule, MatIconModule, MatSelectModule, MatProgressBarModule],
  template: `
    <div class="list-container">
      <div class="filters">
        <mat-form-field class="search-field" appearance="outline">
          <mat-label>Buscar</mat-label>
          <input matInput [formControl]="searchControl" placeholder="Pedido, referencia, destinatario..." />
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Estado</mat-label>
          <mat-select [formControl]="estadoControl">
            <mat-option value="">Todos</mat-option>
            <mat-option value="Pendiente">Pendiente</mat-option>
            <mat-option value="En tránsito">En tránsito</mat-option>
            <mat-option value="Entregado">Entregado</mat-option>
            <mat-option value="Rechazado">Rechazado</mat-option>
          </mat-select>
        </mat-form-field>
      </div>

      <mat-progress-bar *ngIf="loading" mode="indeterminate"></mat-progress-bar>

      <mat-table [dataSource]="items" class="data-table">
        <ng-container matColumnDef="pedidoId">
          <mat-header-cell *matHeaderCellDef>Pedido ID</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.pedidoId }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="referenciaPedido">
          <mat-header-cell *matHeaderCellDef>Referencia</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.referenciaPedido }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="destinatarioNombre">
          <mat-header-cell *matHeaderCellDef>Destinatario</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.destinatarioNombre }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="ciudad">
          <mat-header-cell *matHeaderCellDef>Ciudad</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.ciudad }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="cantidad">
          <mat-header-cell *matHeaderCellDef>Cantidad</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.cantidad }}</mat-cell>
        </ng-container>

        <ng-container matColumnDef="estado">
          <mat-header-cell *matHeaderCellDef>Estado</mat-header-cell>
          <mat-cell *matCellDef="let element">
            <span [ngClass]="'status-' + element.estado.toLowerCase().replace(' ', '-')">
              {{ element.estado }}
            </span>
          </mat-cell>
        </ng-container>

        <ng-container matColumnDef="fechaPedido">
          <mat-header-cell *matHeaderCellDef>Fecha</mat-header-cell>
          <mat-cell *matCellDef="let element">{{ element.fechaPedido | date: 'short' }}</mat-cell>
        </ng-container>

        <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
        <mat-row *matRowDef="let row; columns: displayedColumns;"></mat-row>
      </mat-table>

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

    .filters {
      display: flex;
      gap: 16px;
      margin-bottom: 16px;

      mat-form-field {
        flex: 1;
      }
    }

    .search-field {
      flex: 2;
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

    .status-pendiente {
      color: #FF9800;
      font-weight: 500;
    }

    .status-en-tránsito {
      color: #2196F3;
      font-weight: 500;
    }

    .status-entregado {
      color: #4CAF50;
      font-weight: 500;
    }

    .status-rechazado {
      color: #F44336;
      font-weight: 500;
    }
  `]
})
export class MediapostPedidosListComponent implements OnInit {
  items: MediapostPedido[] = [];
  loading = false;
  total = 0;
  pageSize = 25;
  currentPage = 1;
  searchControl = new FormControl('');
  estadoControl = new FormControl('');
  displayedColumns = ['pedidoId', 'referenciaPedido', 'destinatarioNombre', 'ciudad', 'cantidad', 'estado', 'fechaPedido'];

  constructor(private mediapostService: MediapostService) { }

  ngOnInit(): void {
    this.loadPedidos();
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.currentPage = 1;
        this.loadPedidos();
      });

    this.estadoControl.valueChanges.subscribe(() => {
      this.currentPage = 1;
      this.loadPedidos();
    });
  }

  loadPedidos(): void {
    this.loading = true;
    this.mediapostService.getPedidos(
      this.currentPage,
      this.pageSize,
      this.searchControl.value || '',
      this.estadoControl.value || ''
    ).subscribe({
      next: (result) => {
        this.items = result.items;
        this.total = result.total;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading pedidos', err);
        this.loading = false;
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageSize = event.pageSize;
    this.currentPage = event.pageIndex + 1;
    this.loadPedidos();
  }
}
