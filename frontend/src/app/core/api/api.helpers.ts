import { HttpParams } from '@angular/common/http';

// Convierte un objeto en HttpParams omitiendo null/undefined/empty.
export function toHttpParams(obj: Record<string, unknown>): HttpParams {
  let p = new HttpParams();
  for (const [k, v] of Object.entries(obj)) {
    if (v === null || v === undefined || v === '') continue;
    p = p.set(k, String(v));
  }
  return p;
}

// Helper para descargar un Blob como archivo
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

// Helper para exportar CSV cliente-side a partir de filas
export function exportCSV(filename: string, rows: Record<string, unknown>[]): void {
  if (rows.length === 0) {
    return;
  }
  const headers = Object.keys(rows[0]);
  const escape = (val: unknown): string => {
    if (val === null || val === undefined) return '';
    const s = String(val).replace(/"/g, '""');
    return /[",\n;]/.test(s) ? `"${s}"` : s;
  };
  const csv = [
    headers.join(';'),
    ...rows.map((r) => headers.map((h) => escape(r[h])).join(';')),
  ].join('\n');
  // BOM para Excel
  const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' });
  downloadBlob(blob, filename.endsWith('.csv') ? filename : `${filename}.csv`);
}
