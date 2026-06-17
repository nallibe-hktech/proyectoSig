import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ClientListItemDto, ClientDetailDto, ClientCreateRequest, ClientUpdateRequest,
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
}
