import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  CalculationDetailDto, SyncResultDto, ProcessingResultDto, AuditLogFilterRequest, AuditLogDto,
  VariableDto, VariableCreateRequest, VariableUpdateRequest, PagedResult,
} from '../../models/dtos';
import { toHttpParams, downloadBlob } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class CalculationService {
  private readonly http = inject(HttpClient);
  getByLine(closureLineId: number) {
    return this.http.get<CalculationDetailDto>(`${environment.apiUrl}/calculations/${closureLineId}`);
  }
}

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);
  list(filter: AuditLogFilterRequest) {
    return this.http.get<PagedResult<AuditLogDto>>(`${environment.apiUrl}/audit`, {
      params: toHttpParams({ ...filter }),
    });
  }
}

@Injectable({ providedIn: 'root' })
export class SyncService {
  private readonly http = inject(HttpClient);
  sync(system: 'celero' | 'bizneo' | 'intratime' | 'payhawk' | 'sgpv' | 'sgpv-productos' | 'galan' | 'mediapost') {
    return this.http.post<SyncResultDto>(`${environment.apiUrl}/sync/${system}`, {});
  }
  processAll() {
    return this.http.post<ProcessingResultDto>(`${environment.apiUrl}/sync/process`, {});
  }
}

@Injectable({ providedIn: 'root' })
export class ExportService {
  private readonly http = inject(HttpClient);
  exportA3Innuva(closureId: number) {
    return this.http.get(`${environment.apiUrl}/exports/a3-innuva/${closureId}`, {
      observe: 'response',
      responseType: 'blob'
    });
  }
  exportA3Erp(closureId: number) {
    return this.http.get(`${environment.apiUrl}/exports/a3-erp/${closureId}`, {
      observe: 'response',
      responseType: 'blob'
    });
  }
  saveAttachment(response: HttpResponse<Blob>, fallbackName: string): void {
    if (!response.body) return;
    const disposition = response.headers.get('Content-Disposition') ?? '';
    const match = /filename="?([^"]+)"?/i.exec(disposition);
    const filename = match?.[1] ?? fallbackName;
    downloadBlob(response.body, filename);
  }
}

@Injectable({ providedIn: 'root' })
export class VariableService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/variables`;
  list() { return this.http.get<VariableDto[]>(this.base); }
  listPaginated(page: number, pageSize: number) { return this.http.get<PagedResult<VariableDto>>(`${this.base}/paginated?page=${page}&pageSize=${pageSize}`); }
  getById(id: number) { return this.http.get<VariableDto>(`${this.base}/${id}`); }
  create(req: VariableCreateRequest) { return this.http.post<VariableDto>(this.base, req); }
  update(id: number, req: VariableUpdateRequest) { return this.http.put<VariableDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }
}
