import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import {
  FormulaNode,
  NodeType,
  NodeData,
  Connection,
  EditorState,
  ValidationError,
  PaletteItem,
  EntityMetadata,
  EntityField,
  OperationConfig,
  OperationType,
  Formula,
  ExportResult,
  HistoryEntry,
  EditorEvent,
} from '../models/formula.model';

/**
 * Servicio central para gestionar el editor visual de fórmulas
 * Responsable de:
 * - Crear, actualizar, eliminar nodos
 * - Gestionar conexiones entre nodos
 * - Validar la estructura de la fórmula
 * - Exportar/importar JSON
 * - Manejar undo/redo
 */
@Injectable({
  providedIn: 'root',
})
export class FormulaBuilderService {
  private state$ = new BehaviorSubject<EditorState>({
    nodes: new Map(),
    connections: new Map(),
    isDirty: false,
    validationErrors: [],
  });

  private events$ = new BehaviorSubject<EditorEvent | null>(null);
  private history: HistoryEntry[] = [];
  private historyIndex = -1;

  // Metadatos de entidades disponibles
  private entities: EntityMetadata[] = [
    {
      id: 'celero_visita',
      label: 'Celero Visita',
      icon: 'location_on',
      fields: [{ name: 'duracion_minutos', label: 'Duración (minutos)', type: 'numero' }],
    },
    {
      id: 'payhawk_gasto',
      label: 'PayHawk Gasto',
      icon: 'payment',
      fields: [{ name: 'monto', label: 'Monto', type: 'numero' }],
    },
    {
      id: 'bizneo_hora',
      label: 'Bizneo Hora',
      icon: 'schedule',
      fields: [
        { name: 'horas', label: 'Horas', type: 'numero' },
        { name: 'aprobadas', label: 'Aprobadas', type: 'booleano' },
      ],
    },
    {
      id: 'intratime_fichaje',
      label: 'Intratime Fichaje',
      icon: 'access_time',
      fields: [{ name: 'tiempo_total', label: 'Tiempo Total', type: 'numero' }],
    },
    {
      id: 'sgpv_producto',
      label: 'SGPV Producto',
      icon: 'inventory',
      fields: [
        { name: 'cantidad', label: 'Cantidad', type: 'numero' },
        { name: 'precio', label: 'Precio', type: 'numero' },
      ],
    },
  ];

  // Configuración de operaciones
  private operations: OperationConfig[] = [
    {
      id: 'suma',
      label: 'Suma',
      symbol: '+',
      icon: 'add',
      minOperands: 2,
      maxOperands: 2,
      description: 'Suma dos valores',
    },
    {
      id: 'resta',
      label: 'Resta',
      symbol: '−',
      icon: 'remove',
      minOperands: 2,
      maxOperands: 2,
      description: 'Resta dos valores',
    },
    {
      id: 'multiplica',
      label: 'Multiplica',
      symbol: '×',
      icon: 'close',
      minOperands: 2,
      maxOperands: 2,
      description: 'Multiplica dos valores',
    },
    {
      id: 'divide',
      label: 'Divide',
      symbol: '÷',
      icon: 'trending_down',
      minOperands: 2,
      maxOperands: 2,
      description: 'Divide dos valores',
    },
    {
      id: 'modulo',
      label: 'Módulo',
      symbol: '%',
      icon: 'percent',
      minOperands: 2,
      maxOperands: 2,
      description: 'Resto de la división',
    },
    {
      id: 'promedio',
      label: 'Promedio',
      symbol: '∅',
      icon: 'analytics',
      minOperands: 1,
      maxOperands: Infinity,
      description: 'Promedio de múltiples valores',
    },
    {
      id: 'cuenta',
      label: 'Cuenta',
      symbol: '∑',
      icon: 'sum',
      minOperands: 1,
      maxOperands: Infinity,
      description: 'Cuenta de elementos',
    },
  ];

  constructor() {
    this.saveToHistory('canvas_initialized');
  }

  /**
   * Observable del estado actual del editor
   */
  getState$(): Observable<EditorState> {
    return this.state$.asObservable();
  }

  /**
   * Observable de eventos del editor
   */
  getEvents$(): Observable<EditorEvent | null> {
    return this.events$.asObservable();
  }

  /**
   * Obtener estado actual (snapshot)
   */
  getCurrentState(): EditorState {
    return this.state$.getValue();
  }

