'use client';

import { Info } from 'lucide-react';
import { useId, useState } from 'react';
import { cn } from '@/lib/utils';

/** Buton/başlık yanında “i” bilgi ipucu */
export function HelpTip({
  text,
  className,
  side = 'top'
}: {
  text: string;
  className?: string;
  side?: 'top' | 'bottom' | 'left' | 'right';
}) {
  const id = useId();
  const [open, setOpen] = useState(false);

  return (
    <span className={cn('relative inline-flex align-middle', className)}>
      <button
        type="button"
        aria-describedby={open ? id : undefined}
        aria-label="Bilgi"
        onClick={() => setOpen((v) => !v)}
        onBlur={() => setOpen(false)}
        className="ml-1 inline-flex h-4 w-4 shrink-0 items-center justify-center rounded-full border border-slate-300 bg-white text-[10px] font-bold text-slate-500 hover:border-[color:var(--color-brand-500)] hover:text-[color:var(--color-brand-700)]"
      >
        <Info size={10} strokeWidth={2.5} />
      </button>
      {open && (
        <span
          id={id}
          role="tooltip"
          className={cn(
            'absolute z-50 w-56 rounded-md border border-slate-200 bg-white p-2 text-left text-[11px] font-normal leading-snug text-slate-600 shadow-lg',
            side === 'top' && 'bottom-full left-1/2 mb-1.5 -translate-x-1/2',
            side === 'bottom' && 'top-full left-1/2 mt-1.5 -translate-x-1/2',
            side === 'left' && 'right-full top-1/2 mr-1.5 -translate-y-1/2',
            side === 'right' && 'left-full top-1/2 ml-1.5 -translate-y-1/2'
          )}
        >
          {text}
        </span>
      )}
    </span>
  );
}

export function HelpLabel({
  children,
  tip,
  as = 'span'
}: {
  children: React.ReactNode;
  tip: string;
  as?: 'span' | 'h1' | 'h2' | 'label';
}) {
  const Tag = as;
  return (
    <Tag className="inline-flex items-center gap-0.5">
      {children}
      <HelpTip text={tip} />
    </Tag>
  );
}
