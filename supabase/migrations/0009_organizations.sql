create table public.organizations (
    id          uuid primary key default gen_random_uuid(),
    name        text not null check (length(trim(name)) > 0),
    created_at  timestamptz not null default now()
);

comment on table public.organizations is
    'Uma organização = uma empresa/cliente usando o Cotador Logístico. Cada '
    'uma tem seu próprio OWNER, time e chaves de API — nada é compartilhado '
    'entre organizações diferentes. Criar uma nova organização (onboarding '
    'de um novo cliente) é uma operação administrativa via SQL (ver '
    'supabase/scripts/create-organization-and-owner.sql), nunca pela '
    'aplicação — não existe cadastro público (ver seção 5/8).';

alter table public.organizations enable row level security;

revoke all on public.organizations from anon, authenticated;

alter table public.profiles add column organization_id uuid references public.organizations (id);

do $$
declare
    v_org_id uuid;
begin

    if exists (select 1 from public.profiles where organization_id is null) then
        insert into public.organizations (name)
        values ('Empresa Principal')
        returning id into v_org_id;

        update public.profiles set organization_id = v_org_id where organization_id is null;
    end if;
end $$;

alter table public.profiles alter column organization_id set not null;
create index profiles_organization_id_idx on public.profiles (organization_id);

comment on column public.profiles.organization_id is
    'A empresa/conta a que este usuário pertence. SUPERVISOR/OPERATOR criados '
    'por um OWNER/SUPERVISOR sempre herdam a organização de quem os criou '
    '(ver CotadorLogistico.Api.Controllers.TeamController) — nunca é '
    'possível, pela aplicação, criar um perfil em outra organização.';

create or replace function public.current_profile_organization_id()
returns uuid
language sql
stable
security definer
set search_path = public
as $$
    select organization_id from public.profiles where id = auth.uid();
$$;

revoke all on function public.current_profile_organization_id() from public;
grant execute on function public.current_profile_organization_id() to authenticated;

drop policy if exists profiles_select_owner on public.profiles;
create policy profiles_select_owner
    on public.profiles for select
    to authenticated
    using (
        public.current_profile_role() = 'OWNER'
        and organization_id = public.current_profile_organization_id()
    );

drop policy if exists presence_select_owner on public.presence;
create policy presence_select_owner
    on public.presence for select
    to authenticated
    using (
        public.current_profile_role() = 'OWNER'
        and exists (
            select 1 from public.profiles p
            where p.id = presence.user_id and p.organization_id = public.current_profile_organization_id()
        )
    );

drop policy if exists quotes_select_owner on public.quotes;
create policy quotes_select_owner
    on public.quotes for select
    to authenticated
    using (
        public.current_profile_role() = 'OWNER'
        and exists (
            select 1 from public.profiles p
            where p.id = quotes.user_id and p.organization_id = public.current_profile_organization_id()
        )
    );

alter table public.app_settings add column organization_id uuid references public.organizations (id);

update public.app_settings
set organization_id = (select id from public.organizations order by created_at limit 1)
where organization_id is null;

alter table public.app_settings alter column organization_id set not null;
alter table public.app_settings drop constraint if exists app_settings_single_row;
alter table public.app_settings drop constraint app_settings_pkey;
alter table public.app_settings add primary key (organization_id);
alter table public.app_settings drop column id;

drop policy if exists app_settings_select_authenticated on public.app_settings;
create policy app_settings_select_authenticated
    on public.app_settings for select
    to authenticated
    using (organization_id = public.current_profile_organization_id());

comment on table public.app_settings is
    'Uma linha POR ORGANIZAÇÃO, com a trilha de auditoria (quando/por quem) '
    'de cada integração configurada NAQUELA organização. NUNCA guarda o '
    'valor do segredo nem substitui ISecretsStore como fonte da verdade de '
    '"está configurado?" — ver app_secrets / vault. Chaves de API nunca são '
    'compartilhadas entre organizações (ver comentário no topo do arquivo).';

do $$
declare
    v_org_id uuid;
    v_val text;
    v_key text;
begin
    select id into v_org_id from public.organizations order by created_at limit 1;

    if exists (select 1 from pg_extension where extname = 'supabase_vault') then
        foreach v_key in array array['frenet_token', 'melhor_envio_token', 'gemini_api_key']
        loop
            v_val := app_secrets.get_secret(v_key);
            if v_val is not null then
                perform app_secrets.set_secret(v_org_id::text || ':' || v_key, v_val);

                delete from vault.secrets where name = v_key;
            end if;
        end loop;
    end if;

    if to_regclass('public.encrypted_secrets') is not null then
        update public.encrypted_secrets
        set key = v_org_id::text || ':' || key
        where key in ('frenet_token', 'melhor_envio_token', 'gemini_api_key');
    end if;
end $$;
