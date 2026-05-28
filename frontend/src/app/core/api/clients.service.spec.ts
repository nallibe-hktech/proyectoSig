import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ClientService } from './clients.service';
import { environment } from '../../../environments/environment';
import { ClientCreateRequest, ClientDetailDto, ClientUpdateRequest, PagedResult, ClientListItemDto } from '../../models/dtos';

describe('ClientService', () => {
  let service: ClientService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/clients`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ClientService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ClientService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list: GET /clients con query params', () => {
    const expected: PagedResult<ClientListItemDto> = { items: [], total: 0, page: 1, pageSize: 25 };
    service.list(1, 25, 'Alpha').subscribe((r) => expect(r).toEqual(expected));
    const req = http.expectOne((r) => r.url === base && r.method === 'GET');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.get('search')).toBe('Alpha');
    req.flush(expected);
  });

  it('getById: GET /clients/:id', () => {
    service.getById(7).subscribe();
    const req = http.expectOne(`${base}/7`);
    expect(req.request.method).toBe('GET');
    req.flush({ id: 7, nombre: 'X', nif: 'A12345678' } as ClientDetailDto);
  });

  it('create: POST /clients con body completo', () => {
    const body: ClientCreateRequest = { nombre: 'Nuevo', nif: 'Z99999999' } as ClientCreateRequest;
    service.create(body).subscribe();
    const req = http.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({ id: 1 } as ClientDetailDto);
  });

  it('update: PUT /clients/:id', () => {
    const body: ClientUpdateRequest = { nombre: 'Mod', nif: 'Z99999999' } as ClientUpdateRequest;
    service.update(5, body).subscribe();
    const req = http.expectOne(`${base}/5`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({ id: 5 } as ClientDetailDto);
  });

  it('delete: DELETE /clients/:id', () => {
    service.delete(3).subscribe();
    const req = http.expectOne(`${base}/3`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
