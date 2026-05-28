import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PeriodService } from './periods.service';
import { environment } from '../../../environments/environment';

describe('PeriodService', () => {
  let svc: PeriodService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/periods`;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [PeriodService, provideHttpClient(), provideHttpClientTesting()],
    });
    svc = TestBed.inject(PeriodService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('list: GET /periods', () => {
    svc.list().subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('getActivo: persiste id en sessionStorage si no había activo', () => {
    svc.getActivo().subscribe();
    const req = http.expectOne(`${base}/activo`);
    req.flush({ id: 42, nombre: 'Mar', fechaInicio: '2026-03-01', fechaFin: '2026-03-31', estado: 'Abierto' });
    expect(sessionStorage.getItem('sig_periodo_activo')).toBe('42');
    expect(svc.activeId()).toBe(42);
  });

  it('setActive: persiste y signal se actualiza', () => {
    svc.setActive(5);
    expect(sessionStorage.getItem('sig_periodo_activo')).toBe('5');
    expect(svc.activeId()).toBe(5);
    svc.setActive(null);
    expect(sessionStorage.getItem('sig_periodo_activo')).toBeNull();
    expect(svc.activeId()).toBeNull();
  });

  it('cerrar: POST /periods/:id/cerrar', () => {
    svc.cerrar(7).subscribe();
    const req = http.expectOne(`${base}/7/cerrar`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  it('reabrir: POST /periods/:id/reabrir', () => {
    svc.reabrir(7).subscribe();
    const req = http.expectOne(`${base}/7/reabrir`);
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
