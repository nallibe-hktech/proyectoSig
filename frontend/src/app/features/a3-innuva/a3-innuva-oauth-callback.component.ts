import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-a3-innuva-oauth-callback',
  standalone: true,
  template: `
    <div class="oauth-callback-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Autenticación Wolters Kluwer</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <div *ngIf="isProcessing; else resultContent" class="processing">
            <mat-spinner></mat-spinner>
            <p>Procesando autorización...</p>
          </div>

          <ng-template #resultContent>
            <div [ngClass]="success ? 'success' : 'error'">
              <h3>{{ success ? '✅ Autorización exitosa' : '❌ Error en la autorización' }}</h3>
              <p class="message">{{ message }}</p>

              <div class="actions">
                <button mat-raised-button color="primary" (click)="goToDashboard()">
                  {{ success ? 'Ir al Dashboard' : 'Reintentar' }}
                </button>
              </div>
            </div>
          </ng-template>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .oauth-callback-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    }

    mat-card {
      width: 100%;
      max-width: 500px;
      box-shadow: 0 10px 40px rgba(0,0,0,0.2);
    }

    mat-card-header {
      padding: 20px;
      background: #f5f5f5;
      border-bottom: 1px solid #ddd;
    }

    mat-card-content {
      padding: 40px 20px;
    }

    .processing {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 20px;
    }

    .success, .error {
      text-align: center;
    }

    .success h3 {
      color: #4caf50;
      font-size: 20px;
      margin-bottom: 10px;
    }

    .error h3 {
      color: #f44336;
      font-size: 20px;
      margin-bottom: 10px;
    }

    .message {
      font-size: 14px;
      color: #666;
      margin-bottom: 30px;
      line-height: 1.6;
    }

    .actions {
      display: flex;
      gap: 10px;
      justify-content: center;
    }

    button {
      min-width: 150px;
    }
  `],
  imports: [
    CommonModule,
    MatProgressSpinnerModule,
    MatCardModule,
    MatButtonModule
  ]
})
export class A3InnuvaOAuthCallbackComponent implements OnInit {
  isProcessing = true;
  success = false;
  message = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.handleCallback();
  }

  private handleCallback(): void {
    // Capturar el code de la URL
    this.route.queryParams.subscribe(params => {
      const code = params['code'];
      const error = params['error'];
      const state = params['state'];

      if (error) {
        this.success = false;
        this.message = `Error de autorización: ${error}`;
        this.isProcessing = false;
        return;
      }

      if (!code) {
        this.success = false;
        this.message = 'No se recibió código de autorización. Intenta de nuevo.';
        this.isProcessing = false;
        return;
      }

      // Enviar el código al backend para intercambiar por token
      this.exchangeCodeForToken(code);
    });
  }

  private exchangeCodeForToken(code: string): void {
    // ⚠️ IMPORTANTE: El redirect_uri debe coincidir exactamente con el que está registrado en WK
    // NO usamos window.location.origin porque eso sería http://localhost:4200
    // El redirect_uri debe ser https://localhost:43971/Login (que está registrado en WK)
    // El backend maneja eso automáticamente desde appsettings.json

    const url = `${environment.apiUrl}/a3-innuva-nominas/oauth/callback?code=${encodeURIComponent(code)}`;

    this.http.post<any>(url, {}).subscribe({
      next: (response) => {
        this.success = true;
        this.message = response.message || '✅ Token obtenido correctamente. Puedes sincronizar datos ahora.';
        this.isProcessing = false;

        // Redirigir al dashboard después de 2 segundos
        setTimeout(() => this.goToDashboard(), 2000);
      },
      error: (error) => {
        this.success = false;
        this.message = `Error al obtener token: ${error.error?.error || error.message || 'Error desconocido'}`;
        this.isProcessing = false;
      }
    });
  }

  goToDashboard(): void {
    this.router.navigate(['/a3-innuva']);
  }
}
