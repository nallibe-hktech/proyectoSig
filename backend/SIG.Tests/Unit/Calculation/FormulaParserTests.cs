using SIG.Application.Calculation;
using SIG.Application.Calculation.Nodes;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Calculation;

public class FormulaParserTests
{
    private readonly FormulaParser _sut = new();

    [Fact]
    public void Parse_NumberNode_DevuelveNumberNode()
    {
        var json = """{"type":"Number","value":42.5}""";
        var node = _sut.Parse(json);
        node.Should().BeOfType<NumberNode>();
        ((NumberNode)node).Value.Should().Be(42.5);
    }

    [Fact]
    public void Parse_VariableNode_DevuelveVariableNode()
    {
        var json = """{"type":"Variable","variableId":7}""";
        var node = _sut.Parse(json);
        node.Should().BeOfType<VariableNode>();
        ((VariableNode)node).VariableId.Should().Be(7);
    }

    [Fact]
    public void Parse_AggregateConSource_DevuelveAggregateNode()
    {
        var json = """
        {
          "type":"Aggregate","op":"Sum","field":"Importe",
          "source":{"type":"Source","entity":"GastosPayHawk","filters":[]}
        }
        """;
        var node = _sut.Parse(json);
        node.Should().BeOfType<AggregateNode>();
        var agg = (AggregateNode)node;
        agg.Op.Should().Be("Sum");
        agg.Field.Should().Be("Importe");
        agg.Source.Entity.Should().Be("GastosPayHawk");
    }

    [Fact]
    public void Parse_BinaryOp_DevuelveBinaryOpNode()
    {
        var json = """
        {
          "type":"BinaryOp","op":"Mul",
          "left":{"type":"Number","value":3},
          "right":{"type":"Number","value":4}
        }
        """;
        var node = _sut.Parse(json);
        node.Should().BeOfType<BinaryOpNode>();
        var b = (BinaryOpNode)node;
        b.Op.Should().Be("Mul");
        b.Left.Should().BeOfType<NumberNode>();
        b.Right.Should().BeOfType<NumberNode>();
    }

    [Fact]
    public void Parse_JsonVacio_LanzaFormulaInvalidException()
    {
        var act = () => _sut.Parse("");
        act.Should().Throw<FormulaInvalidException>().WithMessage("*vacía*");
    }

    [Fact]
    public void Parse_JsonMalformado_LanzaFormulaInvalidException()
    {
        var act = () => _sut.Parse("{not valid json");
        act.Should().Throw<FormulaInvalidException>().WithMessage("*JSON inválido*");
    }

    [Fact]
    public void Parse_SourceSinEntity_LanzaFormulaInvalidException()
    {
        var json = """{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"","filters":[]}}""";
        var act = () => _sut.Parse(json);
        act.Should().Throw<FormulaInvalidException>().WithMessage("*Source*entity*vacío*");
    }

    [Fact]
    public void Parse_BinaryOpSinOperador_LanzaFormulaInvalidException()
    {
        var json = """{"type":"BinaryOp","op":"","left":{"type":"Number","value":1},"right":{"type":"Number","value":2}}""";
        var act = () => _sut.Parse(json);
        act.Should().Throw<FormulaInvalidException>().WithMessage("*BinaryOp*op*vacío*");
    }

    [Fact]
    public void Parse_AggregateSinSource_LanzaFormulaInvalidException()
    {
        // op presente pero source null
        var json = """{"type":"Aggregate","op":"Count","source":null}""";
        var act = () => _sut.Parse(json);
        act.Should().Throw<FormulaInvalidException>();
    }

    [Fact]
    public void TryValidate_FormulaValida_DevuelveTrueSinErrores()
    {
        var json = """{"type":"Number","value":1}""";
        var ok = _sut.TryValidate(json, out var errores);
        ok.Should().BeTrue();
        errores.Should().BeEmpty();
    }

    [Fact]
    public void TryValidate_FormulaInvalida_DevuelveFalseConErrores()
    {
        var ok = _sut.TryValidate("", out var errores);
        ok.Should().BeFalse();
        errores.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_FormulaComplejaSemilla_SumaGastosPagada_ParseCorrectamente()
    {
        // Ejemplo de ARQUITECTURA.md §6.3: Suma(GastosPayHawk.importe)
        var json = """
        {
          "type":"Aggregate","op":"Sum","field":"Importe",
          "source":{"type":"Source","entity":"GastosPayHawk","filters":[]}
        }
        """;
        var node = _sut.Parse(json);
        node.Should().BeOfType<AggregateNode>();
    }

    [Fact]
    public void Parse_BonusVisitaEstandar_ParseCorrectamente()
    {
        // Ejemplo de ARQUITECTURA.md §6.3: Cuenta(VisitasCelero TipoVisita=1) × 5
        var json = """
        {
          "type":"BinaryOp","op":"Mul",
          "left":{
            "type":"Aggregate","op":"Count",
            "source":{
              "type":"Source","entity":"VisitasCelero",
              "filters":[{"field":"TipoVisita","op":"Eq","value":1}]
            }
          },
          "right":{"type":"Number","value":5}
        }
        """;
        var node = _sut.Parse(json);
        var b = node.Should().BeOfType<BinaryOpNode>().Subject;
        b.Op.Should().Be("Mul");
        var agg = b.Left.Should().BeOfType<AggregateNode>().Subject;
        agg.Op.Should().Be("Count");
        agg.Source.Filters.Should().HaveCount(1);
        agg.Source.Filters[0].Field.Should().Be("TipoVisita");
        agg.Source.Filters[0].Op.Should().Be("Eq");
    }

    [Fact]
    public void Parse_RefacturacionGastosPct_ParseCorrectamente()
    {
        // Ejemplo de ARQUITECTURA.md §6.3: Suma(GastosPayHawk.importe) × 1.15 (vía Pct con 15)
        var json = """
        {
          "type":"BinaryOp","op":"Pct",
          "left":{"type":"Aggregate","op":"Sum","field":"Importe","source":{"type":"Source","entity":"GastosPayHawk","filters":[]}},
          "right":{"type":"Number","value":15}
        }
        """;
        var node = _sut.Parse(json);
        var b = node.Should().BeOfType<BinaryOpNode>().Subject;
        b.Op.Should().Be("Pct");
    }
}
