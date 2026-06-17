import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  RoleDto, DepartmentDto, DepartmentCreateRequest, DepartmentUpdateRequest,
  CostCenterDto, CostCenterCreateRequest, CostCenterUpdateRequest,
} from '../../models/dtos';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/roles`;
  list() { return this.http.get<RoleDto[]>(this.base); }
  listPaginated(page: number, pageSize: number) { return this.http.get<any>(`${this.base}/paginated?page=${page}&pageSize=${pageSize}`); }
}

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/departments`;
  list() { return this.http.get<DepartmentDto[]>(this.base); }
  listPaginated(page: number, pageSize: number) { return this.http.get<any>(`${this.base}/paginated?page=${page}&pageSize=${pageSize}`); }
  create(req: DepartmentCreateRequest) { return this.http.post<DepartmentDto>(this.base, req); }
  update(id: number, req: DepartmentUpdateRequest) { return this.http.put<DepartmentDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class CostCenterService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/costcenters`;
  list() { return this.http.get<CostCenterDto[]>(this.base); }
  listPaginated(page: number, pageSize: number) { return this.http.get<any>(`${this.base}/paginated?page=${page}&pageSize=${pageSize}`); }
  create(req: CostCenterCreateRequest) { return this.http.post<CostCenterDto>(this.base, req); }
  update(id: number, req: CostCenterUpdateRequest) { return this.http.put<CostCenterDto>(`${this.base}/${id}`, req); }
  delete(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }
}
