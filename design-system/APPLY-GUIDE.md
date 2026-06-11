# SIG-ES Design System — Guía de Aplicación

> Generado automáticamente. Carpeta origen: `proyecto SIG ES/design-system/`
> Proyecto Angular destino: `Workspaces/SIG-es/`

---

## 1. Copia los archivos SCSS al proyecto

```bash
# Desde la raíz del proyecto Angular (Workspaces/SIG-es)
cp "ruta/design-system/_variables.scss"  src/styles/_variables.scss
cp "ruta/design-system/styles.scss"       src/styles/styles.scss
```

Crea la carpeta si no existe:
```bash
mkdir -p src/styles
```

---

## 2. Registra el SCSS global en angular.json

Abre `angular.json` y en la sección `projects > sig-es > architect > build > options > styles` añade:

```json
"styles": [
  "src/styles/styles.scss"
]
```

Si ya existía `src/styles.scss` o `src/styles/styles.scss`, **reemplázalo** o impórtalo al final:

```scss
// src/styles.scss (si prefieres mantener el fichero original)
@forward 'styles/styles';
```

---

## 3. Asegúrate de que Angular Material Dark Theme está importado

En `src/styles/styles.scss` (ya incluido en el archivo generado), el tema oscuro se aplica mediante CSS custom properties en `:root`. Asegúrate de que tienes importado el prebuilt theme o la configuración theming en tu `angular.json`:

```json
"styles": [
  "@angular/material/prebuilt-themes/custom.css",
  "src/styles/styles.scss"
]
```

O bien en el `styles.scss` global (método recomendado Angular 18):

```scss
@use '@angular/material' as mat;
@include mat.core();
// El resto lo gestiona el custom-properties override en styles.scss
```

---

## 4. Aplica el App Shell (layout principal)

### 4a. app.component.html
Reemplaza el contenido de `src/app/app.component.html` con el de `design-system/app-layout.html`.

En `src/app/app.component.ts` añade las propiedades:

```typescript
export class AppComponent {
  currentModule = signal<string>('dashboard');
  detailPanelOpen = false;

  get moduleTitle(): string {
    const titles: Record<string, string> = {
      dashboard:     'Dashboard',
      clientes:      'Clientes',
      proyectos:     'Proyectos',
      acciones:      'Acciones',
      conceptos:     'Conceptos',
      periodos:      'Periodos',
      aprobaciones:  'Aprobaciones',
      contabilidad:  'Contabilidad',
      informes:      'Informes',
      cecos:         'CECOs',
      departamentos: 'Departamentos',
      roles:         'Roles',
      usuarios:      'Usuarios',
      auditoria:     'Auditoría',
    };
    return titles[this.currentModule()] ?? '';
  }

  get userInitials(): string { return 'NA'; }   // TODO: auth service
  get hasNotifications(): boolean { return false; }

  onNavigate(module: string): void {
    this.currentModule.set(module);
  }

  onLogout(): void {
    // TODO: auth.logout()
  }
}
```

### 4b. app.component.scss
Reemplaza el contenido de `src/app/app.component.scss` con el de `design-system/app-layout.scss`.

> Asegúrate de que la ruta del `@use` apunta correctamente:
> ```scss
> @use '../styles/variables' as *;
> ```

---

## 5. Crea el Sidebar Component

Si no existe, genera el componente:
```bash
ng generate component shared/sidebar --standalone
```

Luego copia:
- `design-system/sidebar.component.html` → `src/app/shared/sidebar/sidebar.component.html`
- `design-system/sidebar.component.scss` → `src/app/shared/sidebar/sidebar.component.scss`

En `sidebar.component.ts` declara los inputs/outputs:

```typescript
import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule]
})
export class SidebarComponent {
  @Input() currentModule = '';
  @Output() navigate = new EventEmitter<string>();

  // TODO: inject AuthService
  get userInitials(): string { return 'NA'; }
  get userName(): string     { return 'Nallibe'; }
  get userRole(): string     { return 'Administrador'; }
}
```

Ajusta el `@use` en el SCSS del sidebar:
```scss
@use '../../styles/variables' as *;
```

---

## 6. Crea el Login Component

```bash
ng generate component features/auth/login --standalone
```

Copia:
- `design-system/login.component.html` → `src/app/features/auth/login/login.component.html`
- `design-system/login.component.scss`  → `src/app/features/auth/login/login.component.scss`

En `login.component.ts`:

```typescript
import { Component } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatIconModule, MatProgressSpinnerModule]
})
export class LoginComponent {
  showPassword = false;
  loading      = false;
  loginError   = '';

  loginForm = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  constructor(private fb: FormBuilder) {}

  onSubmit(): void {
    if (this.loginForm.invalid) return;
    this.loading    = true;
    this.loginError = '';
    // TODO: inject AuthService y llamar login()
  }
}
```

Añade la ruta en `app.routes.ts`:
```typescript
{ path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) }
```

---

## 7. Usar las clases del design system en módulos

Las clases `.sig-*` de `styles.scss` están disponibles globalmente. Ejemplos de uso en cualquier componente:

```html
<!-- Tabla de datos -->
<div class="sig-table-wrapper">
  <table class="sig-table">
    <thead>
      <tr>
        <th>ID</th><th>Cliente</th><th>Estado</th>
      </tr>
    </thead>
    <tbody>
      <tr *ngFor="let row of data">
        <td><span class="sig-table-id">{{ row.id }}</span></td>
        <td><span class="sig-table-name">{{ row.nombre }}</span></td>
        <td><span class="sig-badge sig-badge--active">ACTIVO</span></td>
      </tr>
    </tbody>
  </table>
</div>

<!-- KPI cards en Dashboard -->
<div class="sig-kpi-card">
  <span class="sig-kpi-card__label">Proyectos activos</span>
  <span class="sig-kpi-card__value">24</span>
  <span class="sig-kpi-card__trend sig-kpi-card__trend--up">+3 este mes</span>
</div>

<!-- Badge de tipo -->
<span class="sig-type-pill sig-type-pill--pago">PAGO</span>
<span class="sig-type-pill sig-type-pill--factura">FACTURA</span>

<!-- Botones -->
<button class="sig-btn sig-btn--primary">Guardar</button>
<button class="sig-btn sig-btn--outline">Cancelar</button>
<button class="sig-btn sig-btn--teal">Aprobar</button>
```

---

## 8. Resumen de archivos generados

| Archivo generado | Destino en proyecto Angular |
|---|---|
| `_variables.scss` | `src/styles/_variables.scss` |
| `styles.scss` | `src/styles/styles.scss` |
| `app-layout.html` | `src/app/app.component.html` |
| `app-layout.scss` | `src/app/app.component.scss` |
| `sidebar.component.html` | `src/app/shared/sidebar/sidebar.component.html` |
| `sidebar.component.scss` | `src/app/shared/sidebar/sidebar.component.scss` |
| `login.component.html` | `src/app/features/auth/login/login.component.html` |
| `login.component.scss` | `src/app/features/auth/login/login.component.scss` |

---

## 9. Verificación rápida

Tras copiar y compilar (`ng serve`), deberías ver:
- Fondo global `#0d1b2a` (azul marino oscuro)
- Sidebar `#091523` con items nav y acento teal `#00d4c4` en el item activo
- Header `#0a1828` con nombre del módulo en blanco
- Login con card oscura, glow azul/teal y botón azul primario `#2563eb`

Si hay errores de rutas de `@use`, ajusta la profundidad de carpeta relativa (ej: `../styles/variables` o `../../styles/variables`).
