create table public.quotes (
    id                      uuid primary key default gen_random_uuid(),
    user_id                 uuid not null references public.profiles (id) on delete cascade,
    created_at              timestamptz not null default now(),
    source_cep              text not null check (source_cep ~ '^\d{8}$'),
    destination_cep         text not null check (destination_cep ~ '^\d{8}$'),
    destination_label       text,
    package_weight_kg       numeric(10, 3) not null check (package_weight_kg > 0),
    package_length_cm       numeric(10, 2) not null check (package_length_cm > 0),
    package_width_cm        numeric(10, 2) not null check (package_width_cm > 0),
    package_height_cm       numeric(10, 2) not null check (package_height_cm > 0),
    package_quantity        integer not null default 1 check (package_quantity > 0),
    declared_value_brl      numeric(12, 2) not null check (declared_value_brl >= 0),
    comparison_mode         boolean not null default false,
    currency                text not null default 'BRL' check (currency in ('BRL', 'USD', 'MXN')),
    exchange_rate_used      numeric(14, 6),
    is_demo                 boolean not null default false
);

comment on table public.quotes is
    'Uma rota calculada (origem+destino+carga). exchange_rate_used fixa o '
    'câmbio do momento: uma cotação histórica nunca é recalculada com o '
    'câmbio de hoje (ver seção 26). is_demo nunca é true na prática hoje — '
    'cotações de demonstração não são persistidas (ver docs/ARCHITECTURE.md) '
    '— a coluna existe para o schema já suportar essa decisão mudar no futuro.';

create index quotes_user_id_created_at_idx on public.quotes (user_id, created_at desc);
create index quotes_destination_label_idx on public.quotes (destination_label) where is_demo = false;
create index quotes_is_demo_idx on public.quotes (is_demo);

create table public.quote_options (
    id                      uuid primary key default gen_random_uuid(),
    quote_id                uuid not null references public.quotes (id) on delete cascade,
    provider                text not null check (provider in ('FRENET', 'MELHOR_ENVIO')),
    carrier                 text not null,
    service_name            text not null,
    service_code            text,
    price_brl               numeric(12, 2) not null check (price_brl >= 0),
    delivery_days           integer not null check (delivery_days >= 0),
    is_winner_price         boolean not null default false,
    is_winner_time          boolean not null default false,
    was_selected_in_comparison boolean not null default false,
    raw_metadata_json       jsonb
);

comment on table public.quote_options is
    'Uma opção de frete (uma transportadora) para uma quotes. `provider` '
    'distingue de QUAL API a opção veio (FRENET/MELHOR_ENVIO) — `carrier` é '
    'o nome da transportadora de verdade (ex.: "Jadlog"), que pode se '
    'repetir entre providers e por isso não pode ser usado para agrupar '
    'métricas de comparação entre APIs (ver seção 27). Os vencedores de '
    'preço e de prazo são calculados de forma independente (ver seção 22) '
    '— uma opção pode vencer em um e perder no outro.';

create index quote_options_quote_id_idx on public.quote_options (quote_id);
create index quote_options_carrier_idx on public.quote_options (carrier);
create index quote_options_provider_idx on public.quote_options (provider);

alter table public.quotes enable row level security;
alter table public.quote_options enable row level security;

create policy quotes_select_self
    on public.quotes for select
    to authenticated
    using (user_id = auth.uid());

create policy quotes_select_team
    on public.quotes for select
    to authenticated
    using (
        exists (
            select 1 from public.profiles p
            where p.id = quotes.user_id and p.supervisor_id = auth.uid()
        )
    );

create policy quotes_select_owner
    on public.quotes for select
    to authenticated
    using (public.current_profile_role() = 'OWNER');

create policy quotes_insert_self
    on public.quotes for insert
    to authenticated
    with check (user_id = auth.uid());

create policy quote_options_select_via_quote
    on public.quote_options for select
    to authenticated
    using (
        exists (
            select 1 from public.quotes q where q.id = quote_options.quote_id
        )
    );

create policy quote_options_insert_via_quote
    on public.quote_options for insert
    to authenticated
    with check (
        exists (
            select 1 from public.quotes q
            where q.id = quote_options.quote_id and q.user_id = auth.uid()
        )
    );

revoke all on public.quotes from anon, authenticated;
revoke all on public.quote_options from anon, authenticated;
grant select, insert on public.quotes to authenticated;
grant select, insert on public.quote_options to authenticated;
