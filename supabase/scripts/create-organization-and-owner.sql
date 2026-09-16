do $$
declare
    v_org_id       uuid;
    v_org_name     text := '<NOME_DA_EMPRESA_AQUI>';
    v_user_id      uuid := '<USER_UUID_AQUI>';
    v_name         text := '<NOME_COMPLETO_AQUI>';
    v_position     text := 'Owner';
begin
    if not exists (select 1 from auth.users where id = v_user_id) then
        raise exception 'Nenhum usuário encontrado em auth.users com id %. Crie-o primeiro no Supabase Studio.', v_user_id;
    end if;

    insert into public.organizations (name)
    values (v_org_name)
    returning id into v_org_id;

    insert into public.profiles (id, organization_id, name, "position", role, supervisor_id)
    values (v_user_id, v_org_id, v_name, v_position, 'OWNER', null)
    on conflict (id) do update
        set organization_id = v_org_id,
            role = 'OWNER',
            supervisor_id = null;

    raise notice 'Organização "%" criada (id %). Usuário % agora é seu OWNER.', v_org_name, v_org_id, v_user_id;
end $$;
