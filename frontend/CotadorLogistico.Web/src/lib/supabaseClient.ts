import { createClient } from '@supabase/supabase-js';

const supabaseUrl = import.meta.env.VITE_SUPABASE_URL as string | undefined;
const supabasePublishableKey = import.meta.env.VITE_SUPABASE_PUBLISHABLE_KEY as string | undefined;

if (!supabaseUrl || !supabasePublishableKey) {
  console.error(
    'VITE_SUPABASE_URL / VITE_SUPABASE_PUBLISHABLE_KEY não configuradas. ' +
      'Copie .env.example para .env.local e preencha com os dados do seu projeto Supabase.',
  );
}

export const supabase = createClient(supabaseUrl ?? '', supabasePublishableKey ?? '', {
  auth: {
    persistSession: true,
    autoRefreshToken: true,
  },
});
