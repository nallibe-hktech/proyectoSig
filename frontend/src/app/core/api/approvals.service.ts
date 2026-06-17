import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApprovalFilterRequest, CierrePanelItemDto, PagedResult } from '../../models/dtos';
import { toHttpParams } from './api.helpers';

// Ola 3b (#10): el panel de aprobaciones agrega AMBOS tipos de cierre (CierreCostes + CierreFacturacion).
// Cada item indica su TipoCierre. Las acciones por cierre (aprobar/rechazar) viven en CierresService.
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
}
