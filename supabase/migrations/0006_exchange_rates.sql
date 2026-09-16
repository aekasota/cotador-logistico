create table public.exchange_rates (
    currency            text primary key check (currency in ('USD', 'MXN')),
    rate_to_brl         numeric(14, 6) not null check (rate_to_brl > 0),
    rate_from_brl       numeric(14, 6) not null check (rate_from_brl > 0),
    source              text not null default 'frankfurter.dev',
    effective_date      date not null,
    updated_at          timestamptz not null default now()
);

comment on table public.exchange_rates is
    'Câmbio central (1 linha por moeda estrangeira suportada), atualizado '
    'por um BackgroundService. quotes.exchange_rate_used congela o valor '
    'usado em cada cotação — esta tabela nunca é usada para recalcular o '
    'passado.';

alter table public.exchange_rates enable row level security;

create policy exchange_rates_select_authenticated
    on public.exchange_rates for select
    to authenticated
    using (true);

revoke all on public.exchange_rates from anon, authenticated;
grant select on public.exchange_rates to authenticated;
