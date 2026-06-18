import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ForecastDto, ForecastUpsertRequest, ForecastResumenDto } from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class ForecastService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  // Forecast por servicio (PPT slide 36)
  listByService(serviceId: number, anio: number) {
    return this.http.get<ForecastDto[]>(`${this.base}/services/${serviceId}/forecast`, {
      params: toHttpParams({ anio }),
    });
  }
  upsert(serviceId: number, req: ForecastUpsertRequest) {
    return this.http.put<ForecastDto>(`${this.base}/services/${serviceId}/forecast`, req);
  }

  // Resumen pivote (filas dpto+cliente, columnas meses)
  resumen(anio: number, filters?: { departmentId?: number; clientId?: number; serviceId?: number }) {
    return this.http.get<ForecastResumenDto>(`${this.base}/forecast/resumen`, {
      params: toHttpParams({ anio, ...filters }),
    });
  }
}
