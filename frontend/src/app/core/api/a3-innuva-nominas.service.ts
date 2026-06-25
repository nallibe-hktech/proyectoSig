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
  id: number;
  nombre: string;
  fechaInicio: string;
  fechaFin: string;
  diaPago: number;
  estado: string;
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
   * Note: Processes all employees from synced payrolls (no company filter needed)
   */
  syncEmployees(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync/employees`, {});
  }

  /**
   * PHASE 1.4: Sync concepts (conceptos de nómina) from Wolters Kluwer
   * Note: Processes all concepts from synced payrolls (no company filter needed)
   */
  syncConceptos(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync-concepts`, {});
  }

  // ============ PHASE 1 REDESIGNED: Real Wolters Kluwer Endpoints ============
  /**
   * PHASE 1.5: Sync IRPF (tax retention) data from Wolters Kluwer
   * Fetches: TaxType, TaxRate, RetentionAmount, StartDate, EndDate
   */
  syncIRPF(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync-irpf`, {});
  }

  /**
   * PHASE 1.6: Sync remuneration data (bonuses, incentives) from Wolters Kluwer
   * Fetches: RemunerationType, Amount, Concept, StartDate, EndDate
   */
  syncRemuneration(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync-remuneration`, {});
  }

  /**
   * PHASE 1.7: Sync bank account data from Wolters Kluwer
   * Fetches: IBAN, BIC, AccountHolderName, AccountType, IsPrimary, StartDate, EndDate
   */
  syncBankAccounts(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync-bank-accounts`, {});
  }

  /**
   * PHASE 1.8: Sync collective agreements data from Wolters Kluwer
   * Fetches: AgreementCode, AgreementName, AgreementType, StartDate, EndDate, Description
   */
  syncAgreements(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync-agreements`, {});
  }

  /**
   * PHASE 1.9: Sync contract agreement data from Wolters Kluwer
   * Fetches per-employee contract details: ContractCode, Description, Labour Period, Contribution Type
   */
  syncContractAgreements(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync-contract-agreements`, {});
  }

  /**
   * PHASE 1.10: Sync contract timetable data from Wolters Kluwer
   * Fetches per-employee work schedule: WorkDayType, Total Hours, Start/End Times, Complementary Hours
   */
  syncContractTimetables(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync-contract-timetables`, {});
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
   * Get synced companies with pagination and search
   */
  getCompanies(
    page: number,
    pageSize: number,
    search?: string
  ): Observable<PagedResult<any>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<any>>(`${this.apiUrl}/companies`, { params });
  }

  /**
   * Get synced payrolls with pagination and search
   */
  getPayrolls(
    page: number,
    pageSize: number,
    search?: string
  ): Observable<PagedResult<any>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<any>>(`${this.apiUrl}/payrolls`, { params });
  }

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
    return this.http.get<PagedResult<A3InnuvaNominaDto>>(`${this.apiUrl}/calculated`, { params });
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
    return this.http.get<PagedResult<A3InnuvaNominaDto>>(`${this.apiUrl}/sent`, { params });
  }

  /**
   * Get list of periods (periodos) for filtering
   */
  getPeriods(): Observable<PeriodoDto[]> {
    return this.http.get<PeriodoDto[]>(`${environment.apiUrl}/periods`);
  }

  /**
   * Get synced employees with pagination and search
   */
  getEmployees(
    page: number,
    pageSize: number,
    search?: string
  ): Observable<PagedResult<any>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<any>>(`${this.apiUrl}/employees`, { params });
  }

  /**
   * Get synced concepts with pagination and search
   */
  getConceptos(
    page: number,
    pageSize: number,
    search?: string
  ): Observable<PagedResult<any>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<any>>(`${this.apiUrl}/concepts`, { params });
  }
}
