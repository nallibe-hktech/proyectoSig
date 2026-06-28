import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '../../../models/dtos';

// NOTE: Visitas structure ready for use. Currently API returns empty array,
// but when SGPV adds visitas export endpoint, this DTO is ready.
// No changes needed here - just update backend HttpClients.cs GetVisitasAsync()
export interface SgpvVisitaDto {
  id: number;
  visitaIdExterno: string;
  resourceNif: string;
  centroId: string;
  centroNombre: string;
  serviceName: string;
  fecha: string;
  horasDuracion: number;
  userId?: number;
  serviceId?: number;
  payloadJson?: string;
}

export interface SgpvProductoDto {
  id: number;
  idProducto: string;
  idCliente: string;
  cliente: string;
  categoria: string;
  subcategoria?: string;
  codigoReferencia?: string;
  referencia?: string;
  ean?: string;
  marca?: string;
  pvpRecomendado: string;
  competencia: string;
  activo: boolean;
}

@Injectable({ providedIn: 'root' })
export class SgpvService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}`;

  listVisitasPaginated(page: number, pageSize: number, search?: string) {
    const url = `${this.base}/sgpv/visitas/paginated`;
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResult<SgpvVisitaDto>>(url, { params });
  }

  listProductosPaginated(page: number, pageSize: number, search?: string) {
    const url = `${this.base}/sgpv/productos/paginated`;
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResult<SgpvProductoDto>>(url, { params });
  }
}
