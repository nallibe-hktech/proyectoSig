# Motor de cálculo de cierres — explicado punto por punto

> Documento para entender, de principio a fin, cómo el sistema calcula los importes de
> **pagos** (costes) y **facturación**. Lenguaje llano, con ejemplos numéricos que puedes
> reproducir. Refleja el estado tras integrar la lógica del Excel *CierresIntegralesSIG*.

---

## 1. La idea en una frase

> **Cada concepto tiene una "fórmula", y la fórmula es un arbolito de operaciones que el motor recorre para obtener un número.**

- Un **Concepto** es algo que se paga o se factura: "Dietas", "Cuota por visita", "Fee mensual"…
- Cada concepto es de tipo **Pago** (va al cierre de **costes**) o **Factura** (va al cierre de **facturación**).
- La fórmula no es texto tipo Excel (`=A1*B2`); es un **árbol** guardado en JSON. Cada "caja" del árbol es un **nodo**.

Ejemplo mental: *"cuenta las visitas y multiplícalas por 5 €"* se guarda así:

```
   ( × )                ← multiplicar
   /   \
 Cuenta  5              ← un agregado y un número
   |
 Visitas
```

---

## 2. El recorrido completo de un cierre (de principio a fin)

Cuando se crea o recalcula un cierre (de costes o de facturación), pasa esto:

1. **Se eligen los conceptos aplicables**: los del tipo correcto (Pago/Factura), vigentes en el
   período y del servicio (o globales).
2. **Se evalúa cada concepto** → el motor coge su fórmula y la calcula, dando un número.
3. **Se crea una línea por concepto** con ese importe.
4. **El total del cierre** = suma de las líneas.
5. **Se guarda la traza** de cada cálculo (`CalculationLog`): la fórmula usada, los datos de
   entrada y el resultado. Así siempre se puede auditar de dónde sale cada euro.

> 🔁 **Dos pasadas**: primero se calculan los conceptos "normales" (base) y después los que
> dependen de otros (el *fee sobre conceptos*, ver punto 7). Cada concepto se evalúa **una sola vez**.

Y dentro de "evaluar un concepto" (paso 2):

1. Se **lee la fórmula** (el JSON) y se convierte en el árbol de nodos.
2. Se **cargan los datos** del período y servicio (visitas, gastos, horas…).
3. Se **recorre el árbol** de abajo a arriba, calculando cada nodo.
4. Se **redondea a 2 decimales** y se devuelve el número + las incidencias detectadas.

---

## 3. Los nodos, uno por uno

Cada nodo es una pieza de Lego. Se combinan para formar cualquier fórmula.

### 3.1 `Number` — un número fijo
El más simple. Devuelve siempre el mismo valor.

- **Para qué del Excel**: "Cantidad fija mensual" (una cuota fija de 1.500 €).
- **Ejemplo**: `{ "type": "Number", "value": 1500 }` → **1500**.

### 3.2 `Variable` — un valor parametrizable
Apunta a una "Variable" del sistema (p. ej. "TarifaHora").

- **Para qué del Excel**: tarifas que cambian por cliente sin tocar la fórmula.
- **Ejemplo**: `{ "type": "Variable", "variableId": 4 }` → el valor de esa variable (p. ej. 18,5).
- ⚠️ **Hoy es básico**: devuelve un valor constante de la variable. La segmentación real
  (por respuesta de Celero) se hace con filtros sobre el `PayloadJson` (punto 6), no aquí.

### 3.3 `Source` — la entidad de datos (de dónde salen las filas)
No se calcula solo; siempre va dentro de un `Aggregate`. Indica **qué tabla** mirar y con **qué filtros**.

- **Entidades disponibles**: `VisitasCelero`, `GastosPayHawk`, `HorasBizneo`, `HorasIntratime`,
  `VisitasSgpv`, `TarifasServicio`.
- **Filtros**: campo + operador (`=`, `≠`, `>`, `≥`, `<`, `≤`, `∈ en lista`) + valor.
- **Ejemplo**: "visitas de tipo 2" →
  `{ "type": "Source", "entity": "VisitasCelero", "filters": [{ "field": "TipoVisita", "op": "Eq", "value": 2 }] }`

### 3.4 `Aggregate` — resumir muchas filas en un número
Coge las filas de un `Source` y las reduce: contar, sumar, mínimo o máximo.

- **Operaciones**: `Count` (contar filas), `Sum` (sumar un campo), `Min`, `Max`.
- **`distinct`** (novedad): con `Count`, cuenta **valores únicos** de un campo.
  - **Para qué del Excel**: "conteo de **días con actividad**" (aunque haya 3 visitas el mismo
    día, cuenta 1 día). Se pone `"distinct": "Fecha"`.
