import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApprovalFilterRequest, ApprovalPanelItemDto, PagedResult } from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ApprovalService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/approvals`;

  list(filter: ApprovalFilterRequest) {
    return this.http.get<PagedResult<ApprovalPanelItemDto>>(this.base, {
      params: toHttpParams({ ...filter }),
    });
  }

  pendientes(page = 1, pageSize = 25) {
    return this.http.get<PagedResult<ApprovalPanelItemDto>>(`${this.base}/pendientes`, {
      params: toHttpParams({ page, pageSize }),
    });
  }

  batchApprove(ids: number[]) {
    return this.http.post<void>(`${this.base}/batch/aprobar`, { ids });
  }

  batchReject(ids: number[]) {
    return this.http.post<void>(`${this.base}/batch/rechazar`, { ids });
  }
}
