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

  private readonly allOperativoNav: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard', testId: 'nav-dashboard' },
    { label: 'Clients', route: '/clients', icon: 'groups', testId: 'nav-clients' },
    { label: 'Incidencias', route: '/incidencias', icon: 'report_problem', testId: 'nav-incidencias' },
    { label: 'Servicios', route: '/services', icon: 'task_alt', testId: 'nav-services' },
    { label: 'Concepts', route: '/concepts', icon: 'calculate', testId: 'nav-concepts' },
    { label: 'Variables', route: '/variables', icon: 'data_object', testId: 'nav-variables' },
    { label: 'Periods', route: '/periods', icon: 'calendar_month', testId: 'nav-periods' },
    { label: 'Approvals', route: '/approvals', icon: 'approval', testId: 'nav-approvals' },
    { label: 'Cierres de Costes', route: '/cierres-costes', icon: 'payments', testId: 'nav-cierres-costes' },
    { label: 'Cierres de Facturación', route: '/cierres-facturacion', icon: 'receipt_long', testId: 'nav-cierres-facturacion' },
    { label: 'Informes', route: '/reports', icon: 'bar_chart', testId: 'nav-reports' },
    { label: 'Forecast', route: '/forecast', icon: 'trending_up', testId: 'nav-forecast' },
  ];

  private readonly allAdminNav: NavItem[] = [
    { label: 'Config. Presupuesto', route: '/config-presupuesto', icon: 'savings', testId: 'nav-config-presupuesto', roles: ['Administrator', 'Fico'] },
    { label: 'Config. Factura', route: '/config-factura', icon: 'request_quote', testId: 'nav-config-factura', roles: ['Administrator', 'Fico'] },
    { label: 'Contratos un día', route: '/contratos-un-dia', icon: 'description', testId: 'nav-contratos-un-dia', roles: ['Administrator', 'Backoffice'] },
    { label: 'Cost Centers', route: '/cost-centers', icon: 'account_balance', testId: 'nav-cost-centers', roles: ['Administrator'] },
    { label: 'Departments', route: '/departments', icon: 'corporate_fare', testId: 'nav-departments', roles: ['Administrator'] },
    { label: 'Roles', route: '/roles', icon: 'verified_user', testId: 'nav-roles', roles: ['Administrator', 'Auditor'] },
    { label: 'Users', route: '/users', icon: 'manage_accounts', testId: 'nav-users', roles: ['Administrator', 'Auditor'] },
    { label: 'Audit Log', route: '/audit', icon: 'history', testId: 'nav-audit', roles: ['Administrator', 'Auditor'] },
    { label: 'Sync', route: '/sync', icon: 'refresh', testId: 'nav-sync', roles: ['Administrator'] },
    { label: 'Celero Visitas', route: '/celero-visitas', icon: 'location_on', testId: 'nav-celero-visitas', roles: ['Administrator'] },
    { label: 'Galán', route: '/galan', icon: 'warehouse', testId: 'nav-galan', roles: ['Administrator'] },
    { label: 'Mediapost', route: '/mediapost', icon: 'local_shipping', testId: 'nav-mediapost', roles: ['Administrator'] },
    { label: 'Bizneo', route: '/bizneo', icon: 'people', testId: 'nav-bizneo', roles: ['Administrator'] },
    { label: 'Intratime', route: '/intratime', icon: 'schedule', testId: 'nav-intratime', roles: ['Administrator'] },
    { label: 'PayHawk', route: '/payhawk', icon: 'receipt_long', testId: 'nav-payhawk', roles: ['Administrator', 'Fico'] },
  ];

  protected readonly operativoNav = computed(() => this.filterByRole(this.allOperativoNav));
  protected readonly adminNav = computed(() => this.filterByRole(this.allAdminNav));

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