- **Ejemplos**:
  - Contar visitas: `{ "type": "Aggregate", "op": "Count", "source": {…} }` → nº de visitas.
  - Sumar horas: `{ "type": "Aggregate", "op": "Sum", "field": "Horas", "source": {…} }`.
  - Días con actividad: `{ "type": "Aggregate", "op": "Count", "distinct": "Fecha", "source": {…} }`.

### 3.5 `BinaryOp` — operación entre dos cosas
Toma dos sub-resultados (izquierda y derecha) y los combina.

- **Operadores**: `Add` (+), `Sub` (−), `Mul` (×), `Div` (÷), `Pct` (%).
- ⚠️ **Ojo con `Pct`**: significa **"añadir ese %"**, no "el % de". Es decir
  `Pct(100, 15) = 100 × (1 + 15/100) = 115`. Sirve para "refacturar con +15% de margen".
  Si quieres "el 15% de algo", usa `Mul` por 0,15.
- **Ejemplo (visitas × tarifa)**:
  ```json
  { "type": "BinaryOp", "op": "Mul",
    "left":  { "type": "Aggregate", "op": "Count", "source": { "type": "Source", "entity": "VisitasCelero", "filters": [] } },
    "right": { "type": "Number", "value": 5 } }
  ```
  Con 3 visitas → **15**.

### 3.6 `Modifier` — aplicar un tope o ajuste a un resultado *(novedad)*
Envuelve otra expresión y le aplica una regla. Cubre **todos los "FILTROS" del Excel**.

| `kind` | Qué hace | Regla del Excel |
|---|---|---|
| `Min` | Si el resultado es menor que X, devuelve X | **Cantidad mínima** (suelo) |
| `Max` | Si el resultado es mayor que X, devuelve X | **Cantidad máxima** (techo) |
| `FloorZero` | Si el resultado es menor que X, devuelve **0** | **Rendimiento / umbral mínimo** |
| `Franquicia` | Resta X y nunca baja de 0 | **Los primeros X no contabilizan** |

- **Ejemplo (franquicia de km)**: los primeros 300 km no se pagan; si hay 315 →
  `Franquicia[300](315) = 315 − 300 = 15`.
- **Ejemplo (suelo)**: `Min[250](100) = 250` (aunque salga 100, se paga el mínimo 250).

### 3.7 `Tramos` — tarifa por tramos incrementales *(novedad)*
Calcula un precio "por escalones": la 1ª unidad a un precio, las siguientes a otro.

- **Para qué del Excel**: "1ª hora 90 €, siguientes 37 €" (Molins, Inpost…).
- Cada tramo tiene `hasta` (límite acumulado de unidades; `null` = "el resto") y `precio` por unidad.
- **Ejemplo**: cantidad = 3, tramos `[{hasta:1, precio:90}, {hasta:null, precio:37}]`
  → `1×90 + 2×37 = ` **164**.

### 3.8 `ConceptRef` — fee sobre otros conceptos *(novedad)*
No mira datos crudos: mira el **importe de otras líneas del mismo cierre**.

- **Para qué del Excel**: "Fee del 6,5% sobre el total de conceptos" (Granini, JDE, ITC…).
- `conceptIds` vacío = suma **todos** los conceptos base del cierre; con IDs, solo esos.
- Se calcula en la **2ª pasada** (cuando los conceptos base ya tienen importe).
- **Ejemplo**: conceptos base suman 500 → fee 10% =
  `Mul( ConceptRef[], 0.10 ) = ` **50**.

---

## 4. Filtros: explícitos e implícitos

Cuando un `Aggregate` lee un `Source`, **siempre** se aplican filtros automáticos (implícitos),
y además los que tú pongas (explícitos):

- **Implícitos (automáticos)**:
  - **Período**: solo filas con fecha dentro del cierre.
  - **Servicio**: solo filas de ese servicio.
  - **Recurso** (opcional): si se calcula para un empleado concreto, solo sus filas.
- **Explícitos (los que defines)**: `TipoVisita = 2`, `Categoria = "dieta"`, etc.

> Si tras los filtros **no queda ninguna fila**, el agregado devuelve **0** y se anota una
> incidencia `EmptyDataset` (aviso, no error).

---

## 5. Resultado y trazabilidad

Cada evaluación devuelve:
- **Resultado** (redondeado a 2 decimales).
- **InputsJson**: foto de qué entró (nº de filas de cada fuente, período, servicio…).
- **FormulaSnapshotJson**: la fórmula exacta usada (por si luego cambia).
- **SistemaOrigen**: de qué integración salieron los datos (PayHawk, Celero…, "Mixto" o "Ninguno").
- **Incidencias**: avisos como `EmptyDataset`, `DivisionByZero`, `SinConceptosPrevios`.

