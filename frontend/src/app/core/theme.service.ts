import { Injectable, signal, effect } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'sig-theme';

  readonly isDark = signal<boolean>(this.loadPreference());

  constructor() {
    // Apply theme on init
    this.applyTheme(this.isDark());

    // React to changes
    effect(() => {
      const dark = this.isDark();
      this.applyTheme(dark);
      localStorage.setItem(this.STORAGE_KEY, dark ? 'dark' : 'light');
    });
  }

  toggle(): void {
    this.isDark.update(v => !v);
  }

  private loadPreference(): boolean {
    const saved = localStorage.getItem(this.STORAGE_KEY);
    if (saved) return saved === 'dark';
    // Default: dark
    return true;
  }

  private applyTheme(dark: boolean): void {
    const body = document.body;
    if (dark) {
      body.classList.remove('sig-light');
      body.classList.add('sig-dark');
    } else {
      body.classList.remove('sig-dark');
      body.classList.add('sig-light');
    }
  }
}
