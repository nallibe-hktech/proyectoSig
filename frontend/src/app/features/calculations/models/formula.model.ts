/**
 * Modelos de datos para el Editor Visual de Fórmulas
 * Basados en la especificación de fórmulas JSON del motor de cálculo
 */

export type NodeType = 'numero' | 'variable' | 'operacion';
export type OperationType = 'suma' | 'resta' | 'multiplica' | 'divide' | 'modulo' | 'promedio' | 'cuenta' | '+' | '-' | '*' | '/' | '%';
export type EntityType = 'celero_visita' | 'payhawk_gasto' | 'bizneo_hora' | 'intratime_fichaje' | 'sgpv_producto';
export type AggregationType = 'none' | 'sum' | 'avg' | 'count';

/**
 * Representa un nodo en el editor visual
 * Puede ser: Número, Variable u Operación
 */
export interface FormulaNode {
  id: string;
  type: NodeType;
  posX: number;
  posY: number;
  width: number;
  height: number;
  selected: boolean;
  invalid: boolean;
  invalidReason?: string;
  data: NodeData;
}

/**
 * Datos específicos según el tipo de nodo
 */
export interface NodeData {
  // Para tipo NÚMERO
  valor?: number;

  // Para tipo VARIABLE
  entidad?: EntityType;
  campo?: string;
  agregacion?: AggregationType;

  // Para tipo OPERACIÓN
  operacion?: OperationType;
  operandos?: string[]; // IDs de nodos conectados
}

/**
 * Representa una conexión física entre dos nodos
 * Vincula un punto de salida a un punto de entrada
 */
export interface Connection {
  id: string;
  fromNodeId: string;
  fromPoint: 'output';
  toNodeId: string;
  toPoint: string; // 'input_0', 'input_1', etc
  selected: boolean;
  invalid: boolean;
  invalidReason?: string;
}

/**
 * Punto de conexión en un nodo
 * Puede ser entrada (input) o salida (output)
 */
export interface ConnectionPoint {
  nodeId: string;
  pointId: string; // 'output' | 'input_0' | 'input_1'
  type: 'input' | 'output';
  x: number; // Coordenada absoluta en el canvas
  y: number;
}

/**
 * Estado del editor (canvas, nodos, conexiones)
 */
export interface EditorState {
  nodes: Map<string, FormulaNode>;
  connections: Map<string, Connection>;
  selectedNodeId?: string;
  selectedConnectionId?: string;
  isDirty: boolean;
  validationErrors: ValidationError[];
}

/**
 * Error de validación
 */
export interface ValidationError {
  nodeId?: string;
  connectionId?: string;
  severity: 'error' | 'warning';
  message: string;
}

/**
 * Item de la paleta de componentes
 */
export interface PaletteItem {
  id: string;
  name: string;
  type: NodeType;
  icon: string;
  category: 'Números' | 'Variables' | 'Operaciones';
  description: string;
  draggable: true;
  template: Partial<FormulaNode>;
}

/**
 * Metadatos de entidades disponibles (para variables)
 */
export interface EntityMetadata {
  id: EntityType;
  label: string;
  icon: string;
  fields: EntityField[];
}

export interface EntityField {
  name: string;
  label: string;
  type: 'numero' | 'texto' | 'booleano' | 'fecha';
  aggregations?: AggregationType[];
}

/**
 * Configuración de operación (restricciones, validaciones)
 */
export interface OperationConfig {
  id: OperationType;
  label: string;
  symbol: string;
  icon: string;
  minOperands: number;
  maxOperands: number;
  description: string;
}

/**
 * Historial para Undo/Redo
 */
export interface HistoryEntry {
  timestamp: number;
  action: 'add_node' | 'remove_node' | 'update_node' | 'add_connection' | 'remove_connection' | 'clear';
  state: EditorState;
  description: string;
}

/**
 * Resultado de exportación/importación
 */
export interface ExportResult {
  formula: Formula;
  isValid: boolean;
  errors: ValidationError[];
}

/**
 * Fórmula en formato JSON (salida del editor)
 * Estructura que el backend entiende
 */
export interface Formula {
  tipo: 'numero' | 'variable' | 'operacion';
  valor?: number;
  entidad?: EntityType;
  campo?: string;
  agregacion?: AggregationType;
  operacion?: OperationType;
  operandos?: Formula[];
}

/**
 * Eventos del editor para comunicación entre componentes
 */
export interface EditorEvent {
  type: 'node_selected' | 'node_created' | 'node_deleted' | 'node_updated' | 'connection_created' | 'connection_deleted' | 'formula_changed' | 'validation_changed';
  payload: any;
  timestamp: number;
}
