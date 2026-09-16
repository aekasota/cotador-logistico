create table public.profiles (
    id              uuid primary key references auth.users (id) on delete cascade,
    name            text not null check (length(trim(name)) > 0),
    "position"      text,
    role            text not null default 'OPERATOR'
                        check (role in ('OPERATOR', 'SUPERVISOR', 'OWNER')),
    supervisor_id   uuid references public.profiles (id) on delete set null,
    theme           text not null default 'light' check (theme in ('light', 'dark')),
    language        text not null default 'pt-BR' check (language in ('pt-BR', 'es-MX', 'en-US')),
    created_at      timestamptz not null default now(),
    updated_at      timestamptz not null default now(),

    constraint profiles_owner_has_no_supervisor
        check (role <> 'OWNER' or supervisor_id is null),

    constraint profiles_not_own_supervisor check (id <> supervisor_id)
);

comment on table public.profiles is
    'Perfil/role de cada usuário. Relacionado 1:1 com auth.users. A promoção '
    'para SUPERVISOR/OWNER é administrativa (ver supabase/scripts), nunca via API.';

create index profiles_supervisor_id_idx on public.profiles (supervisor_id);
create index profiles_role_idx on public.profiles (role);

create or replace function public.set_updated_at()
returns trigger
language plpgsql
as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

create trigger profiles_set_updated_at
    before update on public.profiles
    for each row
    execute function public.set_updated_at();

create or replace function public.current_profile_role()
returns text
language sql
stable
security definer
set search_path = public
as $$
    select role from public.profiles where id = auth.uid();
$$;

create or replace function public.current_profile_is_supervisor_or_owner()
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    select coalesce(public.current_profile_role() in ('SUPERVISOR', 'OWNER'), false);
$$;

revoke all on function public.current_profile_role() from public;
revoke all on function public.current_profile_is_supervisor_or_owner() from public;
grant execute on function public.current_profile_role() to authenticated;
grant execute on function public.current_profile_is_supervisor_or_owner() to authenticated;

alter table public.profiles enable row level security;

create policy profiles_select_self
    on public.profiles for select
    to authenticated
    using (id = auth.uid());

create policy profiles_select_team
    on public.profiles for select
    to authenticated
    using (supervisor_id = auth.uid());

create policy profiles_select_owner
    on public.profiles for select
    to authenticated
    using (public.current_profile_role() = 'OWNER');

revoke all on public.profiles from anon, authenticated;
grant select on public.profiles to authenticated;
