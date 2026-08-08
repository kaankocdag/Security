'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';

export interface Settings {
  id: string;
  defaultBugBountyProgramId?: string | null;
  openReportUrlTemplate: string;
  minReadinessScoreForSubmit: number;
  preferEnglishReports: boolean;
  apiEnabled: boolean;
  hasApiToken: boolean;
  hasApiTokenIdentifier?: boolean;
  apiTokenIdentifierHint?: string | null;
}

export interface ScanProfile {
  id: string;
  profileKey: string;
  displayName: string;
  userAgentConfigKey: string;
  rateLimitPerMinuteConfigKey: string;
  isEnabled: boolean;
  resolvedUserAgent?: string;
  resolvedRateLimitPerMinute?: number;
}

export function SettingsClient({
  initialSettings,
  profiles
}: {
  initialSettings: Settings | null;
  profiles: ScanProfile[];
}) {
  const router = useRouter();
  const [settings, setSettings] = useState(initialSettings);
  const [token, setToken] = useState('');
  const [identifier, setIdentifier] = useState('');
  const [msg, setMsg] = useState<string | null>(null);
  const list = profiles || [];

  async function saveSettings() {
    if (!settings) return;
    const res = await fetch('/api/backend/api/hackerone/settings', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({
        openReportUrlTemplate: settings.openReportUrlTemplate,
        minReadinessScoreForSubmit: settings.minReadinessScoreForSubmit,
        preferEnglishReports: settings.preferEnglishReports
      })
    });
    if (res.ok) {
      setSettings(await res.json());
      setMsg('Ayarlar kaydedildi');
      router.refresh();
    } else {
      setMsg('Kayıt başarısız');
    }
  }

  async function saveToken() {
    if (!identifier.trim()) {
      setMsg('HackerOne kullanıcı adın (handle) zorunlu — e-posta değil, profilindeki username.');
      return;
    }
    if (!token.trim()) {
      setMsg('API token değeri zorunlu.');
      return;
    }
    const res = await fetch('/api/backend/api/hackerone/settings/api-token', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify({ apiToken: token.trim(), apiUsername: identifier.trim() })
    });
    if (res.ok) {
      setToken('');
      setIdentifier('');
      setMsg('Kullanıcı adı + token şifreli kaydedildi. Auth: username:token (Basic).');
      router.refresh();
    } else {
      const data = await res.json().catch(() => ({}));
      setMsg(data.detail || 'Token kaydı başarısız');
    }
  }

  async function clearToken() {
    await fetch('/api/backend/api/hackerone/settings/api-token', { method: 'DELETE' });
    setMsg('Token silindi');
    router.refresh();
  }

  if (!settings) {
    return <p className="text-sm text-amber-700">Settings yüklenemedi.</p>;
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div className="rounded-lg border border-slate-200 bg-white/80 p-4 space-y-3">
        <div className="text-sm font-semibold text-slate-900">Workspace</div>
        <p className="text-xs text-slate-500">
          Config <code>HackerOne:ApiEnabled</code> = {settings.apiEnabled ? 'true' : 'false'}. Token
          olsa bile API kapalıysa sync/submit reddedilir. Copy Full Report / Open HackerOne API’siz
          çalışır.
        </p>
        <label className="block text-xs text-slate-600">
          Open URL template
          <input
            className="mt-1 w-full rounded border px-2 py-1.5 text-sm"
            value={settings.openReportUrlTemplate}
            onChange={(e) => setSettings({ ...settings, openReportUrlTemplate: e.target.value })}
          />
        </label>
        <label className="block text-xs text-slate-600">
          Min readiness for submit
          <input
            type="number"
            className="mt-1 w-full rounded border px-2 py-1.5 text-sm"
            value={settings.minReadinessScoreForSubmit}
            onChange={(e) =>
              setSettings({ ...settings, minReadinessScoreForSubmit: Number(e.target.value) })
            }
          />
        </label>
        <button
          type="button"
          onClick={saveSettings}
          className="rounded-md bg-[color:var(--color-brand-600)] px-3 py-1.5 text-sm text-white"
        >
          Kaydet
        </button>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white/80 p-4 space-y-3">
        <div className="text-sm font-semibold text-slate-900">HackerOne API kimlik bilgileri</div>
        <p className="text-xs text-slate-600">
          Kişisel (hacker) token oluştururken <strong>isim sorulmaz</strong>. Basic Auth şöyle çalışır:
        </p>
        <ol className="list-decimal space-y-1 pl-4 text-xs text-slate-600">
          <li>
            HackerOne →{' '}
            <a
              className="underline"
              href="https://hackerone.com/settings/api_token/edit"
              target="_blank"
              rel="noreferrer"
            >
              Settings → API Token
            </a>{' '}
            → Generate API Token
          </li>
          <li>
            <strong>Kullanıcı adı</strong> = HackerOne <em>username / handle</em>’ın (profildeki ad; e-posta
            değil). Token sayfasında ayrı bir “identifier” yazmana gerek yok.
          </li>
          <li>
            <strong>Şifre</strong> = bir kez gösterilen API token değeri
          </li>
        </ol>
        <p className="text-xs text-slate-500">
          Auth: <code>-u &quot;HACKERONE_USERNAME:API_TOKEN&quot;</code>. Dokümantasyon:{' '}
          <a
            className="underline"
            href="https://docs.hackerone.com/en/articles/8410331-api-token"
            target="_blank"
            rel="noreferrer"
          >
            Hacker API Token
          </a>
        </p>
        <p className="text-xs text-slate-500">
          Durum:{' '}
          {settings.hasApiToken
            ? `token kayıtlı (şifreli)${
                settings.hasApiTokenIdentifier
                  ? `, kullanıcı adı: ${settings.apiTokenIdentifierHint ?? '***'}`
                  : ' — kullanıcı adı eksik, yeniden kaydedin'
              }`
            : 'token yok'}
        </p>
        <label className="block text-xs font-medium text-slate-700">
          HackerOne kullanıcı adı (handle) *
          <input
            className="mt-1 w-full rounded border px-2 py-1.5 text-sm font-normal"
            placeholder="örn. kaankaan — e-posta değil"
            value={identifier}
            onChange={(e) => setIdentifier(e.target.value)}
            autoComplete="off"
          />
        </label>
        <label className="block text-xs font-medium text-slate-700">
          API token değeri *
          <input
            type="password"
            className="mt-1 w-full rounded border px-2 py-1.5 text-sm font-normal"
            placeholder="HackerOne’ın bir kez gösterdiği token"
            value={token}
            onChange={(e) => setToken(e.target.value)}
            autoComplete="new-password"
          />
        </label>
        <div className="flex flex-wrap gap-2">
          <button type="button" onClick={saveToken} className="rounded-md border px-3 py-1.5 text-sm">
            Kullanıcı adı + token kaydet
          </button>
          <button type="button" onClick={clearToken} className="rounded-md border px-3 py-1.5 text-sm">
            Token sil
          </button>
        </div>
        {!settings.apiEnabled && (
          <p className="rounded border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-900">
            Şu an <code>HackerOne:ApiEnabled=false</code>. Token kaydı yapılabilir ama program sync /
            API submit çalışmaz. Açmak için Api <code>appsettings</code> içinde{' '}
            <code>HackerOne:ApiEnabled</code> = <code>true</code> yapıp Api’yi yeniden başlatın.
          </p>
        )}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white/80 p-4">
        <div className="text-sm font-semibold text-slate-900">Scan profiles</div>
        <ul className="mt-2 space-y-2 text-xs text-slate-600">
          {list.map((p) => (
            <li key={p.id}>
              <strong>{p.displayName}</strong> ({p.profileKey})
              <div>
                UA key: {p.userAgentConfigKey} → {p.resolvedUserAgent}
              </div>
              <div>
                Rate key: {p.rateLimitPerMinuteConfigKey} → {p.resolvedRateLimitPerMinute}/min
              </div>
            </li>
          ))}
        </ul>
      </div>

      {msg && <p className="text-sm text-slate-600">{msg}</p>}
    </div>
  );
}
