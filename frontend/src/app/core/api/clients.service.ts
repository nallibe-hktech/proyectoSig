import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ClientListItemDto, ClientDetailDto, ClientCreateRequest, ClientUpdateRequest,
  ClienteIncidenciaDto, ClienteIncidenciaCreateRequest, ClienteIncidenciaUpdateRequest,
  PlantillaClienteConceptoDto, PlantillaClienteConceptoCreateRequest, PlantillaClienteConceptoUpdateRequest,
  PagedResult,
} from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ClientService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/clients`;

  list(page: number, pageSize: number, search?: string) {
    return this.http.get<PagedResult<ClientListItemDto>>(this.base, {
      params: toHttpParams({ page, pageSize, search }),
    });
  }
  listPaginated(page: number, pageSize: number, search?: string) {
    return this.http.get<PagedResult<ClientListItemDto>>(`${this.base}/paginated`, {
      params: toHttpParams({ page, pageSize, search }),
    });
  }
  getById(id: number) { return this.http.get<ClientDetailDto>(`${this.base}/${id}`); }
  create(req: ClientCreateRequest) { return this.http.post<ClientDetailDto>(this.base, req); }
  update(id: number, req: ClientUpdateRequest) { return this.http.put<ClientDetailDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }

  // Incidencias del cliente (PPT slide 6)
  listIncidencias(clientId: number) {
    return this.http.get<ClienteIncidenciaDto[]>(`${this.base}/${clientId}/incidencias`);
  }
  createIncidencia(clientId: number, req: ClienteIncidenciaCreateRequest) {
    return this.http.post<ClienteIncidenciaDto>(`${this.base}/${clientId}/incidencias`, req);
  }
  updateIncidencia(clientId: number, id: number, req: ClienteIncidenciaUpdateRequest) {
    return this.http.put<ClienteIncidenciaDto>(`${this.base}/${clientId}/incidencias/${id}`, req);
  }
  deleteIncidencia(clientId: number, id: number) {
    return this.http.delete<void>(`${this.base}/${clientId}/incidencias/${id}`);
  }

  // FASE 3: Plantillas de Cliente-Concepto (customización por cliente)
  getPlantillasClienteConcepto(clientId: number) {
    return this.http.get<PlantillaClienteConceptoDto[]>(`${this.base}/${clientId}/plantillas-concepto`);
  }
  createPlantillaClienteConcepto(clientId: number, req: PlantillaClienteConceptoCreateRequest) {
    return this.http.post<PlantillaClienteConceptoDto>(`${this.base}/${clientId}/plantillas-concepto`, req);
  }
  updatePlantillaClienteConcepto(clientId: number, id: number, req: PlantillaClienteConceptoUpdateRequest) {
    return this.http.put<PlantillaClienteConceptoDto>(`${this.base}/${clientId}/plantillas-concepto/${id}`, req);
  }
  deletePlantillaClienteConcepto(clientId: number, id: number) {
    return this.http.delete<void>(`${this.base}/${clientId}/plantillas-concepto/${id}`);
  }
}
