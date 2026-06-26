import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface A3InnuvaCompanyDto {
  id: string;
  code: string;
  name: string;
  taxId: string;
  address: string | null;
  city: string | null;
  country: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
}

export interface A3InnuvaPayrollDto {
  id: string;
  employeeId: string;
  employeeName: string;
  periodCode: string;
  baseSalary: number;
  deductions: number;
  netSalary: number;
  processDate: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root'
})
export class A3InnuvaService {
  private apiUrl = `${environment.apiUrl}/a3-innuva-nominas`;

  constructor(private http: HttpClient) {}

  // === PRODUCTION ENDPOINTS ===
  syncCompanies(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync/companies`, {});
  }

  syncPayrolls(companyCode: string): Observable<any> {
    const params = new HttpParams().set('companyCode', companyCode);
    return this.http.post(`${this.apiUrl}/sync/payrolls`, {}, { params });
  }

  syncEmployees(): Observable<any> {
    return this.http.post(`${this.apiUrl}/sync/employees`, {});
  }

  getCompanies(page: number, pageSize: number, search?: string): Observable<PagedResult<A3InnuvaCompanyDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<A3InnuvaCompanyDto>>(`${this.apiUrl}/companies`, { params });
  }

  getPayrolls(page: number, pageSize: number, search?: string): Observable<PagedResult<A3InnuvaPayrollDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<A3InnuvaPayrollDto>>(`${this.apiUrl}/payrolls`, { params });
  }

  // === TEST ENDPOINTS (Write to TEST tables only) ===
  syncCompaniesTest(): Observable<any> {
    return this.http.post(`${this.apiUrl}/test/sync/companies`, {});
  }

  syncPayrollsTest(companyCode: string): Observable<any> {
    const params = new HttpParams().set('companyCode', companyCode);
    return this.http.post(`${this.apiUrl}/test/sync/payrolls`, {}, { params });
  }

  getCompaniesTest(page: number, pageSize: number, search?: string): Observable<PagedResult<A3InnuvaCompanyDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    console.log('📡 GET', `${this.apiUrl}/test/companies`, { params });
    return this.http.get<PagedResult<A3InnuvaCompanyDto>>(`${this.apiUrl}/test/companies`, { params });
  }

  getPayrollsTest(page: number, pageSize: number, search?: string): Observable<PagedResult<A3InnuvaPayrollDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<A3InnuvaPayrollDto>>(`${this.apiUrl}/test/payrolls`, { params });
  }

  // === OAUTH ENDPOINTS ===
  /**
   * Get OAuth authorize URL for Wolters Kluwer login
   * User must open this URL in browser and authorize the application
   */
  getAuthorizeUrl(): Observable<any> {
    return this.http.get(`${this.apiUrl}/oauth/authorize-url`);
  }

  /**
   * Refresh access token manually (automatic refresh happens on expiry)
   */
  refreshToken(): Observable<any> {
    return this.http.post(`${this.apiUrl}/oauth/refresh`, {});
  }

  /**
   * Generar plantilla Excel A3 Innuva para descarga manual
   */
  generateExcel(periodCode: string): Observable<Blob> {
    const params = new HttpParams().set('periodCode', periodCode);
    return this.http.get(`${this.apiUrl}/generate-excel`, {
      params,
      responseType: 'blob'
    });
  }
}
