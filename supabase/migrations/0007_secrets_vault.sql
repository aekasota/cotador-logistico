create schema if not exists app_secrets;
revoke all on schema app_secrets from public, anon, authenticated;

create or replace function app_secrets.set_secret(p_key text, p_value text)
returns void
language plpgsql
security definer
set search_path = vault, public
as $$
declare
    v_existing_id uuid;
begin
    select id into v_existing_id from vault.secrets where name = p_key limit 1;

    if v_existing_id is not null then
        perform vault.update_secret(v_existing_id, p_value);
    else
        perform vault.create_secret(p_value, p_key, 'Cotador Logistico - ' || p_key);
    end if;
end;
$$;

create or replace function app_secrets.get_secret(p_key text)
returns text
language sql
stable
security definer
set search_path = vault, public
as $$
    select decrypted_secret from vault.decrypted_secrets where name = p_key limit 1;
$$;

create or replace function app_secrets.is_configured(p_key text)
returns boolean
language sql
stable
security definer
set search_path = vault, public
as $$
    select exists (select 1 from vault.secrets where name = p_key);
$$;

revoke execute on function app_secrets.set_secret(text, text) from public, anon, authenticated;
revoke execute on function app_secrets.get_secret(text) from public, anon, authenticated;
revoke execute on function app_secrets.is_configured(text) from public, anon, authenticated;

grant usage on schema app_secrets to service_role;
grant execute on function app_secrets.set_secret(text, text) to service_role;
grant execute on function app_secrets.get_secret(text) to service_role;
grant execute on function app_secrets.is_configured(text) to service_role;
