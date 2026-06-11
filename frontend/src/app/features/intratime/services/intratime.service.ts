import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StagingIntratimeFichaje {
  id: number;
  fichajeIdExterno: string;
  userIdExterno: string;
  userId?: number;
  entrada: string;
  salida?: string;
  horasCalculadas?: number;
  fechaUltimaSincronizacion: string;
}

@Injectable({ providedIn: 'root' })
export class IntratimeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/intratime';

  getFichajes(
    search?: string,
    desde?: string,
    hasta?: string
  ): Observable<StagingIntratimeFichaje[]> {
    let url = `${this.apiUrl}/fichajes`;
    const params = new URLSearchParams();
    if (search) params.append('search', search);
    if (desde) params.append('desde', desde);
    if (hasta) params.append('hasta', hasta);

    if (params.toString()) {
      url += `?${params.toString()}`;
    }
    return this.http.get<StagingIntratimeFichaje[]>(url);
  }
}
