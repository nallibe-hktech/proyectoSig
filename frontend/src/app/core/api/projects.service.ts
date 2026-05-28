import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ProjectListItemDto, ProjectDetailDto, ProjectCreateRequest, ProjectUpdateRequest,
  PagedResult,
} from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/projects`;

  list(page: number, pageSize: number, clientId?: number | null, search?: string) {
    return this.http.get<PagedResult<ProjectListItemDto>>(this.base, {
      params: toHttpParams({ page, pageSize, clientId, search }),
    });
  }
  getById(id: number) { return this.http.get<ProjectDetailDto>(`${this.base}/${id}`); }
  create(req: ProjectCreateRequest) { return this.http.post<ProjectDetailDto>(this.base, req); }
  update(id: number, req: ProjectUpdateRequest) { return this.http.put<ProjectDetailDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }
}