  /**
   * Obtener paleta de items disponibles
   */
  getPaletteItems(): PaletteItem[] {
    const items: PaletteItem[] = [
      {
        id: 'numero-item',
        name: 'Número Fijo',
        type: 'numero',
        icon: 'looks_3',
        category: 'Números',
        description: 'Agrega un número fijo a la fórmula',
        draggable: true,
        template: {
          type: 'numero',
          width: 120,
          height: 80,
          selected: false,
          invalid: false,
          data: { valor: 0 },
        },
      },
    ];

    // Items de variables (por entidad)
    this.entities.forEach((entity) => {
      items.push({
        id: `variable-${entity.id}`,
        name: entity.label,
        type: 'variable',
        icon: 'data_object',
        category: 'Variables',
        description: `Variable de ${entity.label}`,
        draggable: true,
        template: {
          type: 'variable',
          width: 140,
          height: 100,
          selected: false,
          invalid: false,
          data: {
            entidad: entity.id as any,
            campo: entity.fields[0]?.name || '',
            agregacion: 'none',
          },
        },
      });
    });

    // Items de operaciones
    this.operations.forEach((op) => {
      items.push({
        id: `operation-${op.id}`,
        name: op.label,
        type: 'operacion',
        icon: op.icon,
        category: 'Operaciones',
        description: op.description,
        draggable: true,
        template: {
          type: 'operacion',
          width: 140,
          height: 100,
          selected: false,
          invalid: false,
          data: {
            operacion: op.id,
            operandos: [],
          },
        },
      });
    });

    return items;
  }

  /**
   * Obtener metadatos de entidades
   */
  getEntities(): EntityMetadata[] {
    return [...this.entities];
  }

  /**
   * Obtener configuración de operaciones
   */
  getOperations(): OperationConfig[] {
    return [...this.operations];
  }

  /**
   * Crear un nuevo nodo en el canvas
   */
  createNode(type: NodeType, posX: number, posY: number, data?: Partial<NodeData>): FormulaNode {
    const id = this.generateId('node');
    const width = this.getDefaultNodeWidth(type);
    const height = this.getDefaultNodeHeight(type);

    const node: FormulaNode = {
      id,
      type,
      posX,
      posY,
      width,
      height,
      selected: true,
      invalid: false,
      data: data || this.getDefaultNodeData(type),
    };

    const state = this.getCurrentState();
    state.nodes.set(id, node);

    // Deseleccionar otros nodos
    state.nodes.forEach((n) => {
      if (n.id !== id) n.selected = false;
    });

    state.isDirty = true;
    state.selectedNodeId = id;
    this.state$.next(state);

    this.emitEvent('node_created', node);
    this.saveToHistory(`create_node_${type}`);
    this.validate();

    return node;
  }

  /**
   * Actualizar un nodo existente
   */
  updateNode(nodeId: string, updates: Partial<FormulaNode>): void {
    const state = this.getCurrentState();
    const node = state.nodes.get(nodeId);

    if (!node) return;

    const updated = { ...node, ...updates };
    state.nodes.set(nodeId, updated);
    state.isDirty = true;

    this.state$.next(state);
    this.emitEvent('node_updated', updated);
    this.saveToHistory(`update_node_${nodeId}`);
    this.validate();
  }

  /**
   * Eliminar un nodo (y sus conexiones asociadas)
   */
  deleteNode(nodeId: string): void {
    const state = this.getCurrentState();
    state.nodes.delete(nodeId);

    // Eliminar conexiones asociadas
    const connectionsToDelete: string[] = [];
    state.connections.forEach((conn, id) => {
      if (conn.fromNodeId === nodeId || conn.toNodeId === nodeId) {
        connectionsToDelete.push(id);
      }
    });

    connectionsToDelete.forEach((id) => state.connections.delete(id));

    if (state.selectedNodeId === nodeId) {
      state.selectedNodeId = undefined;
    }

    state.isDirty = true;
    this.state$.next(state);

    this.emitEvent('node_deleted', { nodeId });
    this.saveToHistory(`delete_node_${nodeId}`);
    this.validate();
  }

  /**
   * Seleccionar un nodo
   */
  selectNode(nodeId: string | undefined): void {
    const state = this.getCurrentState();

    state.nodes.forEach((node) => {
      node.selected = node.id === nodeId;
    });

    state.selectedNodeId = nodeId;
    this.state$.next(state);

    this.emitEvent('node_selected', { nodeId });
  }

