import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../models/dtos';

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

export interface SgpvVisitaDashboardDto {
  visitaIdExterno: string;
  resourceNif: string;
  centroId: string;
  centroNombre?: string;
  serviceName?: string;
  fecha: string; // DateOnly serialized as string
  horasDuracion?: number;
  gpvNombre?: string;
}

export interface SgpvCentroDashboardDto {
  centroId: string;
  centroNombre?: string;
  provincia?: string;
  ciudad?: string;
}

@Injectable({ providedIn: 'root' })
export class SgpvService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/sgpv`;

  getProductos(page: number, pageSize: number, search?: string): Observable<PagedResult<SgpvProductoDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<PagedResult<SgpvProductoDto>>(`${this.base}/productos/paginated`, { params });
  }

  getVisitas(page: number, pageSize: number, search?: string): Observable<PagedResult<SgpvVisitaDashboardDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<PagedResult<SgpvVisitaDashboardDto>>(`${this.base}/visitas/paginated`, { params });
  }

  getCentros(page: number, pageSize: number, search?: string): Observable<PagedResult<SgpvCentroDashboardDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<PagedResult<SgpvCentroDashboardDto>>(`${this.base}/centros/paginated`, { params });
  }
}
