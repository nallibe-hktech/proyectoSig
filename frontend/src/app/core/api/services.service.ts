import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ServiceListItemDto, ServiceDetailDto, ServiceCreateRequest, ServiceUpdateRequest,
  PagedResult,
} from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ServiceService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/services`;

  list(page: number, pageSize: number, clientId?: number | null, search?: string) {
    return this.http.get<PagedResult<ServiceListItemDto>>(this.base, {
      params: toHttpParams({ page, pageSize, clientId, search }),
    });
  }
  getById(id: number) { return this.http.get<ServiceDetailDto>(`${this.base}/${id}`); }
  create(req: ServiceCreateRequest) { return this.http.post<ServiceDetailDto>(this.base, req); }
  update(id: number, req: ServiceUpdateRequest) { return this.http.put<ServiceDetailDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }
}