  /**
   * Crear una conexión entre dos nodos
   */
  createConnection(fromNodeId: string, toNodeId: string, toPoint: string): Connection | null {
    const state = this.getCurrentState();
    const fromNode = state.nodes.get(fromNodeId);
    const toNode = state.nodes.get(toNodeId);

    if (!fromNode || !toNode) return null;

    // Validar que toNode pueda recibir conexiones (solo operaciones)
    if (toNode.type !== 'operacion') return null;

    // Validar que no haya ciclos
    if (this.wouldCreateCycle(fromNodeId, toNodeId)) {
      console.warn('Connection would create a cycle');
      return null;
    }

    const id = this.generateId('connection');
    const connection: Connection = {
      id,
      fromNodeId,
      fromPoint: 'output',
      toNodeId,
      toPoint,
      selected: false,
      invalid: false,
    };

    state.connections.set(id, connection);

    // Actualizar operandos del nodo destino
    if (toNode.data.operandos) {
      toNode.data.operandos.push(fromNodeId);
    }

    state.isDirty = true;
    this.state$.next(state);

    this.emitEvent('connection_created', connection);
    this.saveToHistory(`create_connection_${fromNodeId}_to_${toNodeId}`);
    this.validate();

    return connection;
  }

  /**
   * Eliminar una conexión
   */
  deleteConnection(connectionId: string): void {
    const state = this.getCurrentState();
    const connection = state.connections.get(connectionId);

    if (!connection) return;

    state.connections.delete(connectionId);

    // Actualizar operandos del nodo destino
    const toNode = state.nodes.get(connection.toNodeId);
    if (toNode?.data.operandos) {
      toNode.data.operandos = toNode.data.operandos.filter((id) => id !== connection.fromNodeId);
    }

    if (state.selectedConnectionId === connectionId) {
      state.selectedConnectionId = undefined;
    }

    state.isDirty = true;
    this.state$.next(state);

    this.emitEvent('connection_deleted', { connectionId });
    this.saveToHistory(`delete_connection_${connectionId}`);
    this.validate();
  }

  /**
   * Limpiar todo el canvas
   */
  clear(): void {
    const state: EditorState = {
      nodes: new Map(),
      connections: new Map(),
      isDirty: false,
      validationErrors: [],
    };

    this.state$.next(state);
    this.emitEvent('formula_changed', null);
    this.saveToHistory('canvas_cleared');
  }

  /**
   * Validar la estructura actual de la fórmula
   */
  validate(): ValidationError[] {
    const state = this.getCurrentState();
    const errors: ValidationError[] = [];

    // Validar nodos
    state.nodes.forEach((node) => {
      const nodeErrors = this.validateNode(node, state);
      errors.push(...nodeErrors);
      node.invalid = nodeErrors.length > 0;
      if (nodeErrors.length > 0) {
        node.invalidReason = nodeErrors[0]?.message;
      }
    });

    // Validar conexiones
    state.connections.forEach((conn) => {
      const connErrors = this.validateConnection(conn, state);
      errors.push(...connErrors);
      conn.invalid = connErrors.length > 0;
      if (connErrors.length > 0) {
        conn.invalidReason = connErrors[0]?.message;
      }
    });

    state.validationErrors = errors;
    this.state$.next(state);

    if (errors.length === 0) {
      this.emitEvent('validation_changed', { isValid: true });
    } else {
      this.emitEvent('validation_changed', { isValid: false, errors });
    }

    return errors;
  }

  /**
   * Exportar fórmula actual a JSON
   */
  exportToFormula(): ExportResult {
    const state = this.getCurrentState();

    // Buscar nodo raíz (nodo sin entrada)
    let rootNode: FormulaNode | undefined;
    state.nodes.forEach((node) => {
      let hasInput = false;
      state.connections.forEach((conn) => {
        if (conn.toNodeId === node.id) hasInput = true;
      });
      if (!hasInput && !rootNode) {
        rootNode = node;
      }
    });

    if (!rootNode) {
      return {
        formula: { tipo: 'numero', valor: 0 },
        isValid: false,
        errors: [
          {
            severity: 'error',
            message: 'No se encontró un nodo raíz para exportar',
          },
        ],
      };
    }

    try {
      const formula = this.nodeToFormula(rootNode, state);
      const errors = state.validationErrors;

      return {
        formula,
        isValid: errors.length === 0,
        errors,
      };
    } catch (error) {
      return {
        formula: { tipo: 'numero', valor: 0 },
        isValid: false,
        errors: [
          {
            severity: 'error',
            message: `Error al exportar: ${error}`,
          },
        ],
      };
    }
  }

