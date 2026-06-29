import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../../models/dtos';
import { toHttpParams } from './api.helpers';

// Notificaciones in-app (circuito de devolución de cierre): campana del shell con contador de no leídas.
@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/notifications`;

  list(soloNoLeidas = false, take = 20) {
    return this.http.get<NotificationDto[]>(this.base, {
      params: toHttpParams({ soloNoLeidas, take }),
    });
  }

  unreadCount() {
    return this.http.get<number>(`${this.base}/unread-count`);
  }

  markRead(id: number) {
    return this.http.post<void>(`${this.base}/${id}/leer`, {});
  }

  markAllRead() {
    return this.http.post<void>(`${this.base}/leer-todas`, {});
  }
}
