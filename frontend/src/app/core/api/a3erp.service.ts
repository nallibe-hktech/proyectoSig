import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { A3ErpStatusDto } from '../../models/dtos';

// A3 ERP (Contabilidad) — hub de traspaso de facturas del cierre a A3 ERP.
// El export real reutiliza ExportService.exportA3Erp (api/exports/a3-erp/{closureId}).
// Este servicio solo cubre el estado de conexión y el sync (stub, pendiente de spec API).
@Injectable({ providedIn: 'root' })
export class A3ErpService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/a3-erp`;

  getStatus() {
    return this.http.get<A3ErpStatusDto>(`${this.base}/status`);
  }

  // Importación desde A3 ERP — pendiente de especificación de la API (501 en backend).
  sync() {
    return this.http.post<void>(`${this.base}/sync`, {});
  }
}
