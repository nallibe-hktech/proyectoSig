import { TestBed, ComponentFixture } from '@angular/core/testing';
import { StateBadgeComponent } from './state-badge.component';
import { EstadoClosure, ApprovalStep } from '../models/enums';

describe('StateBadgeComponent', () => {
  let fixture: ComponentFixture<StateBadgeComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [StateBadgeComponent] });
    fixture = TestBed.createComponent(StateBadgeComponent);
  });

  it('renderiza el badge para estado Aprobado', () => {
    fixture.componentRef.setInput('estado', 'Aprobado' as EstadoClosure);
    fixture.componentRef.setInput('paso', 'SystemExports' as ApprovalStep);
    fixture.detectChanges();
    const span = fixture.nativeElement.querySelector('[data-testid=badge-estado]');
    expect(span).toBeTruthy();
    expect(span.className).toContain('sig-badge');
  });

  it('renderiza badge para estado Borrador con paso PM', () => {
    fixture.componentRef.setInput('estado', 'Borrador' as EstadoClosure);
    fixture.componentRef.setInput('paso', 'ProjectManager' as ApprovalStep);
    fixture.detectChanges();
    const span = fixture.nativeElement.querySelector('[data-testid=badge-estado]');
    expect(span).toBeTruthy();
    expect(span.textContent.length).toBeGreaterThan(0);
  });

  it('renderiza badge para Rechazado', () => {
    fixture.componentRef.setInput('estado', 'Rechazado' as EstadoClosure);
    fixture.componentRef.setInput('paso', 'Backoffice' as ApprovalStep);
    fixture.detectChanges();
    const span = fixture.nativeElement.querySelector('[data-testid=badge-estado]');
    expect(span).toBeTruthy();
  });
});
