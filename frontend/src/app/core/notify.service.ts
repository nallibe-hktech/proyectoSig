import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

// NotifyService — wrapper sobre MatSnackBar con clases semánticas.
@Injectable({ providedIn: 'root' })
export class NotifyService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string, action = 'Cerrar') {
    return this.show(message, action, 'snack-success');
  }
  error(message: string, action = 'Cerrar') {
    return this.show(message, action, 'snack-error');
  }
  warning(message: string, action = 'Cerrar') {
    return this.show(message, action, 'snack-warning');
  }
  info(message: string, action = 'Cerrar') {
    return this.show(message, action, 'snack-info');
  }

  private show(message: string, action: string, panelClass: string) {
    const config: MatSnackBarConfig = {
      duration: 4000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: [panelClass],
    };
    return this.snackBar.open(message, action, config);
  }
}
