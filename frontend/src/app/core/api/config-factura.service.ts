import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  CategoriaFacturaDto, CategoriaFacturaCreateRequest, CategoriaFacturaUpdateRequest,
  ConceptoDisponibleDto, ConfigFacturaResumenDto,
} from '../../models/dtos';

// Configuración de Factura (prototipo 25/28): categorías anidadas bajo el cliente. Lectura para
// autenticados; alta/edición/borrado solo Administrator (lo aplica el backend).
@Injectable({ providedIn: 'root' })
export class ConfigFacturaService {
  private readonly http = inject(HttpClient);
  private base(clientId: number) { return `${environment.apiUrl}/clients/${clientId}/categorias-factura`; }

  list(clientId: number) {
    return this.http.get<CategoriaFacturaDto[]>(this.base(clientId));
  }
  resumen(clientId: number) {
    return this.http.get<ConfigFacturaResumenDto>(`${this.base(clientId)}/resumen`);
  }
  conceptosDisponibles(clientId: number) {
    return this.http.get<ConceptoDisponibleDto[]>(`${this.base(clientId)}/conceptos-disponibles`);
  }
  create(clientId: number, req: CategoriaFacturaCreateRequest) {
    return this.http.post<CategoriaFacturaDto>(this.base(clientId), req);
  }
  update(clientId: number, id: number, req: CategoriaFacturaUpdateRequest) {
    return this.http.put<CategoriaFacturaDto>(`${this.base(clientId)}/${id}`, req);
  }
  delete(clientId: number, id: number) {
    return this.http.delete<void>(`${this.base(clientId)}/${id}`);
  }
}
