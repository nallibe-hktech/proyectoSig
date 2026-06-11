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
import { MediapostService } from '../services/mediapost.service';
import { MediapostPedidosListComponent } from './mediapost-pedidos-list.component';
import { MediapostRecepcionesListComponent } from './mediapost-recepciones-list.component';
import { ChartConfiguration } from 'chart.js';

interface MediapostDashboard {
  pedidosTotal: number;
  pedidosEntregados: number;
  pedidosPendientes: number;
  pedidosRechazados: number;
  tasaEntrega: number;
  recepcionesTotal: number;
  unidadesRecibidas: number;
  unidadesDestrozadas: number;
  costoDistribucion: number;
  pedidosPendientesDetalle: Array<{
    pedidoId: string;
    referenciaPedido: string;
    destinatarioNombre: string;
    estado: string;
    fechaPedido: string;
    diasEnTransito: number;
  }>;
}

@Component({
  selector: 'app-mediapost-dashboard',
  templateUrl: './mediapost-dashboard.component.html',
  styleUrls: ['./mediapost-dashboard.component.scss'],
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
    MediapostPedidosListComponent,
    MediapostRecepcionesListComponent
  ]
})
export class MediapostDashboardComponent implements OnInit {
  dashboard: MediapostDashboard | null = null;
  loading = false;
  dateFromControl = new FormControl(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000));
  dateToControl = new FormControl(new Date());

  deliveryChartConfig: ChartConfiguration<'doughnut'> = {
    type: 'doughnut',
    data: {
      labels: ['Entregados', 'Pendientes', 'Rechazados'],
      datasets: [
        {
          data: [0, 0, 0],
          backgroundColor: ['#4CAF50', '#FF9800', '#F44336'],
          borderColor: ['#45a049', '#e68900', '#da190b'],
          borderWidth: 2
        }
      ]
    },
    options: {
      responsive: true,
      plugins: {
        legend: {
          position: 'bottom'
        }
      }
    }
  };

  constructor(private mediapostService: MediapostService) { }

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

    this.mediapostService.getDashboard(
      `${fromDate.getFullYear()}-${String(fromDate.getMonth() + 1).padStart(2, '0')}-${String(fromDate.getDate()).padStart(2, '0')}`,
      `${toDate.getFullYear()}-${String(toDate.getMonth() + 1).padStart(2, '0')}-${String(toDate.getDate()).padStart(2, '0')}`
    ).subscribe({
      next: (data) => {
        this.dashboard = data;
        this.updateChart();
        this.loading = false;
      },
      error: (err) => {
        console.error('Error loading Mediapost dashboard', err);
        this.loading = false;
      }
    });
  }

  private updateChart(): void {
    if (this.dashboard) {
      this.deliveryChartConfig.data.datasets[0].data = [
        this.dashboard.pedidosEntregados,
        this.dashboard.pedidosPendientes,
        this.dashboard.pedidosRechazados
      ];
    }
  }

  refreshDashboard(): void {
    this.loadDashboard();
  }
}
