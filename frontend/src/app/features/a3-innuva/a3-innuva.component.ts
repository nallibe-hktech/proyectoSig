import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';

import { A3InnuvaService, A3InnuvaCompanyDto, A3InnuvaPayrollDto, PagedResult } from '../../core/api/a3-innuva.service';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-a3-innuva',
  standalone: true,
  templateUrl: './a3-innuva.component.html',
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatCardModule,
    MatTabsModule,
    MatInputModule,
    MatFormFieldModule,
    MatSlideToggleModule,
    MatTooltipModule,
    MatSnackBarModule,
  ]
})
export class A3InnuvaComponent implements OnInit, OnDestroy {
  isTestMode = signal(true);
  loading = signal(false);
  companiesLoading = signal(false);
  payrollsLoading = signal(false);
  oauthLoading = signal(false);
  isAuthorized = signal(false);

  companies = signal<A3InnuvaCompanyDto[]>([]);
  employees = signal<any[]>([]);
  payrolls = signal<A3InnuvaPayrollDto[]>([]);
  periods = signal<string[]>([]);

  page = signal(1);
  pageSize = signal(25);
  companiesTotal = signal(0);
  employeesTotal = signal(0);
  payrollsTotal = signal(0);

  employeesLoading = signal(false);
  selectedCompany: string | null = null;
  selectedPeriod: string | null = null;
  lastSyncMessage = '';
  downloadingExcel = signal(false);

  searchCompanies = '';
  searchPayrolls = '';
  searchEmployees = '';

  private readonly TEST_MODE_KEY = 'a3innuva_isTestMode';
  private readonly AUTHORIZED_KEY = 'a3innuva_isAuthorized';
  private destroy$ = new Subject<void>();

