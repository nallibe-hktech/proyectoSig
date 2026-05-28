import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ClosureListItemDto, ClosureDetailDto, ClosureCreateRequest, ClosureRecalcRequest,
  ClosureApproveRequest, ClosureRejectRequest, ApprovalFilterRequest,
  ApprovalHistoryDto, PagedResult,
} from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ClosureService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/closures`;

  list(filter: ApprovalFilterRequest) {
    return this.http.get<PagedResult<ClosureListItemDto>>(this.base, {
      params: toHttpParams({ ...filter }),
    });
  }
  getById(id: number) { return this.http.get<ClosureDetailDto>(`${this.base}/${id}`); }
  create(req: ClosureCreateRequest) { return this.http.post<ClosureDetailDto>(this.base, req); }

  recalcular(id: number, rowVersion: number, req: ClosureRecalcRequest) {
    return this.http.post<ClosureDetailDto>(`${this.base}/${id}/recalcular`, req, {
      headers: new HttpHeaders({ 'If-Match': String(rowVersion) }),
    });
  }
  aprobar(id: number, rowVersion: number, req: ClosureApproveRequest) {
    return this.http.post<ClosureDetailDto>(`${this.base}/${id}/aprobar`, req, {
      headers: new HttpHeaders({ 'If-Match': String(rowVersion) }),
    });
  }
  rechazar(id: number, rowVersion: number, req: ClosureRejectRequest) {
    return this.http.post<ClosureDetailDto>(`${this.base}/${id}/rechazar`, req, {
      headers: new HttpHeaders({ 'If-Match': String(rowVersion) }),
    });
  }

  historial(closureId: number) {
    return this.http.get<ApprovalHistoryDto[]>(`${environment.apiUrl}/approvals/historial/${closureId}`);
  }
}
