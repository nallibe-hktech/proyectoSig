import { Component, ElementRef, ViewChild, input, effect, AfterViewInit, OnDestroy } from '@angular/core';
import { Chart, ChartConfiguration, ChartData, registerables } from 'chart.js';

Chart.register(...registerables);

export interface ChartSlice { label: string; value: number; color: string; }

@Component({
  selector: 'sig-pie-chart',
  standalone: true,
  template: `<canvas #canvas data-testid="pie-chart"></canvas>`,
  styles: [`
    :host { display: block; width: 100%; height: 240px; }
    canvas { max-width: 100%; max-height: 100%; }
  `],
})
export class PieChartComponent implements AfterViewInit, OnDestroy {
  @ViewChild('canvas', { static: true }) canvas!: ElementRef<HTMLCanvasElement>;
  readonly slices = input.required<ChartSlice[]>();
  private chart: Chart | null = null;

  constructor() {
    effect(() => {
      this.slices(); // dependency
      this.renderChart();
    });
  }

  ngAfterViewInit(): void { this.renderChart(); }

  ngOnDestroy(): void {
    this.chart?.destroy();
    this.chart = null;
  }

  private renderChart(): void {
    if (!this.canvas) return;
    const slices = this.slices();
    if (!slices || slices.length === 0) return;
    const data: ChartData = {
      labels: slices.map((s) => s.label),
      datasets: [{
        data: slices.map((s) => s.value),
        backgroundColor: slices.map((s) => s.color),
        borderColor: '#FFFFFF',
        borderWidth: 2,
      }],
    };
    const config: ChartConfiguration = {
      type: 'doughnut',
      data,
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'right', labels: { font: { family: 'Inter', size: 12 } } },
          tooltip: { titleFont: { family: 'Inter' }, bodyFont: { family: 'Inter' } },
        },
      },
    };
    if (this.chart) {
      this.chart.data = data;
      this.chart.update();
    } else {
      this.chart = new Chart(this.canvas.nativeElement, config);
    }
  }
}
