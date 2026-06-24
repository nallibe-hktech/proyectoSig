using System.Text.Json;
using System.Text.Json.Serialization;
using SIG.Application.Calculation.Nodes;

namespace SIG.Application.Calculation;

/// <summary>
/// Custom JSON converter for FormulaNode that supports both:
/// 1. New format with "type" discriminator (e.g., {"type": "Number", "value": 100})
/// 2. Legacy format without discriminator, where type is inferred from properties
/// Avoids recursive JsonSerializer calls to prevent "Deserialization of abstract types" errors.
/// </summary>
public class FormulaNodeJsonConverter : JsonConverter<FormulaNode>
{
    public override FormulaNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        // Try new format with explicit "type" discriminator
        if (root.TryGetProperty("type", out var typeProperty))
        {
            var typeValue = typeProperty.GetString();
            return typeValue switch
            {
                "Number" => DeserializeNumberNode(root),
                "Variable" => DeserializeVariableNode(root),
                "Source" => DeserializeSourceNode(root),
                "Aggregate" => DeserializeAggregateNode(root),
                "BinaryOp" => DeserializeBinaryOpNode(root),
                "Modifier" => DeserializeModifierNode(root),
                "Tramos" => DeserializeTramosNode(root),
                "ConceptRef" => DeserializeConceptRefNode(root),
                _ => throw new JsonException($"Unknown FormulaNode type: {typeValue}")
            };
        }

