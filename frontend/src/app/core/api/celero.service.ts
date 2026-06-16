import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { SyncResultDto } from '../../models/dtos';
import { toHttpParams } from './api.helpers';

// Visita Celero tal como la expone el backend.
export interface CeleroVisitaDto {
  id: number;
  visitaIdExterno: string;
  resourceNif: string;
  serviceName: string;
  missionName: string;
  fecha: string;
  userId?: number;
  serviceId?: number;
  notas?: string;
  estadoMapeo?: string;
}

export interface CeleroVisitasListResponse {
  items: CeleroVisitaDto[];
  total: number;
}

export interface CeleroVisitasQuery {
  page: number;
  pageSize: number;
  searchNif?: string;
  searchService?: string;
}

export interface CeleroVisitaUpdateRequest {
  userId?: number | null;
  serviceId?: number | null;
  notas?: string | null;
}

// Valor pendiente de mapear (recurso / servicio / misión).
export interface CeleroPendingValueDto {
  valor: string;
  cantidad: number;
  estaMapado: boolean;
  selectedId?: number;
}

export interface CeleroPendientesResponse {
  recursos: CeleroPendingValueDto[];
  servicios: CeleroPendingValueDto[];
  misiones: CeleroPendingValueDto[];
  totalVisitasSinMapear: number;
}

export interface CeleroResourceMappingRequest {
  celeroNif: string;
  userId?: number;
}

export interface CeleroServiceMappingRequest {
  celeroServiceName: string;
  serviceId?: number;
}

export interface CeleroMissionMappingRequest {
  celeroMissionName: string;
  serviceId?: number;
}

export interface CeleroReprocesarResult {
  resueltos: number;
}

@Injectable({ providedIn: 'root' })
export class CeleroService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/celero-visitas`;
  private readonly mappingsBase = `${environment.apiUrl}/celero-mappings`;

  listVisitas(query: CeleroVisitasQuery) {
    return this.http.get<CeleroVisitasListResponse>(this.base, {
      params: toHttpParams({ ...query }),
    });
  }

  updateVisita(id: number, req: CeleroVisitaUpdateRequest) {
    return this.http.put<void>(`${this.base}/${id}`, req);
  }

  getPendientes() {
    return this.http.get<CeleroPendientesResponse>(`${this.mappingsBase}/pendientes`);
  }

  createResourceMapping(req: CeleroResourceMappingRequest) {
    return this.http.post<void>(`${this.mappingsBase}/resources`, req);
  }

  createServiceMapping(req: CeleroServiceMappingRequest) {
    return this.http.post<void>(`${this.mappingsBase}/services`, req);
  }

  createMissionMapping(req: CeleroMissionMappingRequest) {
    return this.http.post<void>(`${this.mappingsBase}/missions`, req);
  }

  reprocesar() {
    return this.http.post<CeleroReprocesarResult>(`${this.mappingsBase}/reprocesar`, {});
  }

  syncCelero() {
    return this.http.post<SyncResultDto>(`${environment.apiUrl}/sync/celero`, {});
  }
}
