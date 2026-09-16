import pg from 'pg';

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

await client.connect();

const { rows: orgs } = await client.query(
  'select id, name, created_at from public.organizations order by created_at',
);

const { rows: profiles } = await client.query(`
  select p.organization_id, p.id, u.email, p.name, p.role, p.supervisor_id
  from public.profiles p
  join auth.users u on u.id = p.id
  order by p.organization_id, p.role desc, p.name
`);

for (const org of orgs) {
  console.log(`\n=== ${org.name} (organization_id: ${org.id}) — criada em ${org.created_at.toISOString()} ===`);
  const members = profiles.filter((p) => p.organization_id === org.id);
  if (members.length === 0) {
    console.log('  (nenhum usuário)');
    continue;
  }
  for (const m of members) {
    console.log(`  [${m.role}] ${m.name} <${m.email}> — id: ${m.id}${m.supervisor_id ? ` (supervisor_id: ${m.supervisor_id})` : ''}`);
  }
}

if (orgs.length === 0) console.log('Nenhuma organização encontrada.');

await client.end();
