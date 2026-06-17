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

export interface MediapostPedidoDto {
  pedidoId: string;
  referenciaPedido: string;
  codigoArticulo: string;
  fechaPedido: string;
  cantidad: number;
  estado: string;
  destinatarioNombre: string;
  direccionEntrega: string;
  codigoPostal: string;
  ciudad: string;
  provincia: string;
}

export interface MediapostRecepcionDto {
  recepcionId: string;
  referenciaRecepcion: string;
  codigoArticulo: string;
  fechaRecepcion: string;
  cantidad: number;
  cantidadDañada: number;
  estado: string;
  almacen: string;
  observaciones: string;
}

export interface MediapostDashboardDto {
  pedidosTotal: number;
  pedidosEntregados: number;
  pedidosPendientes: number;
  pedidosRechazados: number;
  tasaEntrega: number;
  recepcionesTotal: number;
  unidadesRecibidas: number;
  unidadesDestrozadas: number;
  costoDistribucion: number;
  pedidosPendientesDetalle: Array<{
    pedidoId: string;
    referenciaPedido: string;
    destinatarioNombre: string;
    estado: string;
    fechaPedido: string;
    diasEnTransito: number;
  }>;
}

export interface FileSyncResultDto {
  tipoArchivo: string;
  exito: boolean;
  registrosInsertados: number;
  registrosActualizados: number;
  registrosDuplicados: number;
  registrosError: number;
  mensajeError?: string;
  fechaSincronizacion?: string;
  detallesErrores?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class MediapostService {
  private readonly apiUrl = `${environment.apiUrl}/mediapost`;

  constructor(private http: HttpClient) { }

  getPedidos(page: number, pageSize: number, search: string = '', estado: string = ''): Observable<PagedResult<MediapostPedidoDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    if (estado) {
      params = params.set('estado', estado);
    }

    return this.http.get<PagedResult<MediapostPedidoDto>>(`${this.apiUrl}/pedidos`, { params });
  }

  getRecepciones(page: number, pageSize: number, search: string = ''): Observable<PagedResult<MediapostRecepcionDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResult<MediapostRecepcionDto>>(`${this.apiUrl}/recepciones`, { params });
  }

  getPedidoById(id: number): Observable<MediapostPedidoDto> {
    return this.http.get<MediapostPedidoDto>(`${this.apiUrl}/pedidos/${id}`);
  }

  getRecepcionById(id: number): Observable<MediapostRecepcionDto> {
    return this.http.get<MediapostRecepcionDto>(`${this.apiUrl}/recepciones/${id}`);
  }

  getDashboard(desde: string, hasta: string): Observable<MediapostDashboardDto> {
    const params = new HttpParams()
      .set('desde', desde)
      .set('hasta', hasta);

    return this.http.get<MediapostDashboardDto>(`${this.apiUrl}/dashboard`, { params });
  }

  uploadFile(tipo: string, file: File): Observable<unknown> {
    const formData = new FormData();
    formData.append('file', file);
    const params = new HttpParams().set('tipo', tipo);
    return this.http.post(`${this.apiUrl}/upload`, formData, { params });
  }
}
