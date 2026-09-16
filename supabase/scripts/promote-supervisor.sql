do $$
declare
    v_user_id uuid := '<USER_UUID_AQUI>';
begin
    if not exists (select 1 from public.profiles where id = v_user_id) then
        raise exception 'Nenhum profile encontrado para o id %.', v_user_id;
    end if;

    update public.profiles
    set role = 'SUPERVISOR'
    where id = v_user_id and role = 'OPERATOR';

    if not found then
        raise notice 'Nada alterado: usuário % já não está como OPERATOR (verifique o role atual).', v_user_id;
    else
        raise notice 'Usuário % promovido a SUPERVISOR.', v_user_id;
    end if;
end $$;
