import { useEffect, useRef, type ReactNode } from 'react';
import { createPortal } from 'react-dom';
import { useI18n } from '../../contexts/I18nContext';
import { useMountTransition } from '../../hooks/useMountTransition';
import { CloseIcon } from './Icons';

interface PopoverProps {
  isOpen: boolean;
  onClose: () => void;

  anchorEl: HTMLElement | null;
  title?: string;
  children: ReactNode;

  wide?: boolean;
}

export function Popover({ isOpen, onClose, anchorEl, title, children, wide }: PopoverProps) {
  const { t } = useI18n();
  const cardRef = useRef<HTMLDivElement>(null);
  const { shouldRender, isActive } = useMountTransition(isOpen, 220);

  useEffect(() => {
    if (!isOpen) return;

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose();
    }
    document.addEventListener('keydown', handleKeyDown);

    const firstFocusable = cardRef.current?.querySelector<HTMLElement>('button, [href], input, select, textarea, [tabindex]');
    firstFocusable?.focus();

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      anchorEl?.focus();
    };
  }, [isOpen]);

  if (!shouldRender) return null;

  const rect = anchorEl?.getBoundingClientRect();
  const cardWidth = wide ? 640 : 380;
  const margin = 12;

  let top = (rect?.bottom ?? window.innerHeight / 2) + 8;
  let left = rect ? Math.min(Math.max(rect.left, margin), window.innerWidth - cardWidth - margin) : window.innerWidth / 2 - cardWidth / 2;

  const estimatedHeight = 320;
  if (rect && top + estimatedHeight > window.innerHeight - margin) {
    top = Math.max(margin, rect.top - estimatedHeight - 8);
  }
  if (left < margin) left = margin;

  top += window.scrollY;
  left += window.scrollX;

  return createPortal(
    <>
      <div className={`popover-backdrop ${isActive ? 'is-open' : ''}`} onMouseDown={onClose} />
      <div
        ref={cardRef}
        className={`popover-card ${isActive ? 'is-open' : ''} ${wide ? 'is-wide' : ''}`}
        style={{ top, left, width: `min(${cardWidth}px, 92vw)` }}
        role="dialog"
        aria-modal="true"
        aria-label={title}
      >
        <div className="popover-header">
          {title && <div className="popover-title">{title}</div>}
          <button type="button" className="popover-close-btn" onClick={onClose} aria-label={t('common.close')}>
            <CloseIcon />
          </button>
        </div>
        {children}
      </div>
    </>,
    document.body,
  );
}
