import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';
import { A3InnuvaNominasService, A3InnuvaNominaDto, PeriodoDto } from '../../core/api/a3-innuva-nominas.service';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-a3-innuva-nominas',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTabsModule,
    MatTableModule,
  ],
  templateUrl: './a3-innuva-nominas.component.html',
  styleUrls: ['./a3-innuva-nominas.component.scss'],
})
export class A3InnuvaNominasComponent implements OnInit {
  // OAuth State
  isAuthorized = signal(false);
  oauthLoading = signal(false);
  private readonly AUTHORIZED_KEY = 'a3innuva_nominas_authorized';

  // PHASE 1 State
  loadingPhase1 = signal(false);
  loadingPhase2 = signal(false);
  loadingPhase3 = signal(false);
  syncStatus = signal('');

  // PHASE 2: Nóminas Calculadas
  nominasCalculadas = signal<A3InnuvaNominaDto[]>([]);
  pageCalculadas = signal(1);
  pageSizeCalculadas = signal(25);
  totalCalculadas = signal(0);
  searchCalculadas = '';

  // PHASE 3: Nóminas Enviadas
  nominasEnviadas = signal<A3InnuvaNominaDto[]>([]);
  pageEnviadas = signal(1);
  pageSizeEnviadas = signal(25);
  totalEnviadas = signal(0);
  searchEnviadas = '';

  // PHASE 1 Details: Empresas, Nóminas, Empleados, Conceptos
  empresas = signal<any[]>([]);
  pageEmpresas = signal(1);
  pageSizeEmpresas = signal(25);
  totalEmpresas = signal(0);
  searchEmpresas = '';

  nominas = signal<any[]>([]);
  pageNominas = signal(1);
  pageSizeNominas = signal(25);
  totalNominas = signal(0);
  searchNominas = '';

  empleados = signal<any[]>([]);
  pageEmpleados = signal(1);
  pageSizeEmpleados = signal(25);
  totalEmpleados = signal(0);
  searchEmpleados = '';

  conceptos = signal<any[]>([]);
  pageConceptos = signal(1);
  pageSizeConceptos = signal(25);
  totalConceptos = signal(0);
  searchConceptos = '';

  // Filtros
  periodCode = '';
  periods = signal<PeriodoDto[]>([]);

  // Display
  displayedColumnsCalculadas = ['fechaContrato', 'codigoEmpleado', 'nombreEmpleado', 'fecha', 'importeTotal'];
  displayedColumnsEnviadas = ['fechaContrato', 'codigoEmpleado', 'nombreEmpleado', 'fecha', 'importeTotal'];
  displayedColumnsEmpresas = ['codigo', 'nombre', 'nif', 'ciudad', 'pais'];
  displayedColumnsNominas = ['idEmpleado', 'nombreEmpleado', 'codigoPeriodo', 'salarioBase', 'salarioNeto'];
  displayedColumnsEmpleados = ['nif', 'nombre', 'departamento', 'sueldoMensual', 'fechaUltimaSincronizacion'];
  displayedColumnsConceptos = ['codigoEmpleado', 'nombreEmpleado', 'descripcionConcepto', 'tipoConcepto', 'importe'];

  constructor(
    private service: A3InnuvaNominasService,
    private notify: NotifyService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Restaurar estado de autorización desde localStorage
    const saved = localStorage.getItem(this.AUTHORIZED_KEY);
    if (saved === 'true') this.isAuthorized.set(true);

    // Detectar parámetro ?authorized=true desde callback de OAuth
    this.route.queryParams.subscribe(params => {
      if (params['authorized'] === 'true') {
        this.isAuthorized.set(true);
        localStorage.setItem(this.AUTHORIZED_KEY, 'true');
        this.notify.success('✅ Autorización completada con Wolters Kluwer');
      }
    });

    // Cargar periodos para los filtros
    this.loadPeriods();

    // Si está autorizado, cargar las nóminas
    if (this.isAuthorized()) {
      this.loadNominasCalculadas();
      this.loadNominasEnviadas();
    }
  }

  private loadPeriods(): void {
    this.service.getPeriods().subscribe({
      next: (periods: PeriodoDto[]) => {
        this.periods.set(periods);
      },
      error: (err: any) => {
        console.error('Error cargando periodos:', err);
        this.notify.error('Error al cargar periodos');
      }
    });
  }

