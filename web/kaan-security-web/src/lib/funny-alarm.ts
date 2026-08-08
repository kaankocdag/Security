/** Tarama bulgu alarmı — Web Audio ile komik siren (harici dosya yok). */

let sharedCtx: AudioContext | null = null;

function getCtx(): AudioContext | null {
  if (typeof window === 'undefined') return null;
  const AC = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
  if (!AC) return null;
  if (!sharedCtx) sharedCtx = new AC();
  return sharedCtx;
}

function tone(
  ctx: AudioContext,
  freq: number,
  start: number,
  dur: number,
  type: OscillatorType = 'square',
  gain = 0.12
) {
  const osc = ctx.createOscillator();
  const g = ctx.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(freq, start);
  g.gain.setValueAtTime(0.0001, start);
  g.gain.exponentialRampToValueAtTime(gain, start + 0.02);
  g.gain.exponentialRampToValueAtTime(0.0001, start + dur);
  osc.connect(g);
  g.connect(ctx.destination);
  osc.start(start);
  osc.stop(start + dur + 0.02);
}

/** Tarayıcı autoplay kilidini kullanıcı tıklamasında açar (sessiz). */
export async function unlockAudio() {
  const ctx = getCtx();
  if (!ctx) return;
  if (ctx.state === 'suspended') {
    try {
      await ctx.resume();
    } catch {
      /* ignore */
    }
  }
}

/** Komik “weee-ooo / bip-bip” alarm — bulgu çıkınca. */
export async function playFunnyFindingAlarm() {
  const ctx = getCtx();
  if (!ctx) return;
  if (ctx.state === 'suspended') {
    try {
      await ctx.resume();
    } catch {
      return;
    }
  }

  const t0 = ctx.currentTime + 0.02;
  // Siren sweep
  const osc = ctx.createOscillator();
  const g = ctx.createGain();
  osc.type = 'sawtooth';
  osc.frequency.setValueAtTime(420, t0);
  osc.frequency.linearRampToValueAtTime(880, t0 + 0.22);
  osc.frequency.linearRampToValueAtTime(420, t0 + 0.44);
  osc.frequency.linearRampToValueAtTime(980, t0 + 0.66);
  osc.frequency.linearRampToValueAtTime(380, t0 + 0.9);
  g.gain.setValueAtTime(0.0001, t0);
  g.gain.exponentialRampToValueAtTime(0.14, t0 + 0.05);
  g.gain.exponentialRampToValueAtTime(0.0001, t0 + 1.0);
  osc.connect(g);
  g.connect(ctx.destination);
  osc.start(t0);
  osc.stop(t0 + 1.05);

  // Cartoon “honk” + bips
  tone(ctx, 180, t0 + 0.95, 0.18, 'triangle', 0.16);
  tone(ctx, 720, t0 + 1.15, 0.08, 'square', 0.1);
  tone(ctx, 540, t0 + 1.28, 0.08, 'square', 0.1);
  tone(ctx, 900, t0 + 1.42, 0.12, 'square', 0.12);
  tone(ctx, 220, t0 + 1.55, 0.25, 'sawtooth', 0.1);
}

/** Para / bounty potansiyeli — farklı “cash register / jackpot” sesi. */
export async function playMoneyBountyAlarm() {
  const ctx = getCtx();
  if (!ctx) return;
  if (ctx.state === 'suspended') {
    try {
      await ctx.resume();
    } catch {
      return;
    }
  }

  const t0 = ctx.currentTime + 0.02;
  // Rising arpeggio (jackpot)
  const notes = [523.25, 659.25, 783.99, 1046.5, 1318.5];
  notes.forEach((freq, i) => {
    tone(ctx, freq, t0 + i * 0.09, 0.2, 'triangle', 0.14);
  });
  // Cash “cha-ching”
  tone(ctx, 1400, t0 + 0.55, 0.08, 'square', 0.1);
  tone(ctx, 1800, t0 + 0.65, 0.12, 'square', 0.12);
  tone(ctx, 900, t0 + 0.8, 0.25, 'triangle', 0.15);
  // Low rumble
  tone(ctx, 110, t0 + 0.9, 0.35, 'sawtooth', 0.08);
}
