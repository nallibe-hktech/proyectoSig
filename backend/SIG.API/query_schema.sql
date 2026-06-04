SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'staging_celero_visitas'
ORDER BY ordinal_position;
