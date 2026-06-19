import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ClienteIncidenciaDto, ClienteIncidenciaCreateRequest, IncidenciaCambioEstadoRequest,
  IncidenciaListItemDto, PagedResult,
} from '../../models/dtos';
import { EstadoIncidencia } from '../../models/enums';
import { toHttpParams } from './api.helpers';

// Incidencias (prototipo): el listado de 1er nivel cuelga de /api/incidencias (global, con filtros);
// el alta y el cambio de estado siguen siendo anidados bajo el cliente (/api/clients/{id}/incidencias).
export interface IncidenciaFiltros {
  page: number;
  pageSize: number;
  search?: string;
  clientId?: number | null;
  tipo?: string | null;
  estado?: EstadoIncidencia | null;
}

@Injectable({ providedIn: 'root' })
export class IncidenciasService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  list(f: IncidenciaFiltros) {
    return this.http.get<PagedResult<IncidenciaListItemDto>>(`${this.base}/incidencias`, {
      params: toHttpParams({
        page: f.page, pageSize: f.pageSize, search: f.search,
        clientId: f.clientId ?? undefined, tipo: f.tipo ?? undefined, estado: f.estado ?? undefined,
      }),
    });
  }

  getById(clientId: number, id: number) {
    return this.http.get<ClienteIncidenciaDto>(`${this.base}/clients/${clientId}/incidencias/${id}`);
  }

  create(clientId: number, req: ClienteIncidenciaCreateRequest) {
    return this.http.post<ClienteIncidenciaDto>(`${this.base}/clients/${clientId}/incidencias`, req);
  }

  cambiarEstado(clientId: number, id: number, req: IncidenciaCambioEstadoRequest) {
    return this.http.post<ClienteIncidenciaDto>(`${this.base}/clients/${clientId}/incidencias/${id}/estado`, req);
  }
}
