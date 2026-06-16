import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SyncEventService {
  private readonly _synced$ = new Subject<string>();
  readonly synced$ = this._synced$.asObservable();

  notify(system: string): void {
    this._synced$.next(system);
  }
}
