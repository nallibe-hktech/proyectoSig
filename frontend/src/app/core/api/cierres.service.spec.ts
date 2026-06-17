import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CierresService } from './cierres.service';
import { environment } from '../../../environments/environment';
import { ApprovalFilterRequest } from '../../models/dtos';

describe('CierresService', () => {
  let svc: CierresService;
  let http: HttpTestingController;
  const baseCostes = `${environment.apiUrl}/cierres-costes`;
  const baseFact = `${environment.apiUrl}/cierres-facturacion`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CierresService, provideHttpClient(), provideHttpClientTesting()],
    });
    svc = TestBed.inject(CierresService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list (Costes): pega contra api/cierres-costes con query del filtro', () => {
    const filter = { periodId: 3, page: 1, pageSize: 25 } as ApprovalFilterRequest;
    svc.list('Costes', filter).subscribe();
    const req = http.expectOne((r) => r.url === baseCostes);
    expect(req.request.params.get('periodId')).toBe('3');
    req.flush({ items: [], total: 0, page: 1, pageSize: 25 });
  });

  it('list (Facturacion): pega contra api/cierres-facturacion', () => {
    svc.list('Facturacion', { page: 1, pageSize: 25 } as ApprovalFilterRequest).subscribe();
    const req = http.expectOne((r) => r.url === baseFact);
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], total: 0, page: 1, pageSize: 25 });
  });

  it('aprobar: envía header If-Match con rowVersion (concurrencia optimista RNF-03)', () => {
    svc.aprobar('Costes', 99, 42, { comentarios: 'OK' }).subscribe();
    const req = http.expectOne(`${baseCostes}/99/aprobar`);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('If-Match')).toBe('42');
    expect(req.request.body).toEqual({ comentarios: 'OK' });
    req.flush({} as any);
  });

  it('rechazar: envía If-Match con rowVersion y body con motivo', () => {
    svc.rechazar('Facturacion', 99, 7, { motivo: 'Falta info' }).subscribe();
    const req = http.expectOne(`${baseFact}/99/rechazar`);
    expect(req.request.headers.get('If-Match')).toBe('7');
    expect(req.request.body).toEqual({ motivo: 'Falta info' });
    req.flush({} as any);
  });

  it('recalcular: envía If-Match', () => {
    svc.recalcular('Costes', 5, 100, {}).subscribe();
    const req = http.expectOne(`${baseCostes}/5/recalcular`);
    expect(req.request.headers.get('If-Match')).toBe('100');
    req.flush({} as any);
  });

  it('historial: GET /cierres-costes/:id/historial', () => {
    svc.historial('Costes', 5).subscribe();
    const req = http.expectOne(`${baseCostes}/5/historial`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
