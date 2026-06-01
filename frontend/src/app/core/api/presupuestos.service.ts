import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  PresupuestoProyectoDto, PresupuestoProyectoCreateRequest, PresupuestoProyectoUpdateRequest,
} from '../../models/dtos';

@Injectable({ providedIn: 'root' })
export class PresupuestosService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/projects`;

  list(projectId: number) {
    return this.http.get<PresupuestoProyectoDto[]>(`${this.base}/${projectId}/presupuestos`);
  }

  getById(id: number, projectId: number) {
    return this.http.get<PresupuestoProyectoDto>(`${this.base}/${projectId}/presupuestos/${id}`);
  }

  create(projectId: number, req: PresupuestoProyectoCreateRequest) {
    return this.http.post<PresupuestoProyectoDto>(`${this.base}/${projectId}/presupuestos`, req);
  }

  update(id: number, projectId: number, req: PresupuestoProyectoUpdateRequest) {
    return this.http.put<PresupuestoProyectoDto>(`${this.base}/${projectId}/presupuestos/${id}`, req);
  }

  delete(id: number, projectId: number) {
    return this.http.delete<void>(`${this.base}/${projectId}/presupuestos/${id}`);
  }
}
