import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ContratoUnDiaDto, ContratoIgnorarRequest } from '../../models/dtos';

// Contratos A3 Innuva (Ola 2 #2 — contratos de un día). APIs de solo lectura del ERP + marca local "ignorar".
@Injectable({ providedIn: 'root' })
export class ContratoService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/contratos`;

  listUnDia() { return this.http.get<ContratoUnDiaDto[]>(`${this.base}/un-dia`); }
  marcarIgnorar(id: number, req: ContratoIgnorarRequest) {
    return this.http.post<ContratoUnDiaDto>(`${this.base}/${id}/ignorar`, req);
  }
}
