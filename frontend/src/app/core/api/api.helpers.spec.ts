import { toHttpParams, exportCSV } from './api.helpers';

describe('api.helpers', () => {
  describe('toHttpParams', () => {
    it('incluye valores válidos', () => {
      const p = toHttpParams({ page: 1, pageSize: 25, search: 'foo' });
      expect(p.get('page')).toBe('1');
      expect(p.get('pageSize')).toBe('25');
      expect(p.get('search')).toBe('foo');
    });

    it('omite null, undefined y cadena vacía', () => {
      const p = toHttpParams({ a: 1, b: null, c: undefined, d: '' });
      expect(p.get('a')).toBe('1');
      expect(p.has('b')).toBe(false);
      expect(p.has('c')).toBe(false);
      expect(p.has('d')).toBe(false);
    });

    it('serializa booleanos a string', () => {
      const p = toHttpParams({ flag: true, other: false });
      expect(p.get('flag')).toBe('true');
      expect(p.get('other')).toBe('false');
    });
  });

  describe('exportCSV', () => {
    let createElementSpy: jasmine.Spy;
    let anchorClick: jasmine.Spy;

    beforeEach(() => {
      anchorClick = jasmine.createSpy('click');
      createElementSpy = spyOn(document, 'createElement').and.callFake((tag: string) => {
        if (tag === 'a') {
          return { href: '', download: '', click: anchorClick, setAttribute: () => {} } as any;
        }
        return document.createElement(tag);
      });
      spyOn(document.body, 'appendChild').and.callFake((node: any) => node);
      spyOn(document.body, 'removeChild').and.callFake((node: any) => node);
      spyOn(URL, 'createObjectURL').and.returnValue('blob:fake');
      spyOn(URL, 'revokeObjectURL');
    });

    it('genera y descarga CSV con BOM cuando hay filas', () => {
      exportCSV('test', [{ a: 1, b: 'x' }, { a: 2, b: 'y,z' }]);
      expect(anchorClick).toHaveBeenCalled();
      // El anchor.download debió ser 'test.csv'
      expect(createElementSpy).toHaveBeenCalledWith('a');
    });

    it('no descarga nada cuando no hay filas', () => {
      exportCSV('empty', []);
      expect(anchorClick).not.toHaveBeenCalled();
    });
  });
});