  /**
   * Importar fórmula desde JSON
   */
  importFromFormula(formula: Formula): void {
    this.clear();
    this.formulaToNodes(formula, 100, 100);
    this.validate();
    this.emitEvent('formula_changed', formula);
    this.saveToHistory('formula_imported');
  }

  /**
   * Exportar estado actual como JSON serializable
   */
  exportState(): string {
    const state = this.getCurrentState();
    const data = {
      nodes: Array.from(state.nodes.values()),
      connections: Array.from(state.connections.values()),
    };
    return JSON.stringify(data, null, 2);
  }

  /**
   * Importar estado desde JSON
   */
  importState(json: string): boolean {
    try {
      const data = JSON.parse(json);
      const state: EditorState = {
        nodes: new Map(),
        connections: new Map(),
        isDirty: true,
        validationErrors: [],
      };

      // Recrear nodos
      if (Array.isArray(data.nodes)) {
        data.nodes.forEach((node: any) => {
          state.nodes.set(node.id, node);
        });
      }

      // Recrear conexiones
      if (Array.isArray(data.connections)) {
        data.connections.forEach((conn: any) => {
          state.connections.set(conn.id, conn);
        });
      }

      this.state$.next(state);
      this.validate();
      this.saveToHistory('state_imported');
      return true;
    } catch (error) {
      console.error('Error importing state:', error);
      return false;
    }
  }

  /**
   * Undo
   */
  undo(): void {
    if (this.historyIndex > 0) {
      this.historyIndex--;
      const entry = this.history[this.historyIndex];
      this.state$.next(entry.state);
    }
  }

  /**
   * Redo
   */
  redo(): void {
    if (this.historyIndex < this.history.length - 1) {
      this.historyIndex++;
      const entry = this.history[this.historyIndex];
      this.state$.next(entry.state);
    }
  }

  canUndo(): boolean {
    return this.historyIndex > 0;
  }

  canRedo(): boolean {
    return this.historyIndex < this.history.length - 1;
  }

  // ===== PRIVATE METHODS =====

  private getDefaultNodeWidth(type: NodeType): number {
    switch (type) {
      case 'numero':
        return 120;
      case 'variable':
        return 140;
      case 'operacion':
        return 140;
      default:
        return 120;
    }
  }

  private getDefaultNodeHeight(type: NodeType): number {
    switch (type) {
      case 'numero':
        return 80;
      case 'variable':
        return 100;
      case 'operacion':
        return 100;
      default:
        return 80;
    }
  }

  private getDefaultNodeData(type: NodeType): NodeData {
    switch (type) {
      case 'numero':
        return { valor: 0 };
      case 'variable':
        return {
          entidad: 'celero_visita',
          campo: 'duracion_minutos',
          agregacion: 'none',
        };
      case 'operacion':
        return { operacion: 'suma', operandos: [] };
      default:
        return {};
    }
  }

  private validateNode(node: FormulaNode, state: EditorState): ValidationError[] {
    const errors: ValidationError[] = [];

    if (node.type === 'numero') {
      if (typeof node.data.valor !== 'number' || isNaN(node.data.valor)) {
        errors.push({
          nodeId: node.id,
          severity: 'error',
          message: 'Valor numérico inválido',
        });
      }
    } else if (node.type === 'variable') {
      if (!node.data.entidad) {
        errors.push({
          nodeId: node.id,
          severity: 'error',
          message: 'Entidad no seleccionada',
        });
      }
      if (!node.data.campo) {
        errors.push({
          nodeId: node.id,
          severity: 'error',
          message: 'Campo no seleccionado',
        });
      }
    } else if (node.type === 'operacion') {
      const op = this.operations.find((o) => o.id === node.data.operacion);
      if (!op) {
        errors.push({
          nodeId: node.id,
          severity: 'error',
          message: 'Operación inválida',
        });
      } else {
        const operandCount = node.data.operandos?.length || 0;
        if (operandCount < op.minOperands) {
          errors.push({
            nodeId: node.id,
            severity: 'error',
            message: `Mínimo ${op.minOperands} operandos requeridos`,
          });
        }
        if (operandCount > op.maxOperands) {
          errors.push({
            nodeId: node.id,
            severity: 'error',
            message: `Máximo ${op.maxOperands} operandos permitidos`,
          });
        }
      }
    }

    return errors;
  }