Todo esto se guarda en `CalculationLog` → auditoría completa de cada línea.

---

## 6. Cómo se segmentan las visitas (tipo / zona / km)

Las visitas de Celero **no tienen columnas** de "tipo de visita" o "zona": esos datos vienen en
un bloque crudo (`PayloadJson`). El motor lo **abre** y expone sus claves para poder filtrar.

- Claves conocidas que se mapean directas: `tipoVisita`, `puntoMontado`, `zona`, `km`, `horas`,
  `importe`, `categoria`.
- El resto de claves quedan disponibles igualmente para filtrar (diccionario `Extra`).
- Así, "cuota según tipo de visita" se expresa como
  `Count(VisitasCelero, filtro TipoVisita = 2) × tarifa`.

---

## 7. Qué cubre del Excel (resumen de verificación)

| Lógica del Excel | Nodo(s) que la implementan | ¿Antes / Ahora? |
|---|---|---|
| Cantidad fija mensual | `Number` | Antes |
| Conteo de visitas × cantidad | `Aggregate(Count)` × `Number` | Antes |
| Conteo de **días con actividad** × cantidad | `Aggregate(Count, distinct:Fecha)` | **Ahora** |
| Suma de **Km** × coste/km | `Aggregate(Sum, Km)` × `Number` | **Ahora** |
| Conteo/Suma Entidad-A × Entidad-B | `Mul(Aggregate, Aggregate)` | Antes |
| Porcentaje de entidad | `Mul` por (x/100) | Antes |
| **% fijo / Fee sobre conceptos** | `ConceptRef` | **Ahora** |
| Conteo de horas × **cantidad incremental** | `Tramos` | **Ahora** |
| FILTRO cantidad mínima / máxima | `Modifier(Min/Max)` | **Ahora** |
| FILTRO rendimiento/umbral mínimo | `Modifier(FloorZero)` | **Ahora** |
| FILTRO franquicia | `Modifier(Franquicia)` | **Ahora** |
| Segmentar por tipo/zona/mueble de visita | filtros sobre `PayloadJson` | **Ahora** |

✅ **Conclusión: el catálogo de cálculo del Excel está cubierto al 100%.**

---

## 8. Límites y decisiones (lo que NO es del motor)

Honestidad sobre los bordes (ver `SUPOSICIONES_CRITICAS.md` SUP-10):

1. **Las tarifas reales** de cada cliente son **datos** (van en la base de datos), no código.
2. **`Variable`** devuelve hoy una constante (no el mapeo real idQuestion→valor). La
   segmentación se hace por filtros sobre `PayloadJson`.
3. **Logística Galán/MDP** no es una fuente de datos (no hay coste en los staging): se modela
   como tarifa/gasto + margen.
4. **Fee sobre fee** (multinivel) no está soportado (no aparece en el Excel).
5. **Excepciones** (2ª/3ª visita, fallida, cancelación, nocturnidad, adelantos, embargos) se
   resuelven con líneas manuales (override / incentivo) + filtros, no con reglas automáticas.
6. El cierre genera **una línea agregada por concepto** (no una por empleado); la matriz
   empleados×conceptos se deriva en la interfaz.

---

## 9. Cómo comprobarlo tú mismo

- **Tests del motor**: `backend/SIG.Tests/Unit/Calculation/CalculationEngineTests.cs`
  (50 tests; cada nodo tiene su prueba con números reproducibles). Ejecutar:
  `dotnet test --filter "FullyQualifiedName~Calculation"`.
- **Conceptos de ejemplo** (datos anónimos, uno por novedad): `backend/SIG.Infrastructure/Seed/DataSeeder.cs`,
  buscar `EJEMPLOS Excel` (Modifier Min, Franquicia, Tramos, ConceptRef).
- **Editor visual**: en la app, un concepto → "Editor de fórmula": ahí están las primitivas
  Modificador, Tramos y Fee s/conceptos en la paleta.

---

### Apéndice — un ejemplo completo encadenado

*"Kilometraje: los primeros 300 km del mes no se pagan; el resto a 0,23 €/km."*

```json
{ "type": "BinaryOp", "op": "Mul",
  "left": {
    "type": "Modifier", "kind": "Franquicia", "threshold": 300,
    "inner": { "type": "Aggregate", "op": "Sum", "field": "Km",
               "source": { "type": "Source", "entity": "VisitasCelero", "filters": [] } }
  },
  "right": { "type": "Number", "value": 0.23 }
}
```

Lectura: *suma de km* → le quito la franquicia de 300 → lo multiplico por 0,23.
Con 315 km: `(315 − 300) × 0,23 = 15 × 0,23 =` **3,45 €**.
