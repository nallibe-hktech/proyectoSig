import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  ConfigPresupuestoDto, PartidaPresupuestoDto,
  PartidaPresupuestoCreateRequest, PartidaPresupuestoUpdateRequest, MargenObjetivoRequest,
} from '../../models/dtos';

// Configuración de Presupuesto (prototipo 24/28): partidas anidadas bajo el servicio/acción. Lectura para
// autenticados; alta/edición/borrado y margen objetivo solo Administrator (lo aplica el backend).
@Injectable({ providedIn: 'root' })
export class ConfigPresupuestoService {
  private readonly http = inject(HttpClient);
  private base(serviceId: number) { return `${environment.apiUrl}/services/${serviceId}/config-presupuesto`; }

  getConfig(serviceId: number) {
    return this.http.get<ConfigPresupuestoDto>(this.base(serviceId));
  }
  createPartida(serviceId: number, req: PartidaPresupuestoCreateRequest) {
    return this.http.post<PartidaPresupuestoDto>(`${this.base(serviceId)}/partidas`, req);
  }
  updatePartida(serviceId: number, id: number, req: PartidaPresupuestoUpdateRequest) {
    return this.http.put<PartidaPresupuestoDto>(`${this.base(serviceId)}/partidas/${id}`, req);
  }
  deletePartida(serviceId: number, id: number) {
    return this.http.delete<void>(`${this.base(serviceId)}/partidas/${id}`);
  }
  setMargenObjetivo(serviceId: number, req: MargenObjetivoRequest) {
    return this.http.put<ConfigPresupuestoDto>(`${this.base(serviceId)}/margen-objetivo`, req);
  }
}
