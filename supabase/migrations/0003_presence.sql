create table public.presence (
    user_id         uuid primary key references public.profiles (id) on delete cascade,
    reported_status text not null default 'ONLINE'
                        check (reported_status in ('ONLINE', 'QUOTING', 'OFFLINE')),
    last_seen_at    timestamptz not null default now()
);

comment on table public.presence is
    'Heartbeat por usuário. O status exibido na UI é sempre recalculado a '
    'partir de last_seen_at (janela de ~90s) — ver presence_effective_status().';

create or replace function public.presence_effective_status(
    p_reported_status text,
    p_last_seen_at timestamptz
) returns text
language sql
immutable
as $$
    select case
        when p_last_seen_at is null then 'OFFLINE'
        when now() - p_last_seen_at > interval '90 seconds' then 'OFFLINE'
        else p_reported_status
    end;
$$;

alter table public.presence enable row level security;

create policy presence_select_self
    on public.presence for select
    to authenticated
    using (user_id = auth.uid());

create policy presence_select_team
    on public.presence for select
    to authenticated
    using (
        exists (
            select 1 from public.profiles p
            where p.id = presence.user_id and p.supervisor_id = auth.uid()
        )
    );

create policy presence_select_owner
    on public.presence for select
    to authenticated
    using (public.current_profile_role() = 'OWNER');

create policy presence_upsert_self
    on public.presence for insert
    to authenticated
    with check (user_id = auth.uid());

create policy presence_update_self
    on public.presence for update
    to authenticated
    using (user_id = auth.uid())
    with check (user_id = auth.uid());

revoke all on public.presence from anon, authenticated;
grant select, insert, update on public.presence to authenticated;
