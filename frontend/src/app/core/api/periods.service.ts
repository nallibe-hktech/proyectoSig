import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { PeriodDto, PeriodCreateRequest, PeriodUpdateRequest, PagedResult } from '../../models/dtos';

const PERIODO_ACTIVO_KEY = 'sig_periodo_activo';

// PeriodService — CRUD periods + selector global persistente en sessionStorage.
@Injectable({ providedIn: 'root' })
export class PeriodService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/periods`;

  // Selector global persistente (compartido por AppBar y features)
  private readonly _activeId = signal<number | null>(this.readStoredActive());
  readonly activeId = this._activeId.asReadonly();

  setActive(id: number | null): void {
    this._activeId.set(id);
    if (id === null) {
      sessionStorage.removeItem(PERIODO_ACTIVO_KEY);
    } else {
      sessionStorage.setItem(PERIODO_ACTIVO_KEY, String(id));
    }
  }

  list() { return this.http.get<PeriodDto[]>(this.base); }
  listPaginated(page: number, pageSize: number) { return this.http.get<PagedResult<PeriodDto>>(`${this.base}/paginated?page=${page}&pageSize=${pageSize}`); }
  getActivo() { return this.http.get<PeriodDto>(`${this.base}/activo`).pipe(
    tap((p) => { if (this._activeId() === null) this.setActive(p.id); }),
  ); }
  getById(id: number) { return this.http.get<PeriodDto>(`${this.base}/${id}`); }
  create(req: PeriodCreateRequest) { return this.http.post<PeriodDto>(this.base, req); }
  update(id: number, req: PeriodUpdateRequest) { return this.http.put<PeriodDto>(`${this.base}/${id}`, req); }
  cerrar(id: number) { return this.http.post<PeriodDto>(`${this.base}/${id}/cerrar`, {}); }
  reabrir(id: number) { return this.http.post<PeriodDto>(`${this.base}/${id}/reabrir`, {}); }

  private readStoredActive(): number | null {
    const raw = sessionStorage.getItem(PERIODO_ACTIVO_KEY);
    return raw ? Number(raw) : null;
  }
}
