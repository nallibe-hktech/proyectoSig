using SIG.Application.Calculation.Nodes;

namespace SIG.Application.Calculation;

public interface IFormulaParser
{
    FormulaNode Parse(string formulaJson);
    bool TryValidate(string formulaJson, out string[] errores);
}
