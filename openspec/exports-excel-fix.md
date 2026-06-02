# Excel Export Fix - Hybrid Archive

**Status**: ✅ COMPLETE - 4/4 E2E Tests PASSING  
**Date**: 2026-06-01  
**Commit**: ffc5bbb - fix: Excel exports con datos y cargas explícitas de soft-delete navigation properties

## Problem Statement

E2E tests for Excel exports (A3 Innuva .xls and A3 ERP .xlsx) were failing:
1. A3 Innuva test: returning 0 rows instead of employee data
2. A3 ERP test: VAT calculation test expecting 21% but receiving 0%
3. Root cause: Multiple intersecting issues with data, architecture, and test validation

## Root Causes & Fixes

### Issue 1: Empty Excel Exports (0 rows)
**Root Cause**: Seed data was not assigning `UserId` to `ClosureLines`  
**Fix**: Updated `DataSeeder.cs` to rotate through field users and assign `UserId` via modulo operation  
**File**: `backend/SIG.Infrastructure/Seed/DataSeeder.cs` (lines 463-496)

### Issue 2: Missing Column Data
**Root Cause**: Concepts didn't have `ColumnaA3` property set  
**Fix**: Updated seed to assign `ColumnaA3 = "ImporteBruto"` to 5 Pago concepts  
**File**: `backend/SIG.Infrastructure/Seed/DataSeeder.cs` (lines 213-239)

### Issue 3: Navigation Properties Not Loading
**Root Cause**: EF Core global query filters apply independently to navigation properties. `.IgnoreQueryFilters()` on root query doesn't suppress filters on included entities.  
**Fix**: Explicit separate loading of `Concept` and `Client` entities with individual `.IgnoreQueryFilters()` calls  
**File**: `backend/SIG.Infrastructure/Services/ExportService.cs` (lines 194-230)  
**Pattern**: 
```csharp
// Load root
var closure = await _db.Closures...IgnoreQueryFilters().FirstOrDefaultAsync();

// Explicitly load soft-delete-filtered navigation properties
var concepts = await _db.Concepts.IgnoreQueryFilters().Where(...).ToDictionaryAsync();
var client = await _db.Clients.IgnoreQueryFilters().FirstOrDefaultAsync();

// Attach to parent
foreach (var line in closure.Lines)
    if (concepts.TryGetValue(line.ConceptId, out var concept))
        line.Concept = concept;
```

### Issue 4: VAT Test Validation Error
**Root Cause**: Test was parsing `parseFloat('')` from TOTAL row's empty IVA field, resulting in NaN which converts to 0  
**Fix**: Added skip logic to filter out TOTAL row before validation  
**File**: `frontend/e2e/exports.spec.ts` (lines 296-299)
```typescript
if (String(row['Concepto'] || '').trim().toUpperCase() === 'TOTAL') {
  return; // Skip TOTAL row - has empty VAT field
}
```

## Architecture Pattern Documented

New document: `ARCHITECTURE.md`

Critical pattern for future development:
- When loading navigation properties from soft-delete-filtered entities, always check if the navigation entity has `HasQueryFilter()`
- If yes, load separately with explicit `.IgnoreQueryFilters()`
- Manually attach loaded entities to their parents

This pattern is needed because EF Core filters apply independently and cannot be bypassed via the root query's `.IgnoreQueryFilters()`.

## Test Results

```
Running 4 tests using 1 worker

✅ Test 1: Descargar A3 Innuva (.xls) - estructura y datos válidos
✅ Test 2: Descargar A3 ERP (.xlsx) - estructura y datos válidos  
✅ Test 3: VAT calculation en A3 ERP - validar tasas según país
✅ Test 4: Flujo completo: descargar A3 Innuva y A3 ERP en secuencia

4 passed
```

## Changed Files

| File | Change | Purpose |
|------|--------|---------|
| `backend/SIG.Infrastructure/Services/ExportService.cs` | Added explicit navigation property loading with `.IgnoreQueryFilters()` | Fix soft-delete filter behavior |
| `backend/SIG.Infrastructure/Seed/DataSeeder.cs` | Added UserId and ColumnaA3 assignments | Provide test data for exports |
| `frontend/e2e/exports.spec.ts` | Added TOTAL row skip logic | Fix test validation |
| `ARCHITECTURE.md` | New file documenting EF Core pattern | Future development guidance |

## Key Learnings

1. **Include() doesn't bypass filters** - Navigation property filters execute independently of root query filters
2. **Must be explicit** - No implicit way to suppress filters on included entities from root query
3. **Seed data matters** - Export validation requires complete seed (UserId, ColumnaA3, proper client country)
4. **Test validation precision** - Careful parsing of Excel data, skip rows with empty computed fields

## Next Steps (Optional)

- Consider moving soft-delete-filtered navigation loading to a reusable helper/extension method
- Add code comments in future soft-delete filter configurations to alert developers
- Consider using a Specification pattern for complex navigation loading scenarios

---

Generated: 2026-06-01  
Archive Status: Complete - Ready for team review