  private validateConnection(conn: Connection, state: EditorState): ValidationError[] {
    const errors: ValidationError[] = [];

    const fromNode = state.nodes.get(conn.fromNodeId);
    const toNode = state.nodes.get(conn.toNodeId);

    if (!fromNode) {
      errors.push({
        connectionId: conn.id,
        severity: 'error',
        message: 'Nodo origen no existe',
      });
    }

    if (!toNode) {
      errors.push({
        connectionId: conn.id,
        severity: 'error',
        message: 'Nodo destino no existe',
      });
    }

    if (toNode && toNode.type !== 'operacion') {
      errors.push({
        connectionId: conn.id,
        severity: 'error',
        message: 'Solo se pueden conectar a operaciones',
      });
    }

    return errors;
  }

  private wouldCreateCycle(fromNodeId: string, toNodeId: string): boolean {
    const state = this.getCurrentState();

    // DFS para detectar ciclos
    const visited = new Set<string>();
    const stack = [toNodeId];

    while (stack.length > 0) {
      const current = stack.pop()!;
      if (current === fromNodeId) return true;

      if (visited.has(current)) continue;
      visited.add(current);

      state.connections.forEach((conn) => {
        if (conn.fromNodeId === current && !visited.has(conn.toNodeId)) {
          stack.push(conn.toNodeId);
        }
      });
    }

    return false;
  }

  private nodeToFormula(node: FormulaNode, state: EditorState): Formula {
    if (node.type === 'numero') {
      return {
        tipo: 'numero',
        valor: node.data.valor || 0,
      };
    } else if (node.type === 'variable') {
      const formula: Formula = {
        tipo: 'variable',
        entidad: node.data.entidad,
        campo: node.data.campo,
      };

      if (node.data.agregacion && node.data.agregacion !== 'none') {
        formula.agregacion = node.data.agregacion;
      }

      return formula;
    } else if (node.type === 'operacion') {
      const operandos = node.data.operandos || [];
      const operandFormulas: Formula[] = [];

      operandos.forEach((operandId) => {
        const operandNode = state.nodes.get(operandId);
        if (operandNode) {
          operandFormulas.push(this.nodeToFormula(operandNode, state));
        }
      });

      return {
        tipo: 'operacion',
        operacion: node.data.operacion,
        operandos: operandFormulas,
      };
    }

    return { tipo: 'numero', valor: 0 };
  }

  private formulaToNodes(formula: Formula, startX: number, startY: number, offsetY: number = 0): string {
    const nodeId = this.generateId('node');
    let x = startX;
    let y = startY + offsetY;

    if (formula.tipo === 'numero') {
      this.createNode('numero', x, y, { valor: formula.valor });
    } else if (formula.tipo === 'variable') {
      this.createNode('variable', x, y, {
        entidad: formula.entidad as any,
        campo: formula.campo,
        agregacion: formula.agregacion as any,
      });
    } else if (formula.tipo === 'operacion') {
      const operandoNodes = formula.operandos || [];
      const opNode = this.createNode('operacion', x, y, {
        operacion: formula.operacion,
        operandos: [],
      });

      operandoNodes.forEach((operando, index) => {
        const operandoNodeId = this.formulaToNodes(operando, x - 200, y + 150 + index * 120, 0);
        // Crear conexión (simplificado, real debería usar IDs correctos)
      });
    }

    return nodeId;
  }

  private generateId(prefix: string): string {
    return `${prefix}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
  }

  private emitEvent(type: any, payload: any): void {
    this.events$.next({
      type,
      payload,
      timestamp: Date.now(),
    });
  }

  private saveToHistory(description: string): void {
    // Descartar forward history si no estamos en el último estado
    if (this.historyIndex < this.history.length - 1) {
      this.history = this.history.slice(0, this.historyIndex + 1);
    }

    // Crear copia del estado actual
    const stateSnapshot: EditorState = {
      nodes: new Map(this.state$.getValue().nodes),
      connections: new Map(this.state$.getValue().connections),
      isDirty: this.state$.getValue().isDirty,
      validationErrors: [...this.state$.getValue().validationErrors],
    };

    this.history.push({
      timestamp: Date.now(),
      action: 'add_node',
      state: stateSnapshot,
      description,
    });

    this.historyIndex = this.history.length - 1;
  }
}
