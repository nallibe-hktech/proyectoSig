import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  TarifaServicioDto, TarifaServicioCreateRequest, TarifaServicioUpdateRequest,
} from '../../models/dtos';

@Injectable({ providedIn: 'root' })
export class TarifasService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/services`;

  list(serviceId: number) {
    return this.http.get<TarifaServicioDto[]>(`${this.base}/${serviceId}/tarifas`);
  }

  getById(id: number, serviceId: number) {
    return this.http.get<TarifaServicioDto>(`${this.base}/${serviceId}/tarifas/${id}`);
  }

  create(serviceId: number, req: TarifaServicioCreateRequest) {
    return this.http.post<TarifaServicioDto>(`${this.base}/${serviceId}/tarifas`, req);
  }

  update(id: number, serviceId: number, req: TarifaServicioUpdateRequest) {
    return this.http.put<TarifaServicioDto>(`${this.base}/${serviceId}/tarifas/${id}`, req);
  }

  delete(id: number, serviceId: number) {
    return this.http.delete<void>(`${this.base}/${serviceId}/tarifas/${id}`);
  }
}
