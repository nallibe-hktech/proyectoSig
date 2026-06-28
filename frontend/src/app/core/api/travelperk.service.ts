import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface TravelPerkLineaDto {
  id?: number;
  tripId: string;
  service: string;
  costObject: string;
  ceco: string;
  travelerEmail: string;
  costeSinIVA: number;
  fechaGasto: string;
  serviceId?: number | null;
  esGastoInternoSig?: boolean;
  cecoNoMaestro?: boolean;
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

export interface TravelPerkUploadResultDto {
  mensaje?: string;
  sync?: {
    registrosInsertados: number;
    registrosActualizados: number;
    registrosDuplicados: number;
  };
}

@Injectable({ providedIn: 'root' })
export class TravelPerkService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/travelperk`;

  getLineas(page: number, pageSize: number, search: string = '', soloNoMaestro: boolean = false): any {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    if (soloNoMaestro) params = params.set('soloNoMaestro', 'true');
    return this.http.get<PagedResult<TravelPerkLineaDto>>(`${this.apiUrl}/lineas`, { params });
  }

  getDashboard(): any {
    return this.http.get<TravelPerkKpisDto>(`${this.apiUrl}/dashboard`);
  }

  upload(file: File): any {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<TravelPerkUploadResultDto>(`${this.apiUrl}/upload`, formData);
  }
}
