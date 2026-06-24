import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { toHttpParams } from './api.helpers';

// Línea de TravelPerk tal como la expone el backend (hoja "report" de la descarga Excel).
export interface TravelPerkLineaDto {
  id: number;
  tripId: string;
  service: string;
  costObject?: string | null;
  ceco: string;
  serviceId?: number | null;
  costeSinIVA: number;
  fechaGasto?: string | null;
  travelerEmail?: string | null;
  currency?: string | null;
  esGastoInternoSig: boolean;
  cecoNoMaestro: boolean;
  fechaUltimaSincronizacion: string;
}

export interface TravelPerkKpisDto {
  totalLineas: number;
  totalSinIVA: number;
  lineasImputadas: number;
  costeImputado: number;
  lineasGastoInternoSig: number;
  costeGastoInternoSig: number;
  lineasCecoNoMaestro: number;
}

export interface TravelPerkLineasResponse {
  items: TravelPerkLineaDto[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class TravelPerkService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/travelperk`;

  getLineas(page: number, pageSize: number, search?: string, soloNoMaestro = false) {
    return this.http.get<TravelPerkLineasResponse>(`${this.base}/lineas`, {
      params: toHttpParams({ page, pageSize, search: search || undefined, soloNoMaestro: soloNoMaestro || undefined }),
    });
  }

  getDashboard() {
    return this.http.get<TravelPerkKpisDto>(`${this.base}/dashboard`);
  }

  // Sube el Excel de TravelPerk y dispara la sincronización en el backend.
  upload(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ success: boolean; mensaje: string; sync: { registrosInsertados: number } }>(
      `${this.base}/upload`, formData);
  }
}
