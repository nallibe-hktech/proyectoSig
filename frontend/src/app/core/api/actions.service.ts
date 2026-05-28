import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ActionListItemDto, ActionDetailDto, ActionCreateRequest, ActionUpdateRequest,
  PagedResult,
} from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ActionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/actions`;

  list(page: number, pageSize: number, projectId?: number | null, search?: string) {
    return this.http.get<PagedResult<ActionListItemDto>>(this.base, {
      params: toHttpParams({ page, pageSize, projectId, search }),
    });
  }
  getById(id: number) { return this.http.get<ActionDetailDto>(`${this.base}/${id}`); }
  create(req: ActionCreateRequest) { return this.http.post<ActionDetailDto>(this.base, req); }
  update(id: number, req: ActionUpdateRequest) { return this.http.put<ActionDetailDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }
}
