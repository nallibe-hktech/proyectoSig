import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { DashboardKpisDto, DashboardAvisoDto, MiProyectoDto } from '../../models/dtos';
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
  getMisProyectos(periodId?: number | null) {
    return this.http.get<MiProyectoDto[]>(`${this.base}/mis-proyectos`, { params: toHttpParams({ periodId }) });
  }
}
