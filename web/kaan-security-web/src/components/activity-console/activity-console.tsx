'use client';

import { useEffect, useMemo, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { config } from '@/lib/config';
import { cn } from '@/lib/utils';
import { X, Minus, Maximize2, Move } from 'lucide-react';

interface ActivityEvent {
  id: string;
  eventName: string;
  scope: 'personal' | 'company' | 'system';
  timestamp: string;
  payload: Record<string, unknown>;
}

interface Props {
  userId: string;
  companyId?: string | null;
  isSystemAdmin: boolean;
}

const STORAGE_KEY = 'ksp:activity-console:v1';

interface ConsoleState {
  x: number;
  y: number;
  minimized: boolean;
  hidden: boolean;
  activeTab: 'personal' | 'system';
}

const defaultState: ConsoleState = {
  x: -1,
  y: -1,
  minimized: false,
  hidden: false,
  activeTab: 'personal'
};

export function ActivityConsole({ userId, companyId, isSystemAdmin }: Props) {
  const [state, setState] = useState<ConsoleState>(defaultState);
  const [personalEvents, setPersonalEvents] = useState<ActivityEvent[]>([]);
  const [systemEvents, setSystemEvents] = useState<ActivityEvent[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<'idle' | 'connecting' | 'connected' | 'error'>('idle');
  const dragStart = useRef<{ x: number; y: number; ox: number; oy: number } | null>(null);

  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as Partial<ConsoleState>;
        setState((s) => ({ ...s, ...parsed }));
      }
    } catch {
      // ignore
    }
  }, []);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }, [state]);

  useEffect(() => {
    setConnectionStatus('connecting');
    const url = `${config.apiBaseUrl.replace(/\/$/, '')}/hubs/activity`;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: async () => {
          const res = await fetch('/api/session/hub-token', { cache: 'no-store' });
          if (!res.ok) return '';
          const data = await res.json();
          return data.token as string;
        },
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    const push = (scope: ActivityEvent['scope'], eventName: string, payload: unknown) => {
      const evt: ActivityEvent = {
        id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
        eventName,
        scope,
        timestamp: new Date().toISOString(),
        payload: (payload ?? {}) as Record<string, unknown>
      };
      if (scope === 'system') {
        setSystemEvents((prev) => [evt, ...prev].slice(0, 200));
      } else {
        setPersonalEvents((prev) => [evt, ...prev].slice(0, 200));
      }
    };

    const eventNames = [
      'scan.queued',
      'scan.progress',
      'scan.completed',
      'finding.created',
      'membership.requested',
      'domain.verified',
      'worker.heartbeat'
    ];

    eventNames.forEach((name) => {
      connection.on(name, (payload) => {
        const isSystemOnly = name === 'membership.requested' || name === 'worker.heartbeat';
        if (isSystemOnly || (isSystemAdmin && name.startsWith('scan.'))) {
          push('system', name, payload);
        }
        if (!isSystemOnly) {
          push('personal', name, payload);
        }
      });
    });

    connection
      .start()
      .then(() => setConnectionStatus('connected'))
      .catch(() => setConnectionStatus('error'));

    return () => {
      connection.stop().catch(() => undefined);
    };
  }, []);

  const handleMouseDown = (e: React.MouseEvent) => {
    dragStart.current = {
      x: e.clientX,
      y: e.clientY,
      ox: state.x < 0 ? window.innerWidth - 380 : state.x,
      oy: state.y < 0 ? window.innerHeight - 260 : state.y
    };
    document.body.style.userSelect = 'none';
    window.addEventListener('mousemove', handleMouseMove);
    window.addEventListener('mouseup', handleMouseUp);
  };

  const handleMouseMove = (e: MouseEvent) => {
    if (!dragStart.current) return;
    const dx = e.clientX - dragStart.current.x;
    const dy = e.clientY - dragStart.current.y;
    const nx = Math.max(4, Math.min(window.innerWidth - 60, dragStart.current.ox + dx));
    const ny = Math.max(4, Math.min(window.innerHeight - 60, dragStart.current.oy + dy));
    setState((s) => ({ ...s, x: nx, y: ny }));
  };

  const handleMouseUp = () => {
    dragStart.current = null;
    document.body.style.userSelect = '';
    window.removeEventListener('mousemove', handleMouseMove);
    window.removeEventListener('mouseup', handleMouseUp);
  };

  const displayed = useMemo(
    () => (state.activeTab === 'system' ? systemEvents : personalEvents),
    [state.activeTab, personalEvents, systemEvents]
  );

  if (state.hidden) {
    return (
      <button
        onClick={() => setState((s) => ({ ...s, hidden: false }))}
        className="fixed bottom-4 right-4 z-40 rounded-full bg-slate-900 px-4 py-2 text-xs font-semibold text-white shadow-lg hover:bg-slate-800"
      >
        Konsolu aç
      </button>
    );
  }

  const style: React.CSSProperties = {
    position: 'fixed',
    zIndex: 50,
    width: state.minimized ? 220 : 380,
    right: state.x < 0 ? 16 : undefined,
    bottom: state.y < 0 ? 16 : undefined,
    left: state.x >= 0 ? state.x : undefined,
    top: state.y >= 0 ? state.y : undefined
  };

  return (
    <div style={style} className="rounded-xl border border-slate-200 bg-white/95 shadow-xl backdrop-blur">
      <div
        onMouseDown={handleMouseDown}
        className="flex cursor-move items-center justify-between rounded-t-xl border-b border-slate-200 bg-slate-900 px-3 py-2 text-white"
      >
        <div className="flex items-center gap-2 text-xs font-semibold">
          <Move size={14} className="opacity-70" />
          <span>Canlı Konsol</span>
          <span
            className={cn(
              'ml-2 h-2 w-2 rounded-full',
              connectionStatus === 'connected'
                ? 'bg-emerald-400'
                : connectionStatus === 'connecting'
                  ? 'bg-amber-400'
                  : 'bg-rose-400'
            )}
          />
        </div>
        <div className="flex items-center gap-1">
          <button
            title={state.minimized ? 'Büyüt' : 'Küçült'}
            onClick={() => setState((s) => ({ ...s, minimized: !s.minimized }))}
            className="rounded p-1 hover:bg-white/10"
          >
            {state.minimized ? <Maximize2 size={14} /> : <Minus size={14} />}
          </button>
          <button
            title="Kapat"
            onClick={() => setState((s) => ({ ...s, hidden: true }))}
            className="rounded p-1 hover:bg-white/10"
          >
            <X size={14} />
          </button>
        </div>
      </div>
      {!state.minimized && (
        <>
          <div className="flex border-b border-slate-200 bg-slate-50 text-xs">
            <TabButton
              active={state.activeTab === 'personal'}
              onClick={() => setState((s) => ({ ...s, activeTab: 'personal' }))}
              label="Aktivitem"
              count={personalEvents.length}
            />
            {isSystemAdmin && (
              <TabButton
                active={state.activeTab === 'system'}
                onClick={() => setState((s) => ({ ...s, activeTab: 'system' }))}
                label="Sistem"
                count={systemEvents.length}
              />
            )}
          </div>
          <div className="max-h-64 overflow-y-auto p-2 text-xs">
            {displayed.length === 0 ? (
              <div className="p-6 text-center text-slate-400">Henüz olay yok.</div>
            ) : (
              displayed.map((e) => (
                <div
                  key={e.id}
                  className="mb-1 rounded-md border border-slate-100 bg-white p-2 shadow-sm"
                >
                  <div className="flex items-center justify-between">
                    <span className="font-semibold text-slate-800">{prettify(e.eventName)}</span>
                    <span className="text-[10px] text-slate-500">
                      {new Date(e.timestamp).toLocaleTimeString('tr-TR')}
                    </span>
                  </div>
                  <div className="mt-1 truncate text-[11px] text-slate-500">
                    {JSON.stringify(e.payload)}
                  </div>
                </div>
              ))
            )}
          </div>
          <div className="border-t border-slate-100 px-3 py-1.5 text-[10px] text-slate-500">
            Kullanıcı: <span className="font-mono">{userId.slice(0, 8)}</span>
            {companyId ? ' · Firma: ' + companyId.slice(0, 8) : ''}
          </div>
        </>
      )}
    </div>
  );
}

function TabButton({
  active,
  onClick,
  label,
  count
}: {
  active: boolean;
  onClick: () => void;
  label: string;
  count: number;
}) {
  return (
    <button
      onClick={onClick}
      className={cn(
        'flex-1 px-3 py-2 text-xs font-semibold transition',
        active ? 'bg-white text-slate-900 shadow-inner' : 'text-slate-500 hover:text-slate-900'
      )}
    >
      {label}
      <span className="ml-1 rounded-full bg-slate-200 px-1.5 py-0.5 text-[10px] text-slate-700">
        {count}
      </span>
    </button>
  );
}

function prettify(name: string): string {
  const mapping: Record<string, string> = {
    'scan.queued': 'Tarama kuyruğa alındı',
    'scan.progress': 'Tarama ilerliyor',
    'scan.completed': 'Tarama tamamlandı',
    'finding.created': 'Yeni bulgu',
    'membership.requested': 'Yeni üyelik başvurusu',
    'domain.verified': 'Domain doğrulandı',
    'worker.heartbeat': 'Worker heartbeat'
  };
  return mapping[name] ?? name;
}
