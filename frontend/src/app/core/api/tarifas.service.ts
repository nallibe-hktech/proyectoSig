import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  TarifaProyectoDto, TarifaProyectoCreateRequest, TarifaProyectoUpdateRequest,
} from '../../models/dtos';

@Injectable({ providedIn: 'root' })
export class TarifasService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/projects`;

  list(projectId: number) {
    return this.http.get<TarifaProyectoDto[]>(`${this.base}/${projectId}/tarifas`);
  }

  getById(id: number, projectId: number) {
    return this.http.get<TarifaProyectoDto>(`${this.base}/${projectId}/tarifas/${id}`);
  }

  create(projectId: number, req: TarifaProyectoCreateRequest) {
    return this.http.post<TarifaProyectoDto>(`${this.base}/${projectId}/tarifas`, req);
  }

  update(id: number, projectId: number, req: TarifaProyectoUpdateRequest) {
    return this.http.put<TarifaProyectoDto>(`${this.base}/${projectId}/tarifas/${id}`, req);
  }

  delete(id: number, projectId: number) {
    return this.http.delete<void>(`${this.base}/${projectId}/tarifas/${id}`);
  }
}
