import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { A3ErpService } from './a3erp.service';
import { environment } from '../../../environments/environment';

describe('A3ErpService', () => {
  let svc: A3ErpService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/a3-erp`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [A3ErpService, provideHttpClient(), provideHttpClientTesting()],
    });
    svc = TestBed.inject(A3ErpService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getStatus: pega GET contra api/a3-erp/status', () => {
    svc.getStatus().subscribe();
    const req = http.expectOne(`${base}/status`);
    expect(req.request.method).toBe('GET');
    req.flush({ connected: false, modo: 'Test', mensaje: 'A3 ERP no configurado (modo test).' });
  });

  it('sync: pega POST contra api/a3-erp/sync', () => {
    svc.sync().subscribe({ error: () => undefined });
    const req = http.expectOne(`${base}/sync`);
    expect(req.request.method).toBe('POST');
    req.flush({ detail: 'pendiente' }, { status: 501, statusText: 'Not Implemented' });
  });
});
