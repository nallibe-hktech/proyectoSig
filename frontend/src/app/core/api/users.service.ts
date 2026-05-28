import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  UserListItemDto, UserDetailDto, UserCreateRequest, UserUpdateRequest,
  UserPasswordChangeRequest, PagedResult,
} from '../../models/dtos';
import { toHttpParams } from './api.helpers';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/users`;

  list(page: number, pageSize: number, search?: string) {
    return this.http.get<PagedResult<UserListItemDto>>(this.base, {
      params: toHttpParams({ page, pageSize, search }),
    });
  }
  getById(id: number) { return this.http.get<UserDetailDto>(`${this.base}/${id}`); }
  create(req: UserCreateRequest) { return this.http.post<UserDetailDto>(this.base, req); }
  update(id: number, req: UserUpdateRequest) { return this.http.put<UserDetailDto>(`${this.base}/${id}`, req); }
  changePassword(id: number, req: UserPasswordChangeRequest) {
    return this.http.put<void>(`${this.base}/${id}/password`, req);
  }
  delete(id: number) { return this.http.delete<void>(`${this.base}/${id}`); }
}
