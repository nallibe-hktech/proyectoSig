import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';

// SmokeComponent — verifica visualmente que el tema M3 SIG está funcionando.
// Ruta: /_smoke (accesible sin autenticación en Development).
// El orquestador debe abrir ng serve y navegar a /_smoke para verificación visual
// antes de pasar al Desarrollador Frontend.
@Component({
  selector: 'app-smoke',
  standalone: true,
  imports: [
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatTableModule,
    MatIconModule,
    MatChipsModule,
  ],
  template: `
    <div style="padding: 32px; max-width: 900px; margin: 0 auto;">

      <h1 style="color: var(--mat-sys-primary); font-family: 'Inter', sans-serif; font-size: 32px; font-weight: 700; margin-bottom: 8px;">
        SIG · Smoke Test
      </h1>
      <p style="color: var(--mat-sys-on-surface-variant); margin-bottom: 32px;">
        Verifica visualmente que el tema M3 navy Penpot está activo. Colores corporativos: #1F4E78 azul marino, #70AD47 verde, #FFC107 warning, #D32F2F danger.
      </p>

      <!-- Paleta de colores (extraída de Penpot) -->
      <h2 style="font-size: 20px; font-weight: 600; margin-bottom: 16px;">Paleta corporativa SIG — Penpot</h2>
      <div style="display: flex; gap: 12px; flex-wrap: wrap; margin-bottom: 32px;">
        <div style="width: 80px; height: 80px; background: #1F4E78; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 10px; text-align: center; font-weight: 600;">PRIMARY #1F4E78</div>
        <div style="width: 80px; height: 80px; background: #D6DFF3; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: #0D1B30; font-size: 10px; text-align: center; font-weight: 600;">PRIM-CONT #D6DFF3</div>
        <div style="width: 80px; height: 80px; background: #2E5C8A; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 10px; text-align: center; font-weight: 600;">SECONDARY #2E5C8A</div>
        <div style="width: 80px; height: 80px; background: #C9A961; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: #3B2800; font-size: 10px; text-align: center; font-weight: 600;">TERTIARY #C9A961</div>
        <div style="width: 80px; height: 80px; background: #70AD47; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 10px; text-align: center; font-weight: 600;">SUCCESS #70AD47</div>
        <div style="width: 80px; height: 80px; background: #FFC107; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: #3D2600; font-size: 10px; text-align: center; font-weight: 600;">WARNING #FFC107</div>
        <div style="width: 80px; height: 80px; background: #D32F2F; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 10px; text-align: center; font-weight: 600;">ERROR #D32F2F</div>
        <div style="width: 80px; height: 80px; background: #F0F4F8; border-radius: 8px; border: 1px solid #D0D0D0; display: flex; align-items: center; justify-content: center; color: #1A1A1A; font-size: 10px; text-align: center; font-weight: 600;">SURFACE #F0F4F8</div>
      </div>

      <!-- mat-card -->
      <h2 style="font-size: 20px; font-weight: 600; margin-bottom: 16px;">mat-card</h2>
      <mat-card style="margin-bottom: 24px;" data-testid="smoke-card">
        <mat-card-header>
          <mat-card-title>Tarjeta Material 3</mat-card-title>
          <mat-card-subtitle>Subtítulo de la tarjeta</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>Si esta tarjeta se ve con elevación M3, bordes redondeados y colores SIG, el tema está funcionando.</p>
        </mat-card-content>
        <mat-card-actions align="end">
          <button mat-button data-testid="smoke-btn-cancelar">Cancelar</button>
          <button mat-flat-button color="primary" data-testid="smoke-btn-guardar">Guardar</button>
        </mat-card-actions>
      </mat-card>

      <!-- mat-form-field -->
      <h2 style="font-size: 20px; font-weight: 600; margin-bottom: 16px;">mat-form-field (outline)</h2>
      <div style="display: flex; gap: 16px; flex-wrap: wrap; margin-bottom: 32px;">
        <mat-form-field>
          <mat-label>Nombre</mat-label>
          <input matInput placeholder="Ej: Alpha Foods" data-testid="smoke-input-nombre" />
          <mat-icon matSuffix aria-hidden="true">person</mat-icon>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Correo electrónico</mat-label>
          <input matInput type="email" placeholder="usuario@sig.local" data-testid="smoke-input-email" />
          <mat-error>Introduce un correo válido</mat-error>
        </mat-form-field>
      </div>

      <!-- Botones -->
      <h2 style="font-size: 20px; font-weight: 600; margin-bottom: 16px;">Botones</h2>
      <div style="display: flex; gap: 12px; flex-wrap: wrap; margin-bottom: 32px; align-items: center;">
        <button mat-flat-button color="primary" data-testid="smoke-btn-primary">
          <mat-icon>add</mat-icon>
          Primary
        </button>
        <button mat-stroked-button color="primary" data-testid="smoke-btn-outlined">Outlined</button>
        <button mat-button data-testid="smoke-btn-text">Text</button>
        <button mat-flat-button color="warn" data-testid="smoke-btn-warn">
          <mat-icon>delete</mat-icon>
          Eliminar
        </button>
        <button mat-icon-button aria-label="Editar" data-testid="smoke-btn-icon">
          <mat-icon>edit</mat-icon>
        </button>
      </div>

      <!-- Iconos Material Symbols Outlined -->
      <h2 style="font-size: 20px; font-weight: 600; margin-bottom: 16px;">Iconos (Material Symbols Outlined — M3)</h2>
      <p style="font-size: 13px; color: var(--mat-sys-on-surface-variant); margin-bottom: 16px;">
        Si los iconos se ven como outlined (sin relleno), la fuente Material Symbols Outlined está correctamente cargada.
      </p>
      <div style="display: flex; gap: 16px; flex-wrap: wrap; margin-bottom: 32px; align-items: center;">
        <mat-icon aria-hidden="true" style="font-size: 32px; width: 32px; height: 32px;">dashboard</mat-icon>
        <mat-icon aria-hidden="true" style="font-size: 32px; width: 32px; height: 32px;">groups</mat-icon>
        <mat-icon aria-hidden="true" style="font-size: 32px; width: 32px; height: 32px;">calculate</mat-icon>
        <mat-icon aria-hidden="true" style="font-size: 32px; width: 32px; height: 32px;">approval</mat-icon>
        <mat-icon aria-hidden="true" style="font-size: 32px; width: 32px; height: 32px;">lock_clock</mat-icon>
        <mat-icon aria-hidden="true" style="font-size: 32px; width: 32px; height: 32px;">trending_up</mat-icon>
        <mat-icon aria-hidden="true" style="font-size: 32px; width: 32px; height: 32px;">check_circle</mat-icon>
        <mat-icon aria-hidden="true" style="font-size: 32px; width: 32px; height: 32px;">logout</mat-icon>
      </div>

      <!-- Badges de estado -->
      <h2 style="font-size: 20px; font-weight: 600; margin-bottom: 16px;">Badges de estado del flujo</h2>
      <div style="display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 32px;">
        <span class="sig-badge sig-badge--pending-pm">Pdte. PM</span>
        <span class="sig-badge sig-badge--pending-backoffice">Pdte. Backoffice</span>
        <span class="sig-badge sig-badge--pending-fico">Pdte. Fico</span>
        <span class="sig-badge sig-badge--pending-direction">Pdte. Dirección</span>
        <span class="sig-badge sig-badge--approved">Aprobado</span>
        <span class="sig-badge sig-badge--rejected">Rechazado</span>
        <span class="sig-badge sig-badge--closed">Cerrado</span>
      </div>

      <!-- mat-table -->
      <h2 style="font-size: 20px; font-weight: 600; margin-bottom: 16px;">mat-table</h2>
      <mat-table [dataSource]="smokeData" class="sig-table" aria-label="Tabla de smoke test" data-testid="smoke-tabla" style="width: 100%; margin-bottom: 32px;">

        <ng-container matColumnDef="proyecto">
          <th mat-header-cell *matHeaderCellDef>Proyecto</th>
          <td mat-cell *matCellDef="let row">{{ row.proyecto }}</td>
        </ng-container>

        <ng-container matColumnDef="cliente">
          <th mat-header-cell *matHeaderCellDef>Cliente</th>
          <td mat-cell *matCellDef="let row">{{ row.cliente }}</td>
        </ng-container>

        <ng-container matColumnDef="importe">
          <th mat-header-cell *matHeaderCellDef>Importe</th>
          <td mat-cell *matCellDef="let row" class="mono-num">{{ row.importe }}</td>
        </ng-container>

        <ng-container matColumnDef="estado">
          <th mat-header-cell *matHeaderCellDef>Estado</th>
          <td mat-cell *matCellDef="let row">
            <span [class]="'sig-badge sig-badge--' + row.badgeClass">{{ row.estado }}</span>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
        <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
      </mat-table>

      <!-- Skeleton shimmer -->
      <h2 style="font-size: 20px; font-weight: 600; margin-bottom: 16px;">Skeleton shimmer</h2>
      <div style="margin-bottom: 32px;">
        <div class="sig-skeleton-row" style="width: 100%;"></div>
        <div class="sig-skeleton-row" style="width: 100%;"></div>
        <div class="sig-skeleton-row" style="width: 100%;"></div>
        <div class="sig-skeleton-text" style="width: 40%; margin-top: 8px;"></div>
      </div>

      <!-- KPI cards -->
      <h2 style="font-size: 20px; font-weight: 600; margin-bottom: 16px;">Tarjetas KPI</h2>
      <div style="display: flex; gap: 16px; flex-wrap: wrap; margin-bottom: 32px;">
        <mat-card class="sig-kpi-card" data-testid="kpi-smoke-1">
          <mat-card-content>
            <div class="sig-kpi-label">Cierres completados</div>
            <div class="sig-kpi-value">42</div>
            <div class="sig-kpi-trend sig-kpi-trend--up">
              <mat-icon style="font-size: 18px; width: 18px; height: 18px;" aria-hidden="true">trending_up</mat-icon>
              +12% vs. mes anterior
            </div>
          </mat-card-content>
        </mat-card>
        <mat-card class="sig-kpi-card" data-testid="kpi-smoke-2">
          <mat-card-content>
            <div class="sig-kpi-label">Facturación total</div>
            <div class="sig-kpi-value mono-num">128.450 €</div>
            <div class="sig-kpi-trend sig-kpi-trend--down">
              <mat-icon style="font-size: 18px; width: 18px; height: 18px;" aria-hidden="true">trending_down</mat-icon>
              -3% vs. mes anterior
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <p style="color: var(--mat-sys-on-surface-variant); font-size: 13px; margin-top: 32px; border-top: 1px solid var(--mat-sys-outline-variant); padding-top: 16px;">
        Smoke test completado. Si todos los elementos se ven correctamente estilizados con la paleta navy SIG, el esqueleto está listo para el Desarrollador Frontend.
      </p>
    </div>
  `,
})
export class SmokeComponent {
  protected readonly displayedColumns = ['proyecto', 'cliente', 'importe', 'estado'];
  protected readonly smokeData = [
    { proyecto: 'GPV Alpha Premium', cliente: 'Alpha Foods', importe: '4.250,00 €', estado: 'Pdte. PM', badgeClass: 'pending-pm' },
    { proyecto: 'Implantaciones Beta', cliente: 'Beta Cosmetics', importe: '7.100,00 €', estado: 'Pdte. BO', badgeClass: 'pending-backoffice' },
    { proyecto: 'Visitas Gamma', cliente: 'Gamma Retail', importe: '3.890,00 €', estado: 'Aprobado', badgeClass: 'approved' },
    { proyecto: 'Alpha Formación', cliente: 'Alpha Foods', importe: '2.100,00 €', estado: 'Rechazado', badgeClass: 'rejected' },
  ];
}
