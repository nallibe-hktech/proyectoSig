using Microsoft.EntityFrameworkCore;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public class PaymentModelService : IPaymentModelService
{
    private readonly AppDbContext _db;
    public PaymentModelService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<PaymentModelDto>> ListByClientAsync(int clientId, CancellationToken ct)
    {
        return await _db.PaymentModels
            .AsNoTracking()
            .Include(p => p.Client)
            .Where(p => p.ClientId == clientId && !p.IsDeleted)
            .OrderByDescending(p => p.EffectiveFrom)
            .Select(p => new PaymentModelDto(
                p.Id, p.ClientId, p.Client.Nombre, p.ModelType,
                p.EffectiveFrom, p.EffectiveUntil))
            .ToListAsync(ct);
    }

    public async Task<PaymentModelDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var p = await _db.PaymentModels
            .AsNoTracking()
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Modelo de pago no encontrado");
        return new PaymentModelDto(
            p.Id, p.ClientId, p.Client.Nombre, p.ModelType,
            p.EffectiveFrom, p.EffectiveUntil);
    }

    public async Task<PaymentModelDto> CreateAsync(PaymentModelCreateRequest req, int usuarioId, CancellationToken ct)
    {
        var entity = new PaymentModel
        {
            ClientId = req.ClientId,
            ModelType = req.ModelType,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveUntil = req.EffectiveUntil,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.PaymentModels.Add(entity);
        await _db.SaveChangesAsync(ct);

        var client = await _db.Clients.FindAsync(entity.ClientId);
        return new PaymentModelDto(
            entity.Id, entity.ClientId, client?.Nombre ?? "", entity.ModelType,
            entity.EffectiveFrom, entity.EffectiveUntil);
    }

    public async Task<PaymentModelDto> UpdateAsync(int id, PaymentModelUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        var entity = await _db.PaymentModels
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Modelo de pago no encontrado");
        entity.ModelType = req.ModelType;
        entity.EffectiveFrom = req.EffectiveFrom;
        entity.EffectiveUntil = req.EffectiveUntil;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var client = await _db.Clients.FindAsync(entity.ClientId);
        return new PaymentModelDto(
            entity.Id, entity.ClientId, client?.Nombre ?? "", entity.ModelType,
            entity.EffectiveFrom, entity.EffectiveUntil);
    }

    public async Task DeleteAsync(int id, int usuarioId, CancellationToken ct)
    {
        var entity = await _db.PaymentModels
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Modelo de pago no encontrado");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ConceptValidationRuleDto>> GetValidationRulesAsync(int paymentModelId, CancellationToken ct)
    {
        var model = await _db.PaymentModels
            .AsNoTracking()
            .Include(m => m.Client)
            .FirstOrDefaultAsync(m => m.Id == paymentModelId && !m.IsDeleted, ct)
            ?? throw new InvalidOperationException("Modelo de pago no encontrado");

        return await _db.ConceptValidationRules
            .AsNoTracking()
            .Include(r => r.Concept)
            .Where(r => r.PaymentModelType == model.ModelType && !r.IsDeleted)
            .Select(r => new ConceptValidationRuleDto(
                r.Id, r.ConceptId, r.Concept.Nombre, r.PaymentModelType,
                r.IsApplicable, r.IsMandatory, r.CalculationMethod, r.AggregationLevel))
            .ToListAsync(ct);
    }

    public async Task<ConceptValidationRuleDto> UpsertValidationRuleAsync(ConceptValidationRuleUpsertRequest req, int usuarioId, CancellationToken ct)
    {
        var existing = await _db.ConceptValidationRules
            .FirstOrDefaultAsync(r => r.ConceptId == req.ConceptId && r.PaymentModelType == req.PaymentModelType && !r.IsDeleted, ct);

        if (existing != null)
        {
            existing.IsApplicable = req.IsApplicable;
            existing.IsMandatory = req.IsMandatory;
            existing.CalculationMethod = req.CalculationMethod;
            existing.AggregationLevel = req.AggregationLevel;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new ConceptValidationRule
            {
                ConceptId = req.ConceptId,
                PaymentModelType = req.PaymentModelType,
                IsApplicable = req.IsApplicable,
                IsMandatory = req.IsMandatory,
                CalculationMethod = req.CalculationMethod,
                AggregationLevel = req.AggregationLevel,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.ConceptValidationRules.Add(existing);
        }
        await _db.SaveChangesAsync(ct);

        var concept = await _db.Concepts.FindAsync(existing.ConceptId);
        return new ConceptValidationRuleDto(
            existing.Id, existing.ConceptId, concept?.Nombre ?? "", existing.PaymentModelType,
            existing.IsApplicable, existing.IsMandatory, existing.CalculationMethod, existing.AggregationLevel);
    }

    public async Task<IReadOnlyList<PaymentRatesConfigurationDto>> GetRatesAsync(int paymentModelId, CancellationToken ct)
    {
        var model = await _db.PaymentModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == paymentModelId && !m.IsDeleted, ct)
            ?? throw new InvalidOperationException("Modelo de pago no encontrado");

        return await _db.PaymentRatesConfigurations
            .AsNoTracking()
            .Include(r => r.Concept)
            .Where(r => r.ClientId == model.ClientId && !r.IsDeleted)
            .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
            .Select(r => new PaymentRatesConfigurationDto(
                r.Id, r.ClientId, r.ConceptId, r.Concept != null ? r.Concept.Nombre : null,
                r.Year, r.Month, r.BaseRate, r.RateType,
                r.RateFormula, r.MinValue, r.MaxValue))
            .ToListAsync(ct);
    }

    public async Task<PaymentRatesConfigurationDto> UpsertRateAsync(PaymentRatesConfigurationUpsertRequest req, int usuarioId, CancellationToken ct)
    {
        var existing = await _db.PaymentRatesConfigurations
            .FirstOrDefaultAsync(r => r.ClientId == req.ClientId && r.ConceptId == req.ConceptId
                && r.Year == req.Year && r.Month == req.Month && !r.IsDeleted, ct);

        if (existing != null)
        {
            existing.BaseRate = req.BaseRate;
            existing.RateType = req.RateType;
            existing.RateFormula = req.RateFormula;
            existing.MinValue = req.MinValue;
            existing.MaxValue = req.MaxValue;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new PaymentRatesConfiguration
            {
                ClientId = req.ClientId,
                ConceptId = req.ConceptId,
                Year = req.Year,
                Month = req.Month,
                BaseRate = req.BaseRate,
                RateType = req.RateType,
                RateFormula = req.RateFormula,
                MinValue = req.MinValue,
                MaxValue = req.MaxValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.PaymentRatesConfigurations.Add(existing);
        }
        await _db.SaveChangesAsync(ct);

        var concept = existing.ConceptId.HasValue ? await _db.Concepts.FindAsync(existing.ConceptId) : null;
        return new PaymentRatesConfigurationDto(
            existing.Id, existing.ClientId, existing.ConceptId, concept?.Nombre,
            existing.Year, existing.Month, existing.BaseRate, existing.RateType,
            existing.RateFormula, existing.MinValue, existing.MaxValue);
    }

    public async Task<bool> IsConceptApplicableAsync(int conceptId, string paymentModelType, CancellationToken ct)
    {
        var rule = await _db.ConceptValidationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ConceptId == conceptId && r.PaymentModelType == paymentModelType && !r.IsDeleted, ct);
        return rule?.IsApplicable ?? true; // default: applicable if no rule exists
    }
}