  initiateOAuth(): void {
    this.oauthLoading.set(true);
    this.service.getAuthorizeUrl().subscribe({
      next: (res: { authorizeUrl: string; redirectUri: string; message: string }) => {
        this.oauthLoading.set(false);
        window.location.href = res.authorizeUrl;
      },
      error: (err: any) => {
        this.oauthLoading.set(false);
        this.notify.error('Error al iniciar autorización: ' + (err.error?.error || err.message));
      }
    });
  }

  // ============ PHASE 1: SYNC PHASE ============
  syncPhase1(): void {
    const confirmMsg = '¿Ejecutar PHASE 1? (Sincronizar empresas → empleados → nóminas → conceptos → salarios → IRPF → remuneraciones → cuentas bancarias → acuerdos)';
    if (!confirm(confirmMsg)) return;

    this.loadingPhase1.set(true);
    this.syncStatus.set('⏳ Sincronizando empresas...');

    this.service.syncCompanies().subscribe({
      next: (): void => {
        this.syncStatus.set('✅ Empresas sincronizadas. Sincronizando empleados...');
        this.syncEmployeesInternal();
      },
      error: (err: any) => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('❌ Error sincronizando empresas');
        this.notify.error('Error en PHASE 1 (companies): ' + (err.error?.error || err.message));
      }
    });
  }

  private syncEmployeesInternal(): void {
    this.service.syncEmployees().subscribe({
      next: (): void => {
        this.syncStatus.set('✅ Empleados sincronizados. Sincronizando nóminas...');
        this.syncPayrollsInternal();
      },
      error: (err: any) => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('❌ Error sincronizando empleados');
        this.notify.error('Error en PHASE 1 (employees): ' + (err.error?.error || err.message));
      }
    });
  }

  private syncPayrollsInternal(): void {
    this.service.syncPayrolls('1').subscribe({
      next: (): void => {
        this.syncStatus.set('✅ Nóminas sincronizadas. Sincronizando conceptos...');
        this.syncConceptosInternal();
      },
      error: (err: any) => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('❌ Error sincronizando nóminas');
        this.notify.error('Error en PHASE 1 (payrolls): ' + (err.error?.error || err.message));
      }
    });
  }

  private syncConceptosInternal(): void {
    this.service.syncConceptos().subscribe({
      next: (): void => {
        this.syncStatus.set('✅ Conceptos sincronizados. Sincronizando datos de salarios...');
        this.syncSalaryInternal();
      },
      error: (err: any) => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('❌ Error sincronizando conceptos');
        this.notify.error('Error en PHASE 1 (concepts): ' + (err.error?.error || err.message));
      }
    });
  }

  // ============ PHASE 1 REDESIGNED: Real Wolters Kluwer Data ============
  private syncSalaryInternal(): void {
    this.service.syncSalary().subscribe({
      next: (): void => {
        this.syncStatus.set('✅ Salarios sincronizados. Sincronizando IRPF...');
        this.syncIRPFInternal();
      },
      error: (err: any) => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('❌ Error sincronizando salarios');
        this.notify.error('Error en PHASE 1 (salary): ' + (err.error?.error || err.message));
      }
    });
  }

  private syncIRPFInternal(): void {
    this.service.syncIRPF().subscribe({
      next: (): void => {
        this.syncStatus.set('✅ IRPF sincronizado. Sincronizando remuneraciones...');
        this.syncRemunerationInternal();
      },
      error: (err: any) => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('❌ Error sincronizando IRPF');
        this.notify.error('Error en PHASE 1 (irpf): ' + (err.error?.error || err.message));
      }
    });
  }

  private syncRemunerationInternal(): void {
    this.service.syncRemuneration().subscribe({
      next: (): void => {
        this.syncStatus.set('✅ Remuneraciones sincronizadas. Sincronizando cuentas bancarias...');
        this.syncBankAccountsInternal();
      },
      error: (err: any) => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('❌ Error sincronizando remuneraciones');
        this.notify.error('Error en PHASE 1 (remuneration): ' + (err.error?.error || err.message));
      }
    });
  }

  private syncBankAccountsInternal(): void {
    this.service.syncBankAccounts().subscribe({
      next: (): void => {
        this.syncStatus.set('✅ Cuentas bancarias sincronizadas. Sincronizando acuerdos colectivos...');
        this.syncAgreementsInternal();
      },
      error: (err: any) => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('❌ Error sincronizando cuentas bancarias');
        this.notify.error('Error en PHASE 1 (bank_accounts): ' + (err.error?.error || err.message));
      }
    });
  }

  private syncAgreementsInternal(): void {
    this.service.syncAgreements().subscribe({
      next: (): void => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('✅ PHASE 1 completada. Todos los datos sincronizados.');
        this.loadNominasCalculadas();
        this.loadAllPhase1Details(); // Cargar detalles de empresas, nóminas, empleados, conceptos
        this.notify.success('✅ PHASE 1 completada exitosamente');
      },
      error: (err: any) => {
        this.loadingPhase1.set(false);
        this.syncStatus.set('❌ Error sincronizando acuerdos');
        this.notify.error('Error en PHASE 1 (agreements): ' + (err.error?.error || err.message));
      }
    });
  }

  // ============ PHASE 2: CALCULATE PHASE ============
  syncPhase2(): void {
    if (!this.periodCode) {
      this.notify.error('Selecciona un período antes de calcular');
      return;
    }

    const confirmMsg = `¿Ejecutar PHASE 2? (Calcular nóminas para período ${this.periodCode})`;
    if (!confirm(confirmMsg)) return;

    this.loadingPhase2.set(true);
    this.syncStatus.set('⏳ Calculando nóminas...');

    this.service.calculatePayrolls(this.periodCode).subscribe({
      next: (): void => {
        this.loadingPhase2.set(false);
        this.syncStatus.set('✅ PHASE 2 completada. Nóminas calculadas.');
        this.loadNominasCalculadas();
        this.notify.success('✅ PHASE 2 completada exitosamente');
      },
      error: (err: any) => {
        this.loadingPhase2.set(false);
        this.syncStatus.set('❌ Error calculando nóminas');
        this.notify.error('Error en PHASE 2: ' + (err.error?.error || err.message));
      }
    });
  }

  // ============ PHASE 3: WRITE PHASE (optional) ============
  syncPhase3(): void {
    if (!this.periodCode) {
      this.notify.error('Selecciona un período antes de escribir');
      return;
    }

    const confirmMsg = `¿Ejecutar PHASE 3? (Escribir nóminas a Wolters Kluwer para período ${this.periodCode})`;
    if (!confirm(confirmMsg)) return;

    this.loadingPhase3.set(true);
    this.syncStatus.set('⏳ Escribiendo nóminas en Wolters Kluwer...');

    this.service.writePayrolls(this.periodCode).subscribe({
      next: (): void => {
        this.loadingPhase3.set(false);
        this.syncStatus.set('✅ PHASE 3 completada. Nóminas escritas en Wolters Kluwer.');
        this.loadNominasEnviadas();
        this.notify.success('✅ PHASE 3 completada exitosamente');
      },
      error: (err: any) => {
        this.loadingPhase3.set(false);
        this.syncStatus.set('❌ Error escribiendo nóminas');
        this.notify.error('Error en PHASE 3: ' + (err.error?.error || err.message));
      }
    });
  }

  // ============ DATA LOADING ============
  private loadNominasCalculadas(): void {
    this.service.getNominasCalculadas(
      this.pageCalculadas(),
      this.pageSizeCalculadas(),
      this.periodCode || undefined,
      this.searchCalculadas || undefined
    ).subscribe({
      next: (res: any) => {
        this.nominasCalculadas.set(res.items || []);
        this.totalCalculadas.set(res.total || 0);
      },
      error: (err: any) => {
        console.error('Error cargando nóminas calculadas:', err);
        this.notify.error('Error al cargar nóminas calculadas');
      }
    });
  }

  private loadNominasEnviadas(): void {
    this.service.getNominasEnviadas(
      this.pageEnviadas(),
      this.pageSizeEnviadas(),
      this.periodCode || undefined,
      this.searchEnviadas || undefined
    ).subscribe({
      next: (res) => {
        this.nominasEnviadas.set(res.items || []);
        this.totalEnviadas.set(res.total || 0);
      },
      error: (err: any) => {
        console.error('Error cargando nóminas enviadas:', err);
        this.notify.error('Error al cargar nóminas enviadas');
      }
    });
  }

  // ============ PHASE 1 DETAILS: EMPRESAS, NÓMINAS, EMPLEADOS, CONCEPTOS ============
  private loadEmpresas(): void {
    this.service.getCompanies(
      this.pageEmpresas(),
      this.pageSizeEmpresas(),
      this.searchEmpresas || undefined
    ).subscribe({
      next: (res: any) => {
        this.empresas.set(res.items || []);
        this.totalEmpresas.set(res.total || 0);
      },
      error: (err: any) => {
        console.error('Error cargando empresas:', err);
        this.notify.error('Error al cargar empresas');
      }
    });
  }

  private loadNominasDetails(): void {
    this.service.getPayrolls(
      this.pageNominas(),
      this.pageSizeNominas(),
      this.searchNominas || undefined
    ).subscribe({
      next: (res: any) => {
        this.nominas.set(res.items || []);
        this.totalNominas.set(res.total || 0);
      },
      error: (err: any) => {
        console.error('Error cargando nóminas:', err);
        this.notify.error('Error al cargar nóminas');
      }
    });
  }

  private loadEmpleados(): void {
    this.service.getEmployees(
      this.pageEmpleados(),
      this.pageSizeEmpleados(),
      this.searchEmpleados || undefined
    ).subscribe({
      next: (res: any) => {
        this.empleados.set(res.items || []);
        this.totalEmpleados.set(res.total || 0);
      },
      error: (err: any) => {
        console.error('Error cargando empleados:', err);
        this.notify.error('Error al cargar empleados');
      }
    });
  }

  private loadConceptos(): void {
    this.service.getConceptos(
      this.pageConceptos(),
      this.pageSizeConceptos(),
      this.searchConceptos || undefined
    ).subscribe({
      next: (res: any) => {
        this.conceptos.set(res.items || []);
        this.totalConceptos.set(res.total || 0);
      },
      error: (err: any) => {
        console.error('Error cargando conceptos:', err);
        this.notify.error('Error al cargar conceptos');
      }
    });
  }

  // Llamar a loadEmpresas/Nominas/Empleados/Conceptos después de PHASE 1 completada
  private loadAllPhase1Details(): void {
    this.loadEmpresas();
    this.loadNominasDetails();
    this.loadEmpleados();
    this.loadConceptos();
  }

  // ============ PAGINATION & SEARCH ============
  onPageChangeCalculadas(event: PageEvent): void {
    this.pageCalculadas.set(event.pageIndex + 1);
    this.pageSizeCalculadas.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.loadNominasCalculadas();
  }

  onPageChangeEnviadas(event: PageEvent): void {
    this.pageEnviadas.set(event.pageIndex + 1);
    this.pageSizeEnviadas.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.loadNominasEnviadas();
  }

  onPeriodChange(): void {
    this.pageCalculadas.set(1);
    this.pageEnviadas.set(1);
    this.loadNominasCalculadas();
    this.loadNominasEnviadas();
  }

  onSearchCalculadas(): void {
    this.pageCalculadas.set(1);
    this.loadNominasCalculadas();
  }

  onSearchEnviadas(): void {
    this.pageEnviadas.set(1);
    this.loadNominasEnviadas();
  }

  onPageChangeEmpresas(event: PageEvent): void {
    this.pageEmpresas.set(event.pageIndex + 1);
    this.pageSizeEmpresas.set(event.pageSize);
    this.loadEmpresas();
  }

  onPageChangeNominas(event: PageEvent): void {
    this.pageNominas.set(event.pageIndex + 1);
    this.pageSizeNominas.set(event.pageSize);
    this.loadNominasDetails();
  }

  onPageChangeEmpleados(event: PageEvent): void {
    this.pageEmpleados.set(event.pageIndex + 1);
    this.pageSizeEmpleados.set(event.pageSize);
    this.loadEmpleados();
  }

  onPageChangeConceptos(event: PageEvent): void {
    this.pageConceptos.set(event.pageIndex + 1);
    this.pageSizeConceptos.set(event.pageSize);
    this.loadConceptos();
  }

  onSearchEmpresas(): void {
    this.pageEmpresas.set(1);
    this.loadEmpresas();
  }

  onSearchNominas(): void {
    this.pageNominas.set(1);
    this.loadNominasDetails();
  }

  onSearchEmpleados(): void {
    this.pageEmpleados.set(1);
    this.loadEmpleados();
  }

  onSearchConceptos(): void {
    this.pageConceptos.set(1);
    this.loadConceptos();
  }
}
