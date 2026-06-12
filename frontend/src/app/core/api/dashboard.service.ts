import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { DashboardKpisDto, DashboardAvisoDto, MiServicioDto } from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/dashboard`;

  getKpis(periodId?: number | null) {
    return this.http.get<DashboardKpisDto>(this.base, { params: toHttpParams({ periodId }) });
  }
  getAvisos() {
    return this.http.get<DashboardAvisoDto[]>(`${this.base}/avisos`);
  }
  getMisServicios(periodId?: number | null) {
    return this.http.get<MiServicioDto[]>(`${this.base}/mis-servicios`, { params: toHttpParams({ periodId }) });
  }
  regenerateSeed() {
    return this.http.post(`${environment.apiUrl}/dev/regenerar-seed`, {});
  }
}
