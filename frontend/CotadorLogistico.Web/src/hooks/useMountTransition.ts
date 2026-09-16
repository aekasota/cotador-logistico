import { useEffect, useRef, useState } from 'react';

export function useMountTransition(isOpen: boolean, exitDurationMs: number): { shouldRender: boolean; isActive: boolean } {
  const [shouldRender, setShouldRender] = useState(isOpen);
  const [isActive, setIsActive] = useState(false);
  const rafIds = useRef<number[]>([]);
  const timeoutId = useRef<number | undefined>(undefined);

  useEffect(() => {
    rafIds.current.forEach(cancelAnimationFrame);
    rafIds.current = [];
    window.clearTimeout(timeoutId.current);

    if (isOpen) {
      setShouldRender(true);
      const raf1 = requestAnimationFrame(() => {
        const raf2 = requestAnimationFrame(() => setIsActive(true));
        rafIds.current.push(raf2);
      });
      rafIds.current.push(raf1);
      return;
    }

    setIsActive(false);
    timeoutId.current = window.setTimeout(() => setShouldRender(false), exitDurationMs);
  }, [isOpen, exitDurationMs]);

  useEffect(
    () => () => {
      rafIds.current.forEach(cancelAnimationFrame);
      window.clearTimeout(timeoutId.current);
    },
    [],
  );

  return { shouldRender, isActive };
}
