create table public.encrypted_secrets (
    key             text primary key,
    ciphertext      bytea not null,
    nonce           bytea not null,
    auth_tag        bytea not null,
    updated_at      timestamptz not null default now()
);

comment on table public.encrypted_secrets is
    'Alternativa ao Supabase Vault: AES-256-GCM com chave mestra só no '
    'backend. Nunca contém o segredo em claro.';

alter table public.encrypted_secrets enable row level security;

revoke all on public.encrypted_secrets from anon, authenticated;
