import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
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
    MatSnackBarModule
  ]
})
export class A3InnuvaComponent implements OnInit {
  isTestMode = signal(true);
  loading = signal(false);
  companiesLoading = signal(false);
  payrollsLoading = signal(false);

  companies = signal<A3InnuvaCompanyDto[]>([]);
  payrolls = signal<A3InnuvaPayrollDto[]>([]);

  page = signal(1);
  pageSize = signal(25);
  companiesTotal = signal(0);
  payrollsTotal = signal(0);

  selectedCompany: string | null = null;
  lastSyncMessage = '';

  searchCompanies = '';
  searchPayrolls = '';

  constructor(
    private a3Service: A3InnuvaService,
    private notify: NotifyService
  ) {}

  ngOnInit(): void {
    this.loadCompanies();
  }

  onModeChange(): void {
    this.notify.success(this.isTestMode() ? '🧪 Modo TEST activado (datos seguros)' : '⚠️ Modo PRODUCCIÓN (datos reales)');
    this.loadCompanies();
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

  syncPayrolls(): void {
    if (!this.selectedCompany) {
      this.notify.warning('Selecciona una empresa primero');
      return;
    }

    this.loading.set(true);
    this.lastSyncMessage = `Sincronizando nóminas para ${this.selectedCompany}...`;

    const call = this.isTestMode()
      ? this.a3Service.syncPayrollsTest(this.selectedCompany)
      : this.a3Service.syncPayrolls(this.selectedCompany);

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
      },
      error: (err) => {
        this.payrollsLoading.set(false);
        this.notify.error('Error cargando nóminas');
      }
    });
  }

  selectCompany(code: string): void {
    this.selectedCompany = code;
    this.page.set(1);
    this.loadPayrolls();
  }

  onPageChange(event: PageEvent, type: 'companies' | 'payrolls'): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);

    if (type === 'companies') {
      this.loadCompanies();
    } else {
      this.loadPayrolls();
    }
  }
}
