import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ConceptListItemDto, ConceptDetailDto, ConceptCreateRequest, ConceptUpdateRequest,
  PagedResult,
} from '../../models/dtos';
import { TipoConcepto } from '../../models/enums';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ConceptService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/concepts`;

  list(page: number, pageSize: number, tipo?: TipoConcepto | null, search?: string) {
    return this.http.get<PagedResult<ConceptListItemDto>>(this.base, {
      params: toHttpParams({ page, pageSize, tipo, search }),
    });
  }
  listPaginated(page: number, pageSize: number, tipo?: TipoConcepto | null, search?: string) {
    return this.http.get<PagedResult<ConceptListItemDto>>(`${this.base}/paginated`, {
      params: toHttpParams({ page, pageSize, tipo, search }),
    });
  }
  getById(id: number) { return this.http.get<ConceptDetailDto>(`${this.base}/${id}`); }
  create(req: ConceptCreateRequest) { return this.http.post<ConceptDetailDto>(this.base, req); }
  update(id: number, req: ConceptUpdateRequest) { return this.http.put<ConceptDetailDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }
  validateFormula(id: number, formulaJson: string) {
    return this.http.post<{ ok: boolean; errores: string[] }>(`${this.base}/${id}/validar-formula`, { formulaJson });
  }
}
