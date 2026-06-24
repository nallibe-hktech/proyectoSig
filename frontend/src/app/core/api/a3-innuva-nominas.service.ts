import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface A3InnuvaNominaDto {
  id: string;
  codigoEmpleado: string;
  nombreEmpleado: string;
  fecha: string;
  fechaContrato: string;
  importeTotal: number;
  periodoCode?: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface PeriodoDto {
  id: string;
  code: string;
  name: string;
  startDate: string;
  endDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class A3InnuvaNominasService {
  private apiUrl = `${environment.apiUrl}/a3-innuva-nominas`;

  constructor(private http: HttpClient) {}

  // ============ OAUTH ============
  /**
   * Get OAuth authorize URL for Wolters Kluwer login.
   * User must open this URL in browser and authorize the application.
   */
  getAuthorizeUrl(): Observable<{ authorizeUrl: string; redirectUri: string; message: string }> {
    return this.http.get<any>(`${this.apiUrl}/oauth/authorize-url`);
  }

  // ============ PHASE 1: SYNC ============
  /**
   * PHASE 1.1: Sync companies from Wolters Kluwer
   */
  syncCompanies(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync/companies`, {});
  }

  /**
   * PHASE 1.2: Sync payrolls (nóminas) from Wolters Kluwer
   */
  syncPayrolls(companyCode: string): Observable<any> {
    const params = new HttpParams().set('companyCode', companyCode);
    return this.http.post(`${this.apiUrl}/sync/payrolls`, {}, { params });
  }

  /**
   * PHASE 1.3: Sync employees from Wolters Kluwer
   */
  syncEmployees(companyCode: string): Observable<any> {
    const params = new HttpParams().set('companyCode', companyCode);
    return this.http.post(`${this.apiUrl}/sync/employees`, {}, { params });
  }

  /**
   * PHASE 1.4: Sync concepts (conceptos de nómina) from Wolters Kluwer
   */
  syncConceptos(companyCode: string): Observable<any> {
    const params = new HttpParams().set('companyCode', companyCode);
    return this.http.post(`${this.apiUrl}/sync-concepts`, {}, { params });
  }

  // ============ PHASE 2: CALCULATE ============
  /**
   * PHASE 2: Calculate payrolls (nóminas calculadas)
   * Uses CalculationEngine to compute concept importes based on base salary and business rules
   */
  calculatePayrolls(periodCode: string): Observable<any> {
    const params = new HttpParams().set('periodCode', periodCode);
    return this.http.post(`${this.apiUrl}/calculate`, {}, { params });
  }

  // ============ PHASE 3: WRITE ============
  /**
   * PHASE 3: Write payrolls back to Wolters Kluwer
   * Only executes if calculations are valid
   */
  writePayrolls(periodCode: string): Observable<any> {
    const params = new HttpParams().set('periodCode', periodCode);
    return this.http.post(`${this.apiUrl}/write`, {}, { params });
  }

  // ============ DATA FETCHING ============
  /**
   * Get calculated payrolls (nóminas calculadas) with pagination and search
   */
  getNominasCalculadas(
    page: number,
    pageSize: number,
    periodCode?: string,
    search?: string
  ): Observable<PagedResult<A3InnuvaNominaDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (periodCode) params = params.set('periodCode', periodCode);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<A3InnuvaNominaDto>>(`${this.apiUrl}/nóminas-calculadas`, { params });
  }

  /**
   * Get sent payrolls (nóminas enviadas) with pagination and search
   */
  getNominasEnviadas(
    page: number,
    pageSize: number,
    periodCode?: string,
    search?: string
  ): Observable<PagedResult<A3InnuvaNominaDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (periodCode) params = params.set('periodCode', periodCode);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<A3InnuvaNominaDto>>(`${this.apiUrl}/nóminas-enviadas`, { params });
  }

  /**
   * Get list of periods (periodos) for filtering
   */
  getPeriods(): Observable<PeriodoDto[]> {
    return this.http.get<PeriodoDto[]>(`${environment.apiUrl}/periods`);
  }
}
