import { Component, signal, inject, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatTooltipModule } from '@angular/material/tooltip';
import { map } from 'rxjs/operators';
import { toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from '../../core/auth/auth.service';
import { PeriodService } from '../../core/api/periods.service';
import { PeriodDto } from '../../models/dtos';
import { NotifyService } from '../../core/notify.service';
import { ThemeService } from '../../core/theme.service';
import { DashboardDesignComponent } from '../../shared/dashboard-design.component';

interface NavItem {
  label: string;
  route: string;
  icon: string;
  testId: string;
  roles?: string[];   // si presente, solo visible para esos roles
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule,
    MatSelectModule,
    MatFormFieldModule,
    MatTooltipModule,
    // Design SVG components
    DashboardDesignComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly periodSvc = inject(PeriodService);
  private readonly notify = inject(NotifyService);
  private readonly breakpointObserver = inject(BreakpointObserver);
  protected readonly theme = inject(ThemeService);

  protected sidenavOpen = signal(true);
  protected readonly currentUser = this.auth.currentUser;
  protected readonly userDisplay = computed(() => {
    const u = this.currentUser();
    if (!u) return '';
    return `${u.nombre} ${u.apellidos}`.trim() || u.email;
  });
  protected readonly userInitial = computed(() => {
    const u = this.currentUser();
    if (!u) return 'U';
    const name = u.nombre?.trim() || u.email;
    return name.charAt(0).toUpperCase();
  });

  // Navegación canónica según el penpot (verdad de diseño): grupos
  // Principal / Administración / Configuración, en español. Las integraciones
  // y herramientas internas (que el penpot no lista) van a un grupo aparte.
  private readonly allOperativoNav: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard', testId: 'nav-dashboard' },
    { label: 'Clientes', route: '/clients', icon: 'groups', testId: 'nav-clients' },
    { label: 'Incidencias', route: '/incidencias', icon: 'report_problem', testId: 'nav-incidencias' },
    { label: 'Informes', route: '/reports', icon: 'bar_chart', testId: 'nav-reports' },
    { label: 'Servicios', route: '/services', icon: 'task_alt', testId: 'nav-services' },
    { label: 'Conceptos', route: '/concepts', icon: 'calculate', testId: 'nav-concepts' },
    { label: 'Periodos', route: '/periods', icon: 'calendar_month', testId: 'nav-periods' },
    { label: 'Aprobaciones', route: '/approvals', icon: 'approval', testId: 'nav-approvals' },
  ];

  private readonly allAdminNav: NavItem[] = [
    { label: 'Usuarios', route: '/users', icon: 'manage_accounts', testId: 'nav-users', roles: ['Administrator', 'Auditor'] },
    { label: 'Roles', route: '/roles', icon: 'verified_user', testId: 'nav-roles', roles: ['Administrator', 'Auditor'] },
    { label: 'CECOs', route: '/cost-centers', icon: 'account_tree', testId: 'nav-cost-centers', roles: ['Administrator'] },
    { label: 'Departamentos', route: '/departments', icon: 'corporate_fare', testId: 'nav-departments', roles: ['Administrator'] },
    { label: 'Contabilidad', route: '/a3-erp', icon: 'account_balance', testId: 'nav-contabilidad', roles: ['Administrator', 'Fico'] },
    { label: 'Auditoría', route: '/audit', icon: 'history', testId: 'nav-audit', roles: ['Administrator', 'Auditor'] },
  ];

  private readonly allConfigNav: NavItem[] = [
    { label: 'Config. Presupuesto', route: '/config-presupuesto', icon: 'savings', testId: 'nav-config-presupuesto', roles: ['Administrator', 'Fico'] },
    { label: 'Config. Factura', route: '/config-factura', icon: 'request_quote', testId: 'nav-config-factura', roles: ['Administrator', 'Fico'] },
    { label: 'Errores Nómina/Pagos', route: '/errores-nomina', icon: 'rule', testId: 'nav-errores-nomina', roles: ['Administrator', 'Fico', 'RRHH'] },
    { label: 'Errores Facturación', route: '/errores-facturacion', icon: 'price_check', testId: 'nav-errores-facturacion', roles: ['Administrator', 'Fico'] },
    { label: 'Traspaso CECOs', route: '/traspaso-cecos', icon: 'swap_horiz', testId: 'nav-traspaso-cecos', roles: ['Administrator', 'Fico'] },
  ];

  // Integraciones y herramientas internas (no forman parte del menú del penpot,
  // pero deben seguir siendo accesibles). Solo Administrador/Fico.
  private readonly allIntegracionesNav: NavItem[] = [
    { label: 'Cierres de Costes', route: '/cierres-costes', icon: 'payments', testId: 'nav-cierres-costes', roles: ['Administrator', 'Fico'] },
    { label: 'Cierres de Facturación', route: '/cierres-facturacion', icon: 'receipt_long', testId: 'nav-cierres-facturacion', roles: ['Administrator', 'Fico'] },
    { label: 'Variables', route: '/variables', icon: 'data_object', testId: 'nav-variables', roles: ['Administrator'] },
    { label: 'Contratos un día', route: '/contratos-un-dia', icon: 'description', testId: 'nav-contratos-un-dia', roles: ['Administrator', 'Backoffice'] },
    { label: 'Sync', route: '/sync', icon: 'refresh', testId: 'nav-sync', roles: ['Administrator'] },
    { label: 'Celero Visitas', route: '/celero-visitas', icon: 'location_on', testId: 'nav-celero-visitas', roles: ['Administrator'] },
    { label: 'Galán', route: '/galan', icon: 'warehouse', testId: 'nav-galan', roles: ['Administrator'] },
    { label: 'Mediapost', route: '/mediapost', icon: 'local_shipping', testId: 'nav-mediapost', roles: ['Administrator'] },
    { label: 'Bizneo', route: '/bizneo', icon: 'people', testId: 'nav-bizneo', roles: ['Administrator'] },
    { label: 'Intratime', route: '/intratime', icon: 'schedule', testId: 'nav-intratime', roles: ['Administrator'] },
    { label: 'PayHawk', route: '/payhawk', icon: 'receipt_long', testId: 'nav-payhawk', roles: ['Administrator', 'Fico'] },
    { label: 'SGPV', route: '/sgpv', icon: 'location_on', testId: 'nav-sgpv', roles: ['Administrator'] },
    { label: 'Travel Perk', route: '/travelperk', icon: 'flight_takeoff', testId: 'nav-travelperk', roles: ['Administrator', 'Fico'] },
    { label: 'A3 INNUVA Nóminas', route: '/a3-innuva', icon: 'download', testId: 'nav-a3-innuva', roles: ['Administrator'] },
  ];

  protected readonly operativoNav = computed(() => this.filterByRole(this.allOperativoNav));
  protected readonly adminNav = computed(() => this.filterByRole(this.allAdminNav));
  protected readonly configNav = computed(() => this.filterByRole(this.allConfigNav));
  protected readonly integracionesNav = computed(() => this.filterByRole(this.allIntegracionesNav));

  // Períodos para selector AppBar
  protected readonly periodos = signal<PeriodDto[]>([]);
  protected selectedPeriodo = signal<number | null>(this.periodSvc.activeId());

  protected readonly isMobile: ReturnType<typeof toSignal<boolean>>;

  constructor() {
    this.isMobile = toSignal(
      this.breakpointObserver
        .observe([Breakpoints.XSmall, Breakpoints.Small])
        .pipe(map((result) => result.matches)),
      { initialValue: false },
    );
  }

  ngOnInit(): void {
    this.periodSvc.list().subscribe({
      next: (periods) => {
        this.periodos.set(periods);
        if (this.selectedPeriodo() === null && periods.length > 0) {
          // Por defecto el último Abierto, si no el primero
          const abierto = periods.find((p) => p.estado === 'Abierto') ?? periods[0];
          this.selectedPeriodo.set(abierto.id);
          this.periodSvc.setActive(abierto.id);
        }
      },
      error: () => {
        // En backend ausente, dejar el selector vacío sin spam de errores
        this.periodos.set([]);
      },
    });
  }

  protected onChangePeriodo(id: number): void {
    this.selectedPeriodo.set(id);
    this.periodSvc.setActive(id);
  }

  protected toggleSidenav(): void {
    this.sidenavOpen.update((v) => !v);
  }

  protected cerrarSesion(): void {
    this.auth.logout().subscribe({
      next: () => this.notify.info('Sesion cerrada'),
    });
  }

  private filterByRole(items: NavItem[]): NavItem[] {
    const u = this.currentUser();
    if (!u) return items.filter((i) => !i.roles);
    return items.filter((i) => !i.roles || i.roles.some((r) => u.roles.includes(r)));
  }
}
