import { Injectable, signal, computed } from '@angular/core';

/**
 * Service to manage which alerts have been marked as read.
 * Persists read state to localStorage so it survives page reloads.
 * New alerts (with IDs not yet seen) will still appear.
 */
@Injectable({ providedIn: 'root' })
export class AlertReadStateService {
  private readonly KEY = 'sig_alerts_read_ids';
  private readIds = signal<Set<number>>(this.loadFromStorage());

  readonly readCount = computed(() => this.readIds().size);

  /**
   * Check if an alert has been marked as read.
   */
  isRead(id: number): boolean {
    return this.readIds().has(id);
  }

  /**
   * Mark a single alert as read and persist to localStorage.
   */
  markAsRead(id: number): void {
    this.addAndPersist(id);
  }

  /**
   * Mark multiple alerts as read in bulk.
   */
  markAllAsRead(ids: number[]): void {
    if (ids.length === 0) return;
    const updated = new Set(this.readIds());
    ids.forEach(id => updated.add(id));
    this.readIds.set(updated);
    localStorage.setItem(this.KEY, JSON.stringify([...updated]));
  }

  /**
   * Clear all read state (for debugging/testing).
   */
  clearAll(): void {
    this.readIds.set(new Set());
    localStorage.removeItem(this.KEY);
  }

  private addAndPersist(id: number): void {
    const updated = new Set(this.readIds());
    updated.add(id);
    this.readIds.set(updated);
    localStorage.setItem(this.KEY, JSON.stringify([...updated]));
  }

  private loadFromStorage(): Set<number> {
    try {
      const stored = localStorage.getItem(this.KEY);
      if (!stored) return new Set();
      return new Set(JSON.parse(stored) as number[]);
    } catch {
      return new Set();
    }
  }
}
