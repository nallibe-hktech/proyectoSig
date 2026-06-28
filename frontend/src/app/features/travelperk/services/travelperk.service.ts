import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface TravelPerkLineaDto {
  tripId: string;
  service: string;
  costObject: string;
  ceco: string;
  travelerEmail: string;
  costSinIVA: number;
  expenseDate: string;
}

export interface TravelPerkDashboardDto {
  lineasCount: number;
  costeTotal: number;
  lineasSinImputacion: number;
}

export interface FileSyncResultDto {
  tipoArchivo: string;
  exito: boolean;
  registrosInsertados: number;
  registrosActualizados: number;
  registrosDuplicados: number;
  registrosError: number;
  mensajeError?: string;
  fechaSincronizacion?: string;
  detallesErrores?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class TravelPerkService {
  private readonly apiUrl = `${environment.apiUrl}/travelperk`;

  constructor(private http: HttpClient) { }

  listLineas(page: number, pageSize: number, search: string = ''): Observable<PagedResult<TravelPerkLineaDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResult<TravelPerkLineaDto>>(`${this.apiUrl}/lineas`, { params });
  }

  getKpis(): Observable<TravelPerkDashboardDto> {
    return this.http.get<TravelPerkDashboardDto>(`${this.apiUrl}/dashboard`);
  }

  uploadFile(file: File): Observable<FileSyncResultDto> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<FileSyncResultDto>(`${this.apiUrl}/upload`, formData);
  }
}
