import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { map, mergeMap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  CierreListItemDto, CierreDetailDto, CierreCreateRequest, CierreRecalcRequest,
  CierreApproveRequest, CierreRejectRequest, ApprovalFilterRequest,
  CierreHistoryDto, ClosureAlertaDto, PagedResult,
  CierreLineOverrideRequest, CierreLineIncentivoRequest,
} from '../../models/dtos';
import { TipoCierre } from '../../models/enums';
import { toHttpParams } from './api.helpers';

// Ola 3b (#10): un único servicio parametrizado por TipoCierre apuntando a
// api/cierres-costes o api/cierres-facturacion (mismo contrato de endpoints).
// Se mantiene el manejo de If-Match/RowVersion (concurrencia optimista RNF-03).
@Injectable({ providedIn: 'root' })
export class CierresService {
  private readonly http = inject(HttpClient);

  private base(tipo: TipoCierre): string {
    const segment = tipo === 'Costes' ? 'cierres-costes' : 'cierres-facturacion';
    return `${environment.apiUrl}/${segment}`;
  }

  list(tipo: TipoCierre, filter: ApprovalFilterRequest) {
    return this.http.get<PagedResult<CierreListItemDto>>(this.base(tipo), {
      params: toHttpParams({ ...filter }),
    });
  }
  getById(tipo: TipoCierre, id: number) {
    return this.http.get<CierreDetailDto>(`${this.base(tipo)}/${id}`);
  }
  create(tipo: TipoCierre, req: CierreCreateRequest) {
    return this.http.post<CierreDetailDto>(this.base(tipo), req);
  }

  recalcular(tipo: TipoCierre, id: number, rowVersion: number, req: CierreRecalcRequest) {
    return this.http.post<CierreDetailDto>(`${this.base(tipo)}/${id}/recalcular`, req, {
      headers: new HttpHeaders({ 'If-Match': String(rowVersion) }),
    });
  }
  aprobar(tipo: TipoCierre, id: number, rowVersion: number, req: CierreApproveRequest) {
    return this.http.post<CierreDetailDto>(`${this.base(tipo)}/${id}/aprobar`, req, {
      headers: new HttpHeaders({ 'If-Match': String(rowVersion) }),
    });
  }
  rechazar(tipo: TipoCierre, id: number, rowVersion: number, req: CierreRejectRequest) {
    return this.http.post<CierreDetailDto>(`${this.base(tipo)}/${id}/rechazar`, req, {
      headers: new HttpHeaders({ 'If-Match': String(rowVersion) }),
    });
  }

  // Ola 2 (#3a): override manual de importe de línea e incentivos manuales.
  overrideLinea(tipo: TipoCierre, cierreId: number, lineId: number, rowVersion: number, req: CierreLineOverrideRequest) {
    return this.http.post<CierreDetailDto>(`${this.base(tipo)}/${cierreId}/lines/${lineId}/override`, req, {
      headers: new HttpHeaders({ 'If-Match': String(rowVersion) }),
    });
  }
  agregarIncentivo(tipo: TipoCierre, cierreId: number, rowVersion: number, req: CierreLineIncentivoRequest) {
    return this.http.post<CierreDetailDto>(`${this.base(tipo)}/${cierreId}/lines/incentivo`, req, {
      headers: new HttpHeaders({ 'If-Match': String(rowVersion) }),
    });
  }

  historial(tipo: TipoCierre, cierreId: number) {
    return this.http.get<CierreHistoryDto[]>(`${this.base(tipo)}/${cierreId}/historial`);
  }

  getAlertas(tipo: TipoCierre, cierreId: number) {
    return this.http.get<ClosureAlertaDto[]>(`${this.base(tipo)}/${cierreId}/alertas`);
  }

  confirmarAlerta(tipo: TipoCierre, cierreId: number, alertaId: number) {
    return this.http.post<CierreDetailDto>(`${this.base(tipo)}/${cierreId}/alertas/${alertaId}/confirmar`, {});
  }

  // Ola 3b (#10): el backend ya no expone un endpoint global de alertas (antes closures/todas-alertas).
  // Se agregan al vuelo recorriendo ambos tipos de cierre y consultando sus alertas.
  // Cada alerta se enriquece con tipoCierre, closureId, serviceId y closureNombre para la UI.
  getAllAlertas(): Observable<ClosureAlertaDto[]> {
    const filter: ApprovalFilterRequest = { page: 1, pageSize: 500 };
    return forkJoin({
      costes: this.list('Costes', filter),
      facturacion: this.list('Facturacion', filter),
    }).pipe(
      mergeMap((listas) => {
        const todos: CierreListItemDto[] = [...listas.costes.items, ...listas.facturacion.items];
        if (todos.length === 0) return of([] as ClosureAlertaDto[]);
        const calls = todos.map((c) =>
          this.getAlertas(c.tipoCierre, c.id).pipe(
            map((alertas) => alertas.map((a) => ({
              ...a,
              closureId: c.id,
              serviceId: c.serviceId,
              closureNombre: `${c.serviceNombre} — ${c.periodNombre}`,
            } as ClosureAlertaDto)))
          )
        );
        return forkJoin(calls).pipe(map((arrs) => arrs.flat()));
      })
    );
  }
}
