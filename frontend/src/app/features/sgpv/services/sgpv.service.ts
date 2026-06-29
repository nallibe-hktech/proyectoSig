import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '../../../models/dtos';

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

  listProductosPaginated(page: number, pageSize: number, search?: string) {
    const url = `${this.base}/sgpv/productos/paginated`;
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<SgpvProductoDto>>(url, { params });
  }
}
