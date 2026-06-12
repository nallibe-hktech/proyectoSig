import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ServiceService } from './services.service';
import { environment } from '../../../environments/environment';

describe('ServiceService', () => {
  let svc: ServiceService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/services`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ServiceService, provideHttpClient(), provideHttpClientTesting()],
    });
    svc = TestBed.inject(ServiceService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list: GET /services con clientId opcional omitido cuando es null', () => {
    svc.list(1, 25, null, undefined).subscribe();
    const req = http.expectOne((r) => r.url === base);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.has('clientId')).toBe(false);
    expect(req.request.params.has('search')).toBe(false);
    req.flush({ items: [], total: 0, page: 1, pageSize: 25 });
  });

  it('list: GET /services con clientId y search', () => {
    svc.list(2, 10, 99, 'Alpha').subscribe();
    const req = http.expectOne((r) => r.url === base);
    expect(req.request.params.get('clientId')).toBe('99');
    expect(req.request.params.get('search')).toBe('Alpha');
    req.flush({ items: [], total: 0, page: 2, pageSize: 10 });
  });

  it('getById / create / update / delete', () => {
    svc.getById(1).subscribe();
    http.expectOne(`${base}/1`).flush({});
    svc.create({ nombre: 'X' } as any).subscribe();
    http.expectOne(base).flush({});
    svc.update(7, { nombre: 'Y' } as any).subscribe();
    http.expectOne(`${base}/7`).flush({});
    svc.delete(3).subscribe();
    http.expectOne(`${base}/3`).flush(null, { status: 204, statusText: 'NoContent' });
  });
});
