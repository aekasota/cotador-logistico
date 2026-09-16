import { useEffect, useRef } from 'react';
import { api } from '../lib/apiClient';
import { useAuth } from '../contexts/AuthContext';

const HEARTBEAT_INTERVAL_MS = 25_000;

export function usePresenceHeartbeat(isQuoting: boolean): void {
  const { status } = useAuth();
  const isQuotingRef = useRef(isQuoting);
  isQuotingRef.current = isQuoting;

  useEffect(() => {
    if (status !== 'authenticated') return;

    function sendHeartbeat() {
      const heartbeatStatus = isQuotingRef.current ? 'QUOTING' : 'ONLINE';
      api.post('/api/presence/heartbeat', { status: heartbeatStatus }).catch(() => {
      });
    }

    sendHeartbeat();
    const interval = setInterval(sendHeartbeat, HEARTBEAT_INTERVAL_MS);
    return () => clearInterval(interval);
  }, [status]);
}
