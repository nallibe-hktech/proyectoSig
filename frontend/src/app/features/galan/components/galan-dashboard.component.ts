import { Component, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { GalanService } from '../services/galan.service';
import { GalanEntradasListComponent } from './galan-entradas-list.component';
import { GalanSalidasListComponent } from './galan-salidas-list.component';
import { GalanStockListComponent } from './galan-stock-list.component';
import { ChartConfiguration } from 'chart.js';

interface GalanDashboard {
  stockTotalValue: number;
  entradasCount: number;
  salidasCount: number;
  costoLogisticoTotal: number;
  articulosDiferentes: number;
  volumenMovido: number;
  alertasStockBajo: Array<{
    codigoArticulo: string;
    descripcion: string;
    stockActual: number;
    umbraloAlerta: number;
  }>;
}

@Component({
  selector: 'app-galan-dashboard',
  templateUrl: './galan-dashboard.component.html',
  styleUrls: ['./galan-dashboard.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    GalanEntradasListComponent,
    GalanSalidasListComponent,
    GalanStockListComponent
  ]
})
export class GalanDashboardComponent implements OnInit {
  dashboard: GalanDashboard | null = null;
  loading = false;
  dateFromControl = new FormControl(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000));
  dateToControl = new FormControl(new Date());

  kpiChartConfig: ChartConfiguration<'bar'> = {
    type: 'bar',
    data: {
      labels: ['Entradas', 'Salidas', 'Artículos'],
      datasets: [
        {
          label: 'Cantidad',
          data: [0, 0, 0],
          backgroundColor: ['#4CAF50', '#FF9800', '#2196F3'],
          borderColor: ['#45a049', '#e68900', '#0b7dda'],
          borderWidth: 1
        }
      ]
    },
    options: {
      responsive: true,
      scales: {
        y: {
          beginAtZero: true
        }
      }
    }
  };

  constructor(private galanService: GalanService) { }

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    const from = this.dateFromControl.value;
    const to = this.dateToControl.value;

    if (!from || !to) return;

    this.loading = true;
    const fromDate = new Date(from);
    const toDate = new Date(to);

    this.galanService.getDashboard(
      `${fromDate.getFullYear()}-${String(fromDate.getMonth() + 1).padStart(2, '0')}-${String(fromDate.getDate()).padStart(2, '0')}`,
      `${toDate.getFullYear()}-${String(toDate.getMonth() + 1).padStart(2, '0')}-${String(toDate.getDate()).padStart(2, '0')}`
    ).subscribe({
      next: (data) => {
        this.dashboard = data;
        this.updateChart();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading Galán dashboard', err);
        this.loading = false;
      }
    });
  }

  private updateChart(): void {
    if (this.dashboard) {
      this.kpiChartConfig.data.datasets[0].data = [
        this.dashboard.entradasCount,
        this.dashboard.salidasCount,
        this.dashboard.articulosDiferentes
      ];
    }
  }

  refreshDashboard(): void {
    this.loadDashboard();
  }
}
