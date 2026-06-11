import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface StagingPayHawkGasto {
  id: number;
  gastoIdExterno: string;
  userId: number;
  projectId: number;
  fecha: string;
  importe: number;
  categoria: string;
  fechaUltimaSincronizacion: string;
}

@Injectable({ providedIn: 'root' })
export class PayHawkService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/payhawk`;

  getGastos(
    search?: string,
    desde?: string,
    hasta?: string
  ): Observable<StagingPayHawkGasto[]> {
    let url = `${this.apiUrl}/gastos`;
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    if (desde) params.append('desde', desde);
    if (hasta) params.append('hasta', hasta);

    if (params.toString()) {
      url += `?${params.toString()}`;
    }
    return this.http.get<StagingPayHawkGasto[]>(url);
  }
}