        // Legacy format: infer type from properties
        return InferAndDeserializeNode(root);
    }

    public override void Write(Utf8JsonWriter writer, FormulaNode value, JsonSerializerOptions options)
    {
        // Write with type discriminator
        writer.WriteStartObject();
        writer.WriteString("type", value.GetType().Name switch
        {
            "NumberNode" => "Number",
            "VariableNode" => "Variable",
            "SourceNode" => "Source",
            "AggregateNode" => "Aggregate",
            "BinaryOpNode" => "BinaryOp",
            "ModifierNode" => "Modifier",
            "TramosNode" => "Tramos",
            "ConceptRefNode" => "ConceptRef",
            _ => throw new JsonException($"Unknown FormulaNode type: {value.GetType().Name}")
        });

        if (value is NumberNode number)
        {
            writer.WriteNumber("value", number.Value);
        }
        else if (value is VariableNode variable)
        {
            writer.WriteNumber("variableId", variable.VariableId);
        }
        else if (value is SourceNode source)
        {
            writer.WriteString("entity", source.Entity);
            if (!string.IsNullOrEmpty(source.Field))
                writer.WriteString("field", source.Field);
            writer.WritePropertyName("filters");
            JsonSerializer.Serialize(writer, source.Filters, options);
        }
        else if (value is AggregateNode aggregate)
        {
            writer.WriteString("op", aggregate.Op);
            writer.WritePropertyName("source");
            JsonSerializer.Serialize(writer, aggregate.Source, typeof(FormulaNode), options);
            if (!string.IsNullOrEmpty(aggregate.Field))
                writer.WriteString("field", aggregate.Field);
        }
        else if (value is BinaryOpNode binary)
        {
            writer.WriteString("op", binary.Op);
            writer.WritePropertyName("left");
            JsonSerializer.Serialize(writer, binary.Left, typeof(FormulaNode), options);
            writer.WritePropertyName("right");
            JsonSerializer.Serialize(writer, binary.Right, typeof(FormulaNode), options);
        }
        else if (value is ModifierNode modifier)
        {
            writer.WriteString("kind", modifier.Kind);
            writer.WriteNumber("threshold", modifier.Threshold);
            writer.WritePropertyName("inner");
            JsonSerializer.Serialize(writer, modifier.Inner, typeof(FormulaNode), options);
        }
        else if (value is TramosNode tramos)
        {
            writer.WritePropertyName("cantidad");
            JsonSerializer.Serialize(writer, tramos.Cantidad, typeof(FormulaNode), options);
            writer.WritePropertyName("tramos");
            writer.WriteStartArray();
            foreach (var t in tramos.Tramos)
            {
                writer.WriteStartObject();
                if (t.Hasta.HasValue) writer.WriteNumber("hasta", t.Hasta.Value);
                else writer.WriteNull("hasta");
                writer.WriteNumber("precio", t.Precio);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        else if (value is ConceptRefNode conceptRef)
        {
            writer.WritePropertyName("conceptIds");
            JsonSerializer.Serialize(writer, conceptRef.ConceptIds, options);
        }

        writer.WriteEndObject();
    }

    private static NumberNode DeserializeNumberNode(JsonElement root)
    {
        if (!root.TryGetProperty("value", out var valueProperty))
            throw new JsonException("NumberNode requires 'value' property");
        return new NumberNode { Value = valueProperty.GetDouble() };
    }

    private static VariableNode DeserializeVariableNode(JsonElement root)
    {
        if (!root.TryGetProperty("variableId", out var variableIdProperty))
            throw new JsonException("VariableNode requires 'variableId' property");
        return new VariableNode { VariableId = variableIdProperty.GetInt32() };
    }

    private static SourceNode DeserializeSourceNode(JsonElement root)
    {
        if (!root.TryGetProperty("entity", out var entityProperty))
            throw new JsonException("SourceNode requires 'entity' property");

        var sourceNode = new SourceNode { Entity = entityProperty.GetString() ?? "" };

        if (root.TryGetProperty("field", out var fieldProperty) && fieldProperty.ValueKind != JsonValueKind.Null)
            sourceNode.Field = fieldProperty.GetString();

        sourceNode.Filters = new List<FilterSpec>();
        if (root.TryGetProperty("filters", out var filtersProperty) && filtersProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var filterElement in filtersProperty.EnumerateArray())
            {
                var filter = new FilterSpec
                {
                    Field = filterElement.TryGetProperty("field", out var f) ? f.GetString() ?? "" : "",
                    Op = filterElement.TryGetProperty("op", out var o) ? o.GetString() ?? "" : "",
                    Value = filterElement.TryGetProperty("value", out var v) ? ReadFilterValue(v) : null
                };
                sourceNode.Filters.Add(filter);
            }
        }

        return sourceNode;
    }

    // Valor de un filtro: soporta string, número, booleano (flags de excepción) y array (operador In).
    private static object? ReadFilterValue(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String => v.GetString(),
        JsonValueKind.Number => v.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => v.EnumerateArray().Select(ReadFilterValue).ToList(),
        _ => null
    };

    private static AggregateNode DeserializeAggregateNode(JsonElement root)
    {
        if (!root.TryGetProperty("op", out var opProperty))
            throw new JsonException("AggregateNode requires 'op' property");
        if (!root.TryGetProperty("source", out var sourceProperty))
            throw new JsonException("AggregateNode requires 'source' property");

        var aggregateNode = new AggregateNode
        {
            Op = opProperty.GetString() ?? "",
            Source = (SourceNode?)InferAndDeserializeNode(sourceProperty)
                     ?? throw new JsonException("Aggregate source must be a SourceNode")
        };

        if (root.TryGetProperty("field", out var aggregateFieldProperty) && aggregateFieldProperty.ValueKind != JsonValueKind.Null)
            aggregateNode.Field = aggregateFieldProperty.GetString();

        if (root.TryGetProperty("distinct", out var distinctProperty) && distinctProperty.ValueKind != JsonValueKind.Null)
            aggregateNode.Distinct = distinctProperty.GetString();

        return aggregateNode;
    }

    private static ModifierNode DeserializeModifierNode(JsonElement root)
    {
        if (!root.TryGetProperty("kind", out var kindProperty))
            throw new JsonException("ModifierNode requires 'kind' property");
        if (!root.TryGetProperty("inner", out var innerProperty))
            throw new JsonException("ModifierNode requires 'inner' property");
        return new ModifierNode
        {
            Kind = kindProperty.GetString() ?? "",
            Threshold = root.TryGetProperty("threshold", out var th) && th.ValueKind == JsonValueKind.Number ? th.GetDecimal() : 0m,
            Inner = InferAndDeserializeNode(innerProperty)
        };
    }

    private static TramosNode DeserializeTramosNode(JsonElement root)
    {
        if (!root.TryGetProperty("cantidad", out var cantidadProperty))
            throw new JsonException("TramosNode requires 'cantidad' property");
        var node = new TramosNode { Cantidad = InferAndDeserializeNode(cantidadProperty) };
        if (root.TryGetProperty("tramos", out var tramosProperty) && tramosProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in tramosProperty.EnumerateArray())
            {
                node.Tramos.Add(new Tramo
                {
                    Hasta = t.TryGetProperty("hasta", out var h) && h.ValueKind == JsonValueKind.Number ? h.GetDecimal() : (decimal?)null,
                    Precio = t.TryGetProperty("precio", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : 0m
                });
            }
        }
        return node;
    }

    private static ConceptRefNode DeserializeConceptRefNode(JsonElement root)
    {
        var node = new ConceptRefNode();
        if (root.TryGetProperty("conceptIds", out var idsProperty) && idsProperty.ValueKind == JsonValueKind.Array)
        {
            foreach (var id in idsProperty.EnumerateArray())
                if (id.ValueKind == JsonValueKind.Number) node.ConceptIds.Add(id.GetInt32());
        }
        return node;
    }

    private static BinaryOpNode DeserializeBinaryOpNode(JsonElement root)
    {
        if (!root.TryGetProperty("op", out var opProperty))
            throw new JsonException("BinaryOpNode requires 'op' property");
        if (!root.TryGetProperty("left", out var leftProperty))
            throw new JsonException("BinaryOpNode requires 'left' property");
        if (!root.TryGetProperty("right", out var rightProperty))
            throw new JsonException("BinaryOpNode requires 'right' property");

        return new BinaryOpNode
        {
            Op = opProperty.GetString() ?? "",
            Left = InferAndDeserializeNode(leftProperty),
            Right = InferAndDeserializeNode(rightProperty)
        };
    }

    private static FormulaNode InferAndDeserializeNode(JsonElement root)
    {
        // Prefer the explicit "type" discriminator when present (handles nested nodes of any type).
        if (root.TryGetProperty("type", out var typeProperty) && typeProperty.ValueKind == JsonValueKind.String)
        {
            return typeProperty.GetString() switch
            {
                "Number" => DeserializeNumberNode(root),
                "Variable" => DeserializeVariableNode(root),
                "Source" => DeserializeSourceNode(root),
                "Aggregate" => DeserializeAggregateNode(root),
                "BinaryOp" => DeserializeBinaryOpNode(root),
                "Modifier" => DeserializeModifierNode(root),
                "Tramos" => DeserializeTramosNode(root),
                "ConceptRef" => DeserializeConceptRefNode(root),
                _ => throw new JsonException($"Unknown FormulaNode type: {typeProperty.GetString()}")
            };
        }

        // Check for NumberNode: has "value" property (and not other properties)
        if (root.TryGetProperty("value", out var valueProperty) &&
            !root.TryGetProperty("entity", out _) &&
            !root.TryGetProperty("variableId", out _) &&
            !root.TryGetProperty("op", out _))
        {
            return new NumberNode { Value = valueProperty.GetDouble() };
        }

        // Check for VariableNode: has "variableId" property
        if (root.TryGetProperty("variableId", out var variableIdProperty))
        {
            return new VariableNode { VariableId = variableIdProperty.GetInt32() };
        }

        // Check for SourceNode: has "entity" property
        if (root.TryGetProperty("entity", out var entityProperty))
        {
            return DeserializeSourceNode(root);
        }

        // Check for AggregateNode: has "op" and "source" properties
        if (root.TryGetProperty("op", out var opProperty) && root.TryGetProperty("source", out _))
        {
            return DeserializeAggregateNode(root);
        }

        // Check for BinaryOpNode: has "op", "left", "right" properties
        if (root.TryGetProperty("op", out _) && root.TryGetProperty("left", out _) && root.TryGetProperty("right", out _))
        {
            return DeserializeBinaryOpNode(root);
        }

        throw new JsonException("Cannot infer FormulaNode type from JSON properties");
    }
}
