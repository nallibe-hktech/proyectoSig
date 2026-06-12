import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  PresupuestoServicioDto, PresupuestoServicioCreateRequest, PresupuestoServicioUpdateRequest,
} from '../../models/dtos';

@Injectable({ providedIn: 'root' })
export class PresupuestosService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/services`;

  list(serviceId: number) {
    return this.http.get<PresupuestoServicioDto[]>(`${this.base}/${serviceId}/presupuestos`);
  }

  getById(id: number, serviceId: number) {
    return this.http.get<PresupuestoServicioDto>(`${this.base}/${serviceId}/presupuestos/${id}`);
  }

  create(serviceId: number, req: PresupuestoServicioCreateRequest) {
    return this.http.post<PresupuestoServicioDto>(`${this.base}/${serviceId}/presupuestos`, req);
  }

  update(id: number, serviceId: number, req: PresupuestoServicioUpdateRequest) {
    return this.http.put<PresupuestoServicioDto>(`${this.base}/${serviceId}/presupuestos/${id}`, req);
  }

  delete(id: number, serviceId: number) {
    return this.http.delete<void>(`${this.base}/${serviceId}/presupuestos/${id}`);
  }
}
