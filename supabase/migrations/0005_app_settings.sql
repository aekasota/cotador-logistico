create table public.app_settings (
    id                          boolean primary key default true,
    frenet_updated_at          timestamptz,
    frenet_updated_by          uuid references public.profiles (id) on delete set null,
    melhor_envio_updated_at    timestamptz,
    melhor_envio_updated_by    uuid references public.profiles (id) on delete set null,
    gemini_updated_at          timestamptz,
    gemini_updated_by          uuid references public.profiles (id) on delete set null,

    constraint app_settings_single_row check (id)
);

comment on table public.app_settings is
    'Linha única com a trilha de auditoria (quando/por quem) de cada '
    'integração configurada. NUNCA guarda o valor do segredo nem substitui '
    'ISecretsStore como fonte da verdade de "está configurado?" — ver '
    'app_secrets / vault.';

insert into public.app_settings (id) values (true);

alter table public.app_settings enable row level security;

create policy app_settings_select_authenticated
    on public.app_settings for select
    to authenticated
    using (true);

revoke all on public.app_settings from anon, authenticated;
grant select on public.app_settings to authenticated;
