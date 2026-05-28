import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ClosureService } from './closures.service';
import { environment } from '../../../environments/environment';
import { ApprovalFilterRequest } from '../../models/dtos';

describe('ClosureService', () => {
  let svc: ClosureService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/closures`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ClosureService, provideHttpClient(), provideHttpClientTesting()],
    });
    svc = TestBed.inject(ClosureService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list: usa query string completo del filtro', () => {
    const filter = { periodId: 3, page: 1, pageSize: 25 } as ApprovalFilterRequest;
    svc.list(filter).subscribe();
    const req = http.expectOne((r) => r.url === base);
    expect(req.request.params.get('periodId')).toBe('3');
    req.flush({ items: [], total: 0, page: 1, pageSize: 25 });
  });

  it('aprobar: envía header If-Match con rowVersion (concurrencia optimista RNF-03)', () => {
    svc.aprobar(99, 42, { comentarios: 'OK' } as any).subscribe();
    const req = http.expectOne(`${base}/99/aprobar`);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('If-Match')).toBe('42');
    expect(req.request.body).toEqual({ comentarios: 'OK' });
    req.flush({} as any);
  });

  it('rechazar: envía If-Match con rowVersion y body con motivo', () => {
    svc.rechazar(99, 7, { motivo: 'Falta info' } as any).subscribe();
    const req = http.expectOne(`${base}/99/rechazar`);
    expect(req.request.headers.get('If-Match')).toBe('7');
    expect(req.request.body).toEqual({ motivo: 'Falta info' });
    req.flush({} as any);
  });

  it('recalcular: envía If-Match', () => {
    svc.recalcular(5, 100, {} as any).subscribe();
    const req = http.expectOne(`${base}/5/recalcular`);
    expect(req.request.headers.get('If-Match')).toBe('100');
    req.flush({} as any);
  });

  it('historial: GET /approvals/historial/:id', () => {
    svc.historial(5).subscribe();
    const req = http.expectOne(`${environment.apiUrl}/approvals/historial/5`);
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
