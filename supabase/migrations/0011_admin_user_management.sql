alter table public.profiles add column is_active boolean not null default true;
alter table public.profiles add column must_change_password boolean not null default false;

comment on column public.profiles.is_active is
    'Se false, o usuário não consegue mais usar a aplicação (bloqueado no '
    'backend, ver CurrentUserMiddleware — nunca só escondido na interface). '
    'Só o OWNER pode alterar, e nunca a própria conta (ver seção 21).';

comment on column public.profiles.must_change_password is
    'true logo após o OWNER gerar uma senha temporária para este usuário '
    '(seção 19/106). Enquanto true, o backend bloqueia qualquer chamada que '
    'não seja GET /api/me ou POST /api/me/change-password — força a troca '
    'antes de liberar o resto da aplicação.';

create table public.audit_logs (
    id                  uuid primary key default gen_random_uuid(),
    organization_id     uuid not null references public.organizations (id),
    actor_id            uuid references public.profiles (id) on delete set null,
    action              text not null,
    target_user_id      uuid references public.profiles (id) on delete set null,
    metadata            jsonb,
    created_at          timestamptz not null default now()
);

comment on table public.audit_logs is
    'Trilha de auditoria administrativa (seção 47): quem fez o quê, quando, '
    'e sobre qual usuário. NUNCA contém senha, API key, token, secret ou '
    'JWT — metadata só guarda detalhes não sensíveis (ex.: role antigo/novo).';

create index audit_logs_org_created_at_idx on public.audit_logs (organization_id, created_at desc);
create index audit_logs_target_user_id_idx on public.audit_logs (target_user_id);

alter table public.audit_logs enable row level security;

create policy audit_logs_select_owner
    on public.audit_logs for select
    to authenticated
    using (
        public.current_profile_role() = 'OWNER'
        and organization_id = public.current_profile_organization_id()
    );

revoke all on public.audit_logs from anon, authenticated;
grant select on public.audit_logs to authenticated;
