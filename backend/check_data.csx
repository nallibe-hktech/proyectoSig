// Quick script to check staging data for 2026-06
var fechaInicio = new DateOnly(2026, 6, 1);
var fechaFin = new DateOnly(2026, 6, 30);

Console.WriteLine("Checking data for period 2026-06...\n");

// This would need actual DB access - showing the queries instead

var intratime_query = @"
SELECT COUNT(*) as cnt FROM staging_intratime_fichajes 
WHERE entrada >= '2026-06-01'::timestamp 
  AND entrada <= '2026-06-30'::timestamp 
  AND horas_calculadas IS NOT NULL;
";

var celero_query = @"
SELECT COUNT(*) as cnt FROM staging_celero_visitas
WHERE fecha >= '2026-06-01' 
  AND fecha <= '2026-06-30'
  AND resource_nif IS NOT NULL;
";

var travelperk_query = @"
SELECT COUNT(*) as cnt FROM staging_travel_perk_lineas
WHERE fecha_gasto >= '2026-06-01'
  AND fecha_gasto <= '2026-06-30';
";

var bizneo_query = @"
SELECT COUNT(*) as cnt FROM staging_bizneo_absences
WHERE fecha >= '2026-06-01'
  AND fecha <= '2026-06-30';
";

Console.WriteLine("Intratime fichajes (2026-06):");
Console.WriteLine(intratime_query);

Console.WriteLine("\nCelero visitas (2026-06):");
Console.WriteLine(celero_query);

Console.WriteLine("\nTravelPerk lineas (2026-06):");
Console.WriteLine(travelperk_query);

Console.WriteLine("\nBizneo absences (2026-06):");
Console.WriteLine(bizneo_query);
