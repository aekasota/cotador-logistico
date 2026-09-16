create or replace function app_secrets.delete_secret(p_key text)
returns void
language plpgsql
security definer
set search_path = vault, public
as $$
begin
    delete from vault.secrets where name = p_key;
end;
$$;

revoke execute on function app_secrets.delete_secret(text) from public, anon, authenticated;
grant execute on function app_secrets.delete_secret(text) to service_role;

create table public.user_integrations (
    user_id                     uuid primary key references public.profiles (id) on delete cascade,
    frenet_updated_at          timestamptz,
    frenet_updated_by          uuid references public.profiles (id) on delete set null,
    melhor_envio_updated_at    timestamptz,
    melhor_envio_updated_by    uuid references public.profiles (id) on delete set null,
    gemini_updated_at          timestamptz,
    gemini_updated_by          uuid references public.profiles (id) on delete set null
);

comment on table public.user_integrations is
    'Uma linha por usuário, com a trilha de auditoria (quando/por quem) de '
    'cada integração configurada. "por quem" normalmente é o próprio '
    'usuário (só ele configura a própria), mas pode ser um OWNER que '
    'resetou a integração de outra pessoa. NUNCA guarda o valor do segredo '
    'nem substitui ISecretsStore como fonte da verdade de "está '
    'configurado?" — ver app_secrets / vault.';

do $$
declare
    r record;
    v_user_id uuid;
begin
    for r in select * from public.app_settings loop
        v_user_id := coalesce(r.frenet_updated_by, r.melhor_envio_updated_by, r.gemini_updated_by);
        if v_user_id is not null then
            insert into public.user_integrations (
                user_id, frenet_updated_at, frenet_updated_by,
                melhor_envio_updated_at, melhor_envio_updated_by,
                gemini_updated_at, gemini_updated_by
            )
            values (
                v_user_id, r.frenet_updated_at, r.frenet_updated_by,
                r.melhor_envio_updated_at, r.melhor_envio_updated_by,
                r.gemini_updated_at, r.gemini_updated_by
            )
            on conflict (user_id) do nothing;
        end if;
    end loop;
end $$;

alter table public.user_integrations enable row level security;

create policy user_integrations_select_self
    on public.user_integrations for select
    to authenticated
    using (user_id = auth.uid());

create policy user_integrations_select_owner
    on public.user_integrations for select
    to authenticated
    using (
        public.current_profile_role() = 'OWNER'
        and exists (
            select 1 from public.profiles p
            where p.id = user_integrations.user_id
              and p.organization_id = public.current_profile_organization_id()
        )
    );

revoke all on public.user_integrations from anon, authenticated;
grant select on public.user_integrations to authenticated;

do $$
declare
    v_org_id uuid;
    v_user_id uuid;
    v_val text;
    v_key text;
begin
    select coalesce(frenet_updated_by, melhor_envio_updated_by, gemini_updated_by)
        into v_user_id
        from public.app_settings
        limit 1;

    select id into v_org_id from public.organizations order by created_at limit 1;

    if v_user_id is not null and v_org_id is not null
       and exists (select 1 from pg_extension where extname = 'supabase_vault') then
        foreach v_key in array array['frenet_token', 'melhor_envio_token', 'gemini_api_key']
        loop
            v_val := app_secrets.get_secret(v_org_id::text || ':' || v_key);
            if v_val is not null then
                perform app_secrets.set_secret(v_user_id::text || ':' || v_key, v_val);
                perform app_secrets.delete_secret(v_org_id::text || ':' || v_key);
            end if;
        end loop;
    end if;

    if v_user_id is not null and v_org_id is not null and to_regclass('public.encrypted_secrets') is not null then
        update public.encrypted_secrets
        set key = v_user_id::text || ':' || split_part(key, ':', 2)
        where key = v_org_id::text || ':frenet_token'
           or key = v_org_id::text || ':melhor_envio_token'
           or key = v_org_id::text || ':gemini_api_key';
    end if;
end $$;

drop table public.app_settings;
