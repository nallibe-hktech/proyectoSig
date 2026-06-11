import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface StagingBizneoEmpleado {
  id: number;
  empleadoIdExterno: string;
  nif: string;
  nombre: string;
  departamento?: string;
  fechaUltimaSincronizacion: string;
}

export interface StagingBizneoAbsence {
  id: number;
  registroIdExterno: string;
  userId: number;
  projectId: number;
  fecha: string;
  horas: number;
  fechaUltimaSincronizacion: string;
}

@Injectable({ providedIn: 'root' })
export class BizneoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/bizneo';

  getEmpleados(search?: string): Observable<StagingBizneoEmpleado[]> {
    let url = `${this.apiUrl}/empleados`;
    if (search) {
      url += `?search=${encodeURIComponent(search)}`;
    }
    return this.http.get<StagingBizneoEmpleado[]>(url);
  }

  getAusencias(search?: string): Observable<StagingBizneoAbsence[]> {
    let url = `${this.apiUrl}/ausencias`;
    if (search) {
      url += `?search=${encodeURIComponent(search)}`;
    }
    return this.http.get<StagingBizneoAbsence[]>(url);
  }
}
