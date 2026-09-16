import { readdirSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import pg from 'pg';

const here = path.dirname(fileURLToPath(import.meta.url));
const migrationsDir = path.join(here, '..', 'migrations');

const required = ['PGHOST', 'PGDATABASE', 'PGUSER', 'PGPASSWORD'];
const missing = required.filter((key) => !process.env[key]);
if (missing.length > 0) {
  console.error(`Faltou definir: ${missing.join(', ')} (e opcionalmente PGPORT, padrão 5432).`);
  process.exit(1);
}

const client = new pg.Client({
  host: process.env.PGHOST,
  port: Number(process.env.PGPORT ?? 5432),
  database: process.env.PGDATABASE,
  user: process.env.PGUSER,
  password: process.env.PGPASSWORD,
  ssl: { rejectUnauthorized: false },
});

const files = readdirSync(migrationsDir).filter((f) => f.endsWith('.sql')).sort();

await client.connect();
console.log(`Conectado. ${files.length} migration(s) encontrada(s).`);

try {
  await client.query(`
    create table if not exists public._migrations_applied (
      filename    text primary key,
      applied_at  timestamptz not null default now()
    );
  `);

  const { rows } = await client.query('select filename from public._migrations_applied');
  const applied = new Set(rows.map((r) => r.filename));

  for (const file of files) {
    if (applied.has(file)) {
      console.log(`\n--- Pulando ${file} (já aplicada anteriormente) ---`);
      continue;
    }

    const sql = readFileSync(path.join(migrationsDir, file), 'utf8');
    console.log(`\n--- Aplicando ${file} ---`);
    await client.query(sql);
    await client.query('insert into public._migrations_applied (filename) values ($1)', [file]);
    console.log(`OK: ${file}`);
  }
  console.log('\nTodas as migrations pendentes foram aplicadas com sucesso.');
} finally {
  await client.end();
}
