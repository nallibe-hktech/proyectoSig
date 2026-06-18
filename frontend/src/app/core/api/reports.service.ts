import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ReporteResultadoDto, PrevisionRealDto } from '../../models/dtos';
import { toHttpParams } from './api.helpers';

export interface ReportFilters {
  departmentId?: number;
  clientId?: number;
  serviceId?: number;
}

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/reports`;

  // Informes nativos (PPT slide 23) — no Power BI.
  resultado(anio: number, filters?: ReportFilters) {
    return this.http.get<ReporteResultadoDto>(`${this.base}/resultado`, { params: toHttpParams({ anio, ...filters }) });
  }
  previsionVsReal(anio: number, filters?: ReportFilters) {
    return this.http.get<PrevisionRealDto>(`${this.base}/prevision-vs-real`, { params: toHttpParams({ anio, ...filters }) });
  }
}
