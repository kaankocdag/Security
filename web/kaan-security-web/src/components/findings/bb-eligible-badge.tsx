'use client';

import { useEffect } from 'react';

/** Yeşil BB aday rozeti ($ / para kazanma sinyali). */
export function BbEligibleBadge({
  eligible,
  compact = false,
  className = ''
}: {
  eligible?: boolean | null;
  compact?: boolean;
  className?: string;
}) {
  if (!eligible) return null;

  if (compact) {
    return (
      <span
        title="Bug Bounty adayı — para kazandırabilir"
        className={`inline-flex items-center gap-0.5 rounded-full border border-emerald-400 bg-emerald-500 px-1.5 py-0.5 text-[10px] font-bold text-white shadow-sm ${className}`}
      >
        $ BB
      </span>
    );
  }

  return (
    <div
      className={`flex items-start gap-3 rounded-xl border-2 border-emerald-400 bg-emerald-50 px-4 py-3 text-emerald-950 shadow-sm ${className}`}
      role="status"
    >
      <span
        className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-emerald-500 text-lg font-black text-white"
        aria-hidden
      >
        $
      </span>
      <div>
        <div className="text-sm font-bold tracking-wide text-emerald-800">
          $$$ BUG BOUNTY ADAYI
        </div>
        <p className="mt-0.5 text-xs text-emerald-900/80">
          Demonstrated impact + program politikası uygun. Amazon VRP / HackerOne gönderimi için
          ManualReview veya Submit adayı.
        </p>
      </div>
    </div>
  );
}

/**
 * BB adayı sayfası açılınca kısa yeşil “ching” sinyali (Web Audio).
 * Tarayıcı engellerse sessizce geçer.
 */
export function BbEligibleChime({ play }: { play: boolean }) {
  useEffect(() => {
    if (!play || typeof window === 'undefined') return;

    let ctx: AudioContext | null = null;
    try {
      const AC =
        window.AudioContext ||
        (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
      if (!AC) return;
      ctx = new AC();
      const now = ctx.currentTime;

      const beep = (freq: number, start: number, dur: number, gainValue: number) => {
        const osc = ctx!.createOscillator();
        const gain = ctx!.createGain();
        osc.type = 'sine';
        osc.frequency.value = freq;
        gain.gain.setValueAtTime(0.0001, now + start);
        gain.gain.exponentialRampToValueAtTime(gainValue, now + start + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.0001, now + start + dur);
        osc.connect(gain);
        gain.connect(ctx!.destination);
        osc.start(now + start);
        osc.stop(now + start + dur + 0.02);
      };

      // Kısa “cash register” benzeri iki ton
      beep(880, 0, 0.12, 0.12);
      beep(1175, 0.1, 0.18, 0.1);
    } catch {
      // autoplay / AudioContext kısıtı
    }

    return () => {
      void ctx?.close().catch(() => undefined);
    };
  }, [play]);

  return null;
}