  constructor(
    private a3Service: A3InnuvaService,
    private notify: NotifyService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    // Leer estado guardado en localStorage
    const savedTestMode = localStorage.getItem(this.TEST_MODE_KEY);
    const savedAuthorized = localStorage.getItem(this.AUTHORIZED_KEY);

    // Detectar si volvemos del OAuth callback
    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const authorized = params['authorized'] === 'true';

        if (authorized) {
          // Volvemos del OAuth callback: mantener PRODUCCIÓN y marcar como autorizado
          this.isTestMode.set(false);
          this.isAuthorized.set(true);
          localStorage.setItem(this.TEST_MODE_KEY, 'false');
          localStorage.setItem(this.AUTHORIZED_KEY, 'true');
          this.notify.success('✅ Autorización completada. Conectado a Wolters Kluwer.');
        } else if (savedTestMode !== null) {
          // Restaurar estado guardado
          const isTest = savedTestMode === 'true';
          this.isTestMode.set(isTest);
          this.isAuthorized.set(savedAuthorized === 'true' && !isTest);
        } else {
          // Primer acceso: TEST mode por defecto, autorizado localmente
          this.isTestMode.set(true);
          this.isAuthorized.set(true);
        }

        this.loadCompanies();
        this.loadEmployees();
        this.loadPeriods();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onModeChange(): void {
    this.notify.success(this.isTestMode() ? '🧪 Modo TEST activado (datos seguros)' : '⚠️ Modo PRODUCCIÓN (datos reales)');
    // Guardar modo en localStorage
    localStorage.setItem(this.TEST_MODE_KEY, this.isTestMode().toString());

    // En TEST mode, asumir autorizado; en PROD, requiere OAuth real
    if (this.isTestMode()) {
      this.isAuthorized.set(true);
      localStorage.setItem(this.AUTHORIZED_KEY, 'true');
    } else {
      // En PRODUCCIÓN, resetear autorización (debe hacer OAuth)
      this.isAuthorized.set(false);
      localStorage.setItem(this.AUTHORIZED_KEY, 'false');
      this.notify.info('ℹ️ Modo PRODUCCIÓN: necesitas autorizar con tus credenciales de Wolters Kluwer');
    }

    this.loadCompanies();
  }

  initiateOAuth(): void {
    this.oauthLoading.set(true);
    // ⚠️ IMPORTANTE: El backend devuelve la URL de WK con el redirect_uri correcto
    // que está registrado en WK (https://localhost:43971/Login)
    // NO pasamos un redirect_uri del frontend, dejamos que el backend maneje todo
    this.a3Service.getAuthorizeUrl().subscribe({
      next: (response: any) => {
        this.oauthLoading.set(false);
        if (response.authorizeUrl) {
          // Redirigir al usuario a Wolters Kluwer para que se autentique
          // WK redirigirá al backend (https://localhost:43971/Login) después de autenticación
          window.location.href = response.authorizeUrl;
        } else {
          this.notify.error('No se pudo obtener URL de autorización');
        }
      },
      error: (error) => {
        this.oauthLoading.set(false);
        this.notify.error('Error al iniciar autorización: ' + (error.error?.error || error.message));
      }
    });
  }

  syncCompanies(): void {
    this.loading.set(true);
    this.lastSyncMessage = 'Sincronizando empresas...';

    const call = this.isTestMode()
      ? this.a3Service.syncCompaniesTest()
      : this.a3Service.syncCompanies();

    call.subscribe({
      next: (res) => {
        this.loading.set(false);
        this.lastSyncMessage = res.message;
        this.notify.success('✅ ' + res.message);
        this.loadCompanies();
      },
      error: (err) => {
        this.loading.set(false);
        this.lastSyncMessage = '❌ Error: ' + (err.error?.error || err.message);
        this.notify.error('Error sincronizando: ' + (err.error?.error || err.message));
      }
    });
  }

  syncEmployees(): void {
    this.loading.set(true);
    this.lastSyncMessage = 'Sincronizando empleados desde A3 Innuva...';

    const call = this.a3Service.syncEmployees();

    call.subscribe({
      next: (res) => {
        this.loading.set(false);
        this.lastSyncMessage = res.message;
        this.notify.success('✅ ' + res.message);
        this.loadPayrolls();
      },
      error: (err) => {
        this.loading.set(false);
        this.lastSyncMessage = '❌ Error: ' + (err.error?.error || err.message);
        this.notify.error('Error sincronizando: ' + (err.error?.error || err.message));
      }
    });
  }

  syncPayrolls(): void {
    if (!this.selectedCompany) {
      this.notify.warning('Selecciona una empresa primero');
      return;
    }

    this.loading.set(true);
    this.lastSyncMessage = 'Sincronizando nóminas y calculando datos...';

    const call = this.isTestMode()
      ? this.a3Service.syncPayrollsTest(this.selectedCompany)
      : this.a3Service.syncPayrolls(this.selectedCompany);

    call.subscribe({
      next: (res) => {
        this.loading.set(false);
        this.lastSyncMessage = res.message;
        this.notify.success('✅ ' + res.message);
        this.page.set(1);
        this.loadPayrolls();
        this.loadPeriods();
      },
      error: (err) => {
        this.loading.set(false);
        this.lastSyncMessage = '❌ Error: ' + (err.error?.error || err.message);
        this.notify.error('Error sincronizando nóminas: ' + (err.error?.error || err.message));
      }
    });
  }

  loadCompanies(): void {
    this.companiesLoading.set(true);

    const call = this.isTestMode()
      ? this.a3Service.getCompaniesTest(this.page(), this.pageSize(), this.searchCompanies || undefined)
      : this.a3Service.getCompanies(this.page(), this.pageSize(), this.searchCompanies || undefined);

    call.subscribe({
      next: (res: PagedResult<A3InnuvaCompanyDto>) => {
        this.companies.set(res.items);
        this.companiesTotal.set(res.total);
        this.companiesLoading.set(false);
      },
      error: (err) => {
        this.companiesLoading.set(false);
        this.notify.error('Error cargando empresas');
      }
    });
  }

  loadPayrolls(): void {
    if (!this.selectedCompany) return;

    this.payrollsLoading.set(true);

    const call = this.isTestMode()
      ? this.a3Service.getPayrollsTest(this.page(), this.pageSize(), this.searchPayrolls || undefined)
      : this.a3Service.getPayrolls(this.page(), this.pageSize(), this.searchPayrolls || undefined);

    call.subscribe({
      next: (res: PagedResult<A3InnuvaPayrollDto>) => {
        this.payrolls.set(res.items);
        this.payrollsTotal.set(res.total);
        this.payrollsLoading.set(false);
        // Cargar períodos únicos después de cargar nóminas
        this.loadPeriods();
      },
      error: (err) => {
        this.payrollsLoading.set(false);
        this.notify.error('Error cargando nóminas');
      }
    });
  }

  loadEmployees(): void {
    this.employeesLoading.set(true);

    const call = this.isTestMode()
      ? this.a3Service.getEmployeesTest(this.page(), this.pageSize(), this.searchEmployees || undefined)
      : this.a3Service.getEmployees(this.page(), this.pageSize(), this.searchEmployees || undefined);

    call.subscribe({
      next: (res: PagedResult<any>) => {
        this.employees.set(res.items);
        this.employeesTotal.set(res.total);
        this.employeesLoading.set(false);
      },
      error: (err) => {
        this.employeesLoading.set(false);
        this.notify.error('Error cargando empleados');
      }
    });
  }

  loadPeriods(): void {
    // Extraer períodos únicos de las nóminas cargadas
    const periodsSet = new Set(this.payrolls().map(p => p.periodCode));
    const uniquePeriods = Array.from(periodsSet).sort().reverse();
    this.periods.set(uniquePeriods);
  }

  selectCompany(code: string): void {
    this.selectedCompany = code;
    this.page.set(1);
    this.loadPayrolls();
  }

  onPageChange(event: PageEvent, type: 'companies' | 'payrolls' | 'employees'): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);

    if (type === 'companies') {
      this.loadCompanies();
    } else if (type === 'employees') {
      this.loadEmployees();
    } else {
      this.loadPayrolls();
    }
  }

  getUniquePeriods(): string[] {
    return this.periods();
  }

  formatPeriodForDisplay(periodCode: string): string {
    // Convertir "2026-06" a "junio 2026"
    if (!periodCode || periodCode.length !== 7) return periodCode;

    const [year, month] = periodCode.split('-');
    const monthNum = parseInt(month, 10);

    const monthNames = [
      'enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio',
      'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'
    ];

    return `${monthNames[monthNum - 1]} ${year}`;
  }

  downloadExcel(): void {
    if (!this.selectedPeriod) {
      this.notify.warning('Selecciona un período primero');
      return;
    }

    this.downloadingExcel.set(true);
    this.a3Service.generateExcel(this.selectedPeriod).subscribe({
      next: (blob: Blob) => {
        this.downloadingExcel.set(false);
        // Crear un link y descargar el archivo
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `plantilla-a3-innuva-${this.selectedPeriod}.xlsx`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        this.notify.success('✅ Plantilla Excel descargada correctamente');
      },
      error: (err) => {
        this.downloadingExcel.set(false);
        this.notify.error('Error descargando Excel: ' + (err.error?.error || err.message));
      }
    });
  }
}
