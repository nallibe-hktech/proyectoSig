import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ConceptListItemDto, ConceptDetailDto, ConceptCreateRequest, ConceptUpdateRequest,
  PagedResult, AuditLogDto, TarifaConceptoDto, TarifaConceptoCreateRequest, TarifaConceptoUpdateRequest,
  PresupuestoConceptoDto, PresupuestoConceptoCreateRequest, PresupuestoConceptoUpdateRequest,
} from '../../models/dtos';
import { TipoConcepto } from '../../models/enums';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ConceptService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/concepts`;
  private readonly variablesBase = `${environment.apiUrl}/variables`;

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
  getHistorial(id: number, page = 1, pageSize = 20) {
    return this.http.get<PagedResult<AuditLogDto>>(`${this.base}/${id}/historial`, {
      params: toHttpParams({ page, pageSize }),
    });
  }

  // FASE 2: Tarifas por Concepto
  getTarifas(conceptId: number) {
    return this.http.get<TarifaConceptoDto[]>(`${this.base}/${conceptId}/tarifas`);
  }
  getTarifasPaginated(conceptId: number, page: number, pageSize: number, search?: string) {
    return this.http.get<PagedResult<TarifaConceptoDto>>(`${this.base}/${conceptId}/tarifas/paginated`, {
      params: toHttpParams({ page, pageSize, search }),
    });
  }
  getTarifa(conceptId: number, tarifaId: number) {
    return this.http.get<TarifaConceptoDto>(`${this.base}/${conceptId}/tarifas/${tarifaId}`);
  }
  createTarifa(conceptId: number, req: TarifaConceptoCreateRequest) {
    return this.http.post<TarifaConceptoDto>(`${this.base}/${conceptId}/tarifas`, req);
  }
  updateTarifa(conceptId: number, tarifaId: number, req: TarifaConceptoUpdateRequest) {
    return this.http.put<TarifaConceptoDto>(`${this.base}/${conceptId}/tarifas/${tarifaId}`, req);
  }
  deleteTarifa(conceptId: number, tarifaId: number) {
    return this.http.delete<void>(`${this.base}/${conceptId}/tarifas/${tarifaId}`);
  }

  // FASE 2: Presupuestos por Concepto
  getPresupuestos(conceptId: number) {
    return this.http.get<PresupuestoConceptoDto[]>(`${this.base}/${conceptId}/presupuestos`);
  }
  getPresupuestosPaginated(conceptId: number, page: number, pageSize: number, search?: string) {
    return this.http.get<PagedResult<PresupuestoConceptoDto>>(`${this.base}/${conceptId}/presupuestos/paginated`, {
      params: toHttpParams({ page, pageSize, search }),
    });
  }
  getPresupuesto(conceptId: number, presupuestoId: number) {
    return this.http.get<PresupuestoConceptoDto>(`${this.base}/${conceptId}/presupuestos/${presupuestoId}`);
  }
  createPresupuesto(conceptId: number, req: PresupuestoConceptoCreateRequest) {
    return this.http.post<PresupuestoConceptoDto>(`${this.base}/${conceptId}/presupuestos`, req);
  }
  updatePresupuesto(conceptId: number, presupuestoId: number, req: PresupuestoConceptoUpdateRequest) {
    return this.http.put<PresupuestoConceptoDto>(`${this.base}/${conceptId}/presupuestos/${presupuestoId}`, req);
  }
  deletePresupuesto(conceptId: number, presupuestoId: number) {
    return this.http.delete<void>(`${this.base}/${conceptId}/presupuestos/${presupuestoId}`);
  }

  // Variables for formula editor
  getVariables() {
    return this.http.get<PagedResult<any>>(`${this.variablesBase}/paginated`, {
      params: toHttpParams({ page: 1, pageSize: 1000 }),
    });
  }
}
