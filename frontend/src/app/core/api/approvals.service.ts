import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApprovalFilterRequest, ApprovalHistoryDto, CierreDetailDto, CierrePanelItemDto, PagedResult } from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ApprovalService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/approvals`;

  list(filter: ApprovalFilterRequest) {
    return this.http.get<PagedResult<CierrePanelItemDto>>(this.base, {
      params: toHttpParams({ ...filter }),
    });
  }

  pendientes(page = 1, pageSize = 25) {
    return this.http.get<PagedResult<CierrePanelItemDto>>(`${this.base}/pendientes`, {
      params: toHttpParams({ page, pageSize }),
    });
  }

  historial(closureId: number) {
    return this.http.get<ApprovalHistoryDto[]>(`${this.base}/historial/${closureId}`);
  }

  batchAprobar(ids: number[]) {
    return this.http.post<CierreDetailDto[]>(`${this.base}/batch/aprobar`, { ids });
  }

  batchRechazar(ids: number[]) {
    return this.http.post<CierreDetailDto[]>(`${this.base}/batch/rechazar`, { ids });
  }
}
