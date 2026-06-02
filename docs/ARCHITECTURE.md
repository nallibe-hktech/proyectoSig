# SIG-es Architecture & Patterns

## Critical Pattern: EF Core Global Query Filters on Navigation Properties

### Problem
Entity Framework Core applies global query filters independently on each entity. When a navigation property's entity (e.g., `Concept`, `Client`) has `HasQueryFilter()` defined, the filter is applied **regardless** of `.IgnoreQueryFilters()` called on the root query.

**Example of the Issue:**
```csharp
// ❌ WRONG - This WILL NOT bypass Concept's soft-delete filter
var closure = await _db.Closures
    .Include(c => c.Lines).ThenInclude(l => l.Concept)
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(c => c.Id == id);
// closure.Lines[0].Concept will be NULL if Concept.IsDeleted == true
// because Concept entity still has HasQueryFilter(c => !c.IsDeleted) applied
```

### Root Cause
- Configuration file: `backend/SIG.Infrastructure/Persistence/Configurations/Configurations.cs`
- Multiple entities have soft-delete filters:
  - `Concept`: `HasQueryFilter(c => !c.IsDeleted)`
  - `Client`: `HasQueryFilter(c => !c.IsDeleted)`
  - `Project`, `User`, `Action`, `CostCenter`, etc.

When these are included as navigation properties, their filters execute independently.

### Solution
Load soft-delete-filtered navigation properties **separately** with explicit `.IgnoreQueryFilters()`:

```csharp
// ✅ CORRECT - Explicit separate load
private async Task<Closure> LoadClosureForExportAsync(int id, CancellationToken ct)
{
    // Load root + non-filtered navigations
    var closure = await _db.Closures
        .Include(c => c.Project)
        .Include(c => c.Period)
        .Include(c => c.Lines)
        .Include(c => c.Lines).ThenInclude(l => l.User).ThenInclude(u => u!.Department)
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(c => c.Id == id, ct);

    // Explicitly load Concept (soft-delete filter)
    var conceptIds = closure.Lines.Select(l => l.ConceptId).Distinct().ToList();
    var concepts = await _db.Concepts
        .IgnoreQueryFilters()  // ← Explicit
        .Where(c => conceptIds.Contains(c.Id))
        .ToDictionaryAsync(c => c.Id, ct);

    // Explicitly load Client (soft-delete filter)
    if (closure.Project?.ClientId > 0)
    {
        var client = await _db.Clients
            .IgnoreQueryFilters()  // ← Explicit
            .FirstOrDefaultAsync(c => c.Id == closure.Project.ClientId, ct);
        if (closure.Project != null)
            closure.Project.Client = client;
    }

    // Attach loaded entities
    foreach (var line in closure.Lines)
    {
        if (concepts.TryGetValue(line.ConceptId, out var concept))
            line.Concept = concept;
    }

    return closure;
}
```

### Implementation Details
- **File**: `backend/SIG.Infrastructure/Services/ExportService.cs`
- **Method**: `LoadClosureForExportAsync()`
- **Usage**: Called by `ExportA3InnuvaAsync()` and `ExportA3ErpAsync()` to fetch data for Excel exports
- **Impact**: Without this pattern, exports would return 0 rows even with valid data in database

### Key Learnings
1. **Include() doesn't bypass filters** - Navigation property filters are applied independently
2. **Must be explicit** - No implicit way to skip filters on included entities
3. **Applied in ExportService only** - This pattern is needed when exporting data that must include soft-deleted navigation properties
4. **Clean Architecture impact** - Services must be aware of EF Core filter behavior to ensure complete data loading

### Recommendations for Future Code
- When using navigation properties from soft-delete-filtered entities, always:
  1. Check if the navigation entity has `HasQueryFilter()`
  2. If yes, load separately with explicit `.IgnoreQueryFilters()`
  3. Manually attach loaded entities to their parents
- Consider marking soft-delete filters with code comments to alert future developers
- Document this pattern in service method comments

---

## Excel Export Architecture

### Components
- **ExportService**: Handles A3 Innuva (.xls) and A3 ERP (.xlsx) generation
- **Libraries**: ClosedXML (for .xlsx), NPOI (for .xls)
- **Data Source**: Closure + ClosureLines + Concepts + Client

### A3 Innuva Format (.xls)
- Groups ClosureLines by UserId (field workers)
- Aggregates amounts by ColumnaA3 mapping (ImporteBruto, IRPF, etc.)
- Requires: ClosureLines.UserId + Concept.ColumnaA3

### A3 ERP Format (.xlsx)
- Lists invoice lines with VAT calculation
- VAT rates: 21% for Spain, 0% for intra-EU
- Client.Pais determines VAT rate

### E2E Test Considerations
- Test skips TOTAL row (has empty VAT field)
- Validates data presence, structure, and calculations
- Uses Playwright + XLSX library for Excel validation

---

## Data Seed Architecture

### Current Seed Data (DataSeeder.cs)
- **Concepts**: 5 Pago concepts with ColumnaA3 defined
- **ClosureLines**: UserId assigned via field user rotation
- **Clients**: Multiple test clients with Pais="España"
- **Closures**: Created with Line data for export testing

### Seed Strategy
- Only runs if database is empty (`RunIfEmptyAsync`)
- Creates complete closure workflow with calculations
- Includes approval history for workflow states

---

Generated: 2026-06-01 | Pattern documented after fixing exports issue
