import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface GalanEntradaDto {
  codigoArticulo: string;
  codigoDepartamento: string;
  codigoFamilia: string;
  descripcion: string;
  fecha: string;
  unidades: number;
  empresa: string;
  almacen: string;
  celda: string;
}

export interface GalanSalidaDto {
  albaran: string;
  numeroPedidoTercero: string;
  codigoArticulo: string;
  codigoDepartamento: string;
  codigoFamilia: string;
  descripcion: string;
  unidades: number;
  codigoTransporte: string;
  matricula: string;
  fecha: string;
  destinatario: string;
  almacen: string;
  celda: string;
}

export interface GalanStockDto {
  codigoArticulo: string;
  codigoDepartamento: string;
  codigoFamilia: string;
  codigoCelda: string;
  stockB: number;
  stockA: number;
  stock: number;
  almacen: string;
  familia: string;
  subFamilia: string;
  descripcion: string;
}

export interface GalanDashboardDto {
  stockTotalValue: number;
  entradasCount: number;
  salidasCount: number;
  costoLogisticoTotal: number;
  articulosDiferentes: number;
  volumenMovido: number;
  alertasStockBajo: Array<{
    codigoArticulo: string;
    descripcion: string;
    stockActual: number;
    umbraloAlerta: number;
  }>;
}

@Injectable({
  providedIn: 'root'
})
export class GalanService {
  private readonly apiUrl = `${environment.apiUrl}/galan`;

  constructor(private http: HttpClient) { }

  getEntradas(page: number, pageSize: number, search: string = ''): Observable<PagedResult<GalanEntradaDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResult<GalanEntradaDto>>(`${this.apiUrl}/entradas`, { params });
  }

  getSalidas(page: number, pageSize: number, search: string = ''): Observable<PagedResult<GalanSalidaDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResult<GalanSalidaDto>>(`${this.apiUrl}/salidas`, { params });
  }

  getStock(): Observable<GalanStockDto[]> {
    return this.http.get<GalanStockDto[]>(`${this.apiUrl}/stock`);
  }

  getEntradaById(id: number): Observable<GalanEntradaDto> {
    return this.http.get<GalanEntradaDto>(`${this.apiUrl}/entradas/${id}`);
  }

  getSalidaById(id: number): Observable<GalanSalidaDto> {
    return this.http.get<GalanSalidaDto>(`${this.apiUrl}/salidas/${id}`);
  }

  getStockById(id: number): Observable<GalanStockDto> {
    return this.http.get<GalanStockDto>(`${this.apiUrl}/stock/${id}`);
  }

  getDashboard(desde: string, hasta: string): Observable<GalanDashboardDto> {
    const params = new HttpParams()
      .set('desde', desde)
      .set('hasta', hasta);

    return this.http.get<GalanDashboardDto>(`${this.apiUrl}/dashboard`, { params });
  }
}
