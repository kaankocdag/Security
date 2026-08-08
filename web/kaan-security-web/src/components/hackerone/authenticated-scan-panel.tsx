'use client';

import { useCallback, useEffect, useState } from 'react';
import { apiFetch } from '@/lib/api';

type Tab = 'overview' | 'anonymous' | 'authenticated' | 'compare' | 'accounts' | 'audit';

interface Preconditions {
  targetId: string;
  hasScopePolicy: boolean;
  hasAuthorizationEvidence: boolean;
  targetInBountyScope: boolean;
  autoRegistrationAllowed: boolean;
  activeTestAccountCount: number;
  maxTestAccounts: number;
  missingItems: string[];
  disclaimer: string;
}

interface TestAccount {
  id: string;
  targetId: string;
  targetDomain: string;
  label: string;
  email?: string | null;
  username?: string | null;
  displayName?: string | null;
  accountStatus: string | number;
  verificationStatus: string | number;
  loginUrl?: string | null;
  lastSuccessfulLoginAt?: string | null;
  lastAuthenticatedScanAt?: string | null;
  isActive: boolean;
  role: string | number;
}

interface Observation {
  isAuthenticatedMode: boolean;
  maskedAccountLabel?: string | null;
  url: string;
  statusCode: number;
  finalUrl?: string | null;
  loginDetected: boolean;
  accessDeniedDetected: boolean;
  authenticationConfirmed: boolean;
  redactedEvidence?: string | null;
  comparisonResult?: string | number | null;
}

interface ScanRun {
  id: string;
  status: string | number;
  takeoverReason?: string | number;
  takeoverMessage?: string | null;
  authenticationConfirmed: boolean;
  loginUrlUsed?: string | null;
  browserSessionHeld?: boolean;
  errorCode?: string | null;
  errorMessage?: string | null;
  stopReason?: string | null;
  anonymousObservations: Observation[];
  authenticatedObservations: Observation[];
  comparisons: Observation[];
}

interface LoginDiscovery {
  bestLoginUrl?: string | null;
  candidateUrls: string[];
  passwordFormDetected: boolean;
  oAuthOnlyLikely: boolean;
  oAuthProviders: string[];
  note: string;
}

function isAwaitingTakeover(run: ScanRun | null) {
  if (!run) return false;
  return run.status === 'AwaitingManualTakeover' || run.status === 3;
}

export function AuthenticatedScanPanel({
  targetId,
  hostName
}: {
  targetId: string;
  hostName: string;
}) {
  const [open, setOpen] = useState(false);
  const [tab, setTab] = useState<Tab>('overview');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pre, setPre] = useState<Preconditions | null>(null);
  const [accounts, setAccounts] = useState<TestAccount[]>([]);
  const [run, setRun] = useState<ScanRun | null>(null);
  const [selectedAccountId, setSelectedAccountId] = useState<string>('');
  const [approved, setApproved] = useState(false);
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [username, setUsername] = useState('');
  const [ownership, setOwnership] = useState(false);
  const [testingPerm, setTestingPerm] = useState(false);
  const [revealed, setRevealed] = useState<string | null>(null);
  const [loginUrlInput, setLoginUrlInput] = useState('');
  const [discovery, setDiscovery] = useState<LoginDiscovery | null>(null);
  const [cookieData, setCookieData] = useState('');

  const load = useCallback(async () => {
    setBusy(true);
    setError(null);
    try {
      const [p, a] = await Promise.all([
        apiFetch<Preconditions>(`/api/authenticated-scanning/targets/${targetId}/preconditions`),
        apiFetch<TestAccount[]>(`/api/authenticated-scanning/targets/${targetId}/accounts`)
      ]);
      setPre(p);
      setAccounts(a);
      if (!selectedAccountId && a[0]) setSelectedAccountId(a[0].id);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Yükleme başarısız');
    } finally {
      setBusy(false);
    }
  }, [targetId, selectedAccountId]);

  useEffect(() => {
    if (open) void load();
  }, [open, load]);

  async function registerExisting() {
    if (!ownership || !testingPerm) {
      setError('OwnershipConfirmed ve TestingPermissionConfirmed zorunlu.');
      return;
    }
    if (!email.includes('@')) {
      setError('Kendi kontrolünüzdeki test e-postasını girin — sistem e-posta uydurmaz.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await apiFetch(`/api/authenticated-scanning/accounts/register-existing`, {
        method: 'POST',
        body: {
          targetId,
          label: 'Security Test Account',
          email,
          username: username || null,
          displayName: 'Security Test',
          password,
          loginUrl: `https://${hostName}/login`,
          role: 0,
          ownershipConfirmed: true,
          testingPermissionConfirmed: true
        }
      });
      setPassword('');
      await load();
      setTab('accounts');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kayıt başarısız');
    } finally {
      setBusy(false);
    }
  }

  async function startAuthScan() {
    if (!approved || !selectedAccountId) {
      setError('Hesap seçin ve girişli tarama onayını verin.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const data = await apiFetch<ScanRun>(`/api/authenticated-scanning/runs/start`, {
        method: 'POST',
        body: {
          targetId,
          testAccountId: selectedAccountId,
          explicitUserApproval: true,
          headedBrowser: true
        }
      });
      setRun(data);
      if (isAwaitingTakeover(data)) {
        setTab('overview');
        if (data.loginUrlUsed && !data.browserSessionHeld) {
          window.open(data.loginUrlUsed, '_blank', 'noopener,noreferrer');
        }
      } else {
        setTab('compare');
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Tarama başlatılamadı');
    } finally {
      setBusy(false);
    }
  }

  async function discoverLogin() {
    setBusy(true);
    setError(null);
    try {
      const data = await apiFetch<LoginDiscovery>(
        `/api/authenticated-scanning/targets/${targetId}/login-discovery`
      );
      setDiscovery(data);
      if (data.bestLoginUrl) setLoginUrlInput(data.bestLoginUrl);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Login sayfası aranamadı');
    } finally {
      setBusy(false);
    }
  }

  async function startCookieSession() {
    if (!approved) {
      setError('Girişli tarama onayını verin.');
      return;
    }
    if (!cookieData.trim()) {
      setError('Oturum çerezini yapıştırın.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const data = await apiFetch<ScanRun>(`/api/authenticated-scanning/runs/start-cookie-session`, {
        method: 'POST',
        body: {
          targetId,
          cookieData,
          explicitUserApproval: true,
          runAnonymousBaseline: true
        }
      });
      setRun(data);
      setCookieData('');
      setTab('compare');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Çerez oturumlu tarama başlatılamadı');
    } finally {
      setBusy(false);
    }
  }

  async function startManualLoginSession() {
    if (!approved) {
      setError('Girişli tarama onayını verin.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const data = await apiFetch<ScanRun>(`/api/authenticated-scanning/runs/start-manual-login`, {
        method: 'POST',
        body: {
          targetId,
          loginUrl: loginUrlInput.trim() || null,
          explicitUserApproval: true,
          runAnonymousBaseline: true
        }
      });
      setRun(data);
      setTab('overview');
      if (data.loginUrlUsed && !data.browserSessionHeld) {
        window.open(data.loginUrlUsed, '_blank', 'noopener,noreferrer');
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Manuel giriş oturumu başlatılamadı');
    } finally {
      setBusy(false);
    }
  }

  async function continueAfterTakeover() {
    if (!run?.id) return;
    setBusy(true);
    setError(null);
    try {
      const data = await apiFetch<ScanRun>(
        `/api/authenticated-scanning/runs/${run.id}/continue-after-takeover`,
        { method: 'POST' }
      );
      setRun(data);
      setTab('compare');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Devam edilemedi');
    } finally {
      setBusy(false);
    }
  }

  function openLoginInBrowser() {
    const url = run?.loginUrlUsed || `https://${hostName}/login`;
    window.open(url, '_blank', 'noopener,noreferrer');
  }

  async function reveal(accountId: string, forCopy: boolean) {
    setBusy(true);
    setError(null);
    try {
      const data = await apiFetch<{ password: string }>(
        `/api/authenticated-scanning/accounts/${accountId}/reveal-password?forCopy=${forCopy}`,
        { method: 'POST' }
      );
      setRevealed(data.password);
      if (forCopy && navigator.clipboard) await navigator.clipboard.writeText(data.password);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Secret görüntülenemedi');
    } finally {
      setBusy(false);
    }
  }

  async function disable(accountId: string) {
    setBusy(true);
    try {
      await apiFetch(`/api/authenticated-scanning/accounts/${accountId}/disable`, { method: 'POST' });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Devre dışı bırakılamadı');
    } finally {
      setBusy(false);
    }
  }

  async function wipeVault(accountId: string) {
    setBusy(true);
    try {
      await apiFetch(`/api/authenticated-scanning/accounts/${accountId}/vault`, { method: 'DELETE' });
      setRevealed(null);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Vault silinemedi');
    } finally {
      setBusy(false);
    }
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="rounded-md border border-indigo-200 bg-indigo-50 px-2 py-1 text-[11px] font-semibold text-indigo-800"
      >
        Girişli Tarama
      </button>
    );
  }

  const tabs: [Tab, string][] = [
    ['overview', 'Genel Bakış'],
    ['anonymous', 'Anonim Tarama'],
    ['authenticated', 'Girişli Tarama'],
    ['compare', 'Karşılaştırma'],
    ['accounts', 'Test Hesapları'],
    ['audit', 'Audit Log']
  ];

  return (
    <div className="mt-2 w-full max-w-3xl rounded-lg border border-indigo-200 bg-indigo-50/40 p-3 text-left text-xs text-slate-800">
      <div className="mb-2 flex items-center justify-between gap-2">
        <div>
          <div className="text-sm font-semibold text-indigo-950">Girişli Tarama / Authenticated Scanning</div>
          <div className="text-[11px] text-slate-600">{hostName}</div>
        </div>
        <button type="button" onClick={() => setOpen(false)} className="rounded border px-2 py-0.5 text-[11px]">
          Kapat
        </button>
      </div>

      {error && <div className="mb-2 rounded border border-rose-200 bg-rose-50 px-2 py-1 text-rose-700">{error}</div>}

      {isAwaitingTakeover(run) && (
        <div className="mb-3 space-y-2 rounded-md border border-amber-400 bg-amber-50 p-3 text-amber-950">
          <div className="font-semibold">Manuel işlem gerekli</div>
          <p>
            {run?.takeoverMessage ||
              "Tarayıcı kontrolü size bırakıldı. İşlemi tamamladıktan sonra ‘Devam Et’ düğmesine basın."}
          </p>
          {run?.loginUrlUsed && (
            <p className="font-mono text-[11px] break-all">Login URL: {run.loginUrlUsed}</p>
          )}
          <p className="text-[11px]">
            {run?.browserSessionHeld
              ? 'Chromium penceresi açık tutuluyor — CAPTCHA/MFA/girişi orada tamamlayın.'
              : 'Otomatik Chromium açılamadı. ‘Tarayıcıda Aç’ ile kendi tarayıcınızda girişi yapın.'}
          </p>
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={openLoginInBrowser}
              className="rounded border border-amber-700 bg-white px-3 py-1.5 font-semibold text-amber-900"
            >
              Tarayıcıda Aç
            </button>
            <button
              type="button"
              disabled={busy}
              onClick={() => void continueAfterTakeover()}
              className="rounded bg-amber-700 px-3 py-1.5 font-semibold text-white disabled:opacity-50"
            >
              Devam Et
            </button>
          </div>
        </div>
      )}

      <div className="mb-2 flex flex-wrap gap-1">
        {tabs.map(([k, label]) => (
          <button
            key={k}
            type="button"
            onClick={() => setTab(k)}
            className={`rounded px-2 py-1 ${tab === k ? 'bg-indigo-700 text-white' : 'bg-white border'}`}
          >
            {label}
          </button>
        ))}
      </div>

      {tab === 'overview' && (
        <div className="space-y-2">
          <p className="text-slate-700">
            Bu alan kimlik doğrulaması gerektiriyor olabilir. Anonim tarama tamamlandıktan sonra kendi yetkili test
            hesabınızla girişli tarama başlatabilirsiniz. Üçüncü kişi hesabı istemeyin / denemeyin.
          </p>
          {pre && (
            <ul className="list-disc pl-4 text-slate-600">
              <li>Scope: {pre.hasScopePolicy ? 'var' : 'yok'}</li>
              <li>Authorization: {pre.hasAuthorizationEvidence ? 'var' : 'yok'}</li>
              <li>
                Aktif test hesabı: {pre.activeTestAccountCount}/{pre.maxTestAccounts}
              </li>
              {pre.missingItems.length > 0 && <li>Eksik: {pre.missingItems.join(', ')}</li>}
            </ul>
          )}
          <p className="text-[11px] text-slate-500">{pre?.disclaimer}</p>
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={approved} onChange={(e) => setApproved(e.target.checked)} />
            Girişli taramayı kendi yetkili test oturumumla başlatmayı onaylıyorum
          </label>

          {/* En kolay yol: kendi tarayıcında giriş yap, oturum çerezini yapıştır */}
          <div className="space-y-2 rounded-md border-2 border-sky-400 bg-sky-50/60 p-3">
            <div className="flex items-center gap-2">
              <span className="rounded bg-sky-700 px-1.5 py-0.5 text-[10px] font-bold text-white">EN KOLAY</span>
              <span className="font-semibold text-sky-900">Oturum Çerezi Yapıştır (Google/SSO dahil her site)</span>
            </div>
            <p className="text-[11px] text-slate-600">
              Otomasyon yok, “güvenli tarayıcı değil” hatası yok. Kendi normal tarayıcında siteye giriş yap, oturum
              çerezini yapıştır — sistem o oturumla güvenli GET taraması yapar. Çerez kaydedilmez, sadece bu tarama
              için bellekte tutulur.
            </p>
            <ol className="list-decimal pl-4 text-[11px] text-slate-600">
              <li>Chrome’a ücretsiz “Cookie-Editor” eklentisini kur.</li>
              <li>
                <span className="font-mono">{hostName}</span> sitesine normal şekilde giriş yap (Google/SSO fark etmez).
              </li>
              <li>Cookie-Editor’ı aç → “Export” → “Export as JSON” (veya Header String) → kopyala.</li>
              <li>Aşağıya yapıştır ve “Çerezle Tara”ya bas.</li>
            </ol>
            <textarea
              className="h-20 w-full rounded border px-2 py-1 font-mono text-[11px]"
              placeholder='Ham başlık: "sessionid=abc; csrftoken=xyz"  —  veya Cookie-Editor JSON: [{"name":"sessionid","value":"abc"}, ...]'
              value={cookieData}
              onChange={(e) => setCookieData(e.target.value)}
            />
            <div className="flex items-center gap-2">
              <button
                type="button"
                disabled={busy || !approved || !cookieData.trim()}
                onClick={() => void startCookieSession()}
                className="rounded bg-sky-700 px-3 py-1 font-semibold text-white disabled:opacity-50"
              >
                Çerezle Tara
              </button>
              {!approved && (
                <span className="text-[11px] text-amber-700">Önce yukarıdaki onay kutusunu işaretleyin.</span>
              )}
            </div>
          </div>

          {/* Alternatif yol: şifre yazmadan, tarayıcı içinden manuel giriş (Google/SSO dahil) */}
          <div className="space-y-2 rounded-md border-2 border-emerald-400 bg-emerald-50/60 p-3">
            <div className="flex items-center gap-2">
              <span className="rounded bg-emerald-700 px-1.5 py-0.5 text-[10px] font-bold text-white">ALTERNATİF</span>
              <span className="font-semibold text-emerald-900">Tarayıcıdan Giriş (Google/SSO bazı sitelerde “güvenli değil” diyebilir)</span>
            </div>
            <p className="text-[11px] text-slate-600">
              Hesap eklemenize gerek yok. Görünür Chromium penceresi açılır; girişi Google/Microsoft/SSO/MFA dahil
              kendiniz yaparsınız. Şifre platforma girilmez, saklanmaz. Giriş bittikten sonra ‘Devam Et’ ile yalnızca
              güvenli GET probe’ları çalışır.
            </p>
            <ol className="list-decimal pl-4 text-[11px] text-slate-600">
              <li>“Login Sayfasını Bul” ile giriş URL’sini otomatik tespit edin (veya elle yazın).</li>
              <li>“Tarayıcıdan Giriş Başlat” → açılan pencerede Google/SSO ile giriş yapın.</li>
              <li>Giriş bitince üstteki “Devam Et” düğmesine basın; tarama oturumla sürer.</li>
            </ol>
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                disabled={busy}
                onClick={() => void discoverLogin()}
                className="rounded border border-emerald-400 bg-white px-2 py-1 font-medium text-emerald-800 disabled:opacity-50"
              >
                Login Sayfasını Bul
              </button>
              <input
                className="min-w-[220px] flex-1 rounded border px-2 py-1 font-mono text-[11px]"
                placeholder={`https://${hostName}/login`}
                value={loginUrlInput}
                onChange={(e) => setLoginUrlInput(e.target.value)}
              />
              <button
                type="button"
                disabled={busy || !approved}
                onClick={() => void startManualLoginSession()}
                className="rounded bg-emerald-700 px-3 py-1 font-semibold text-white disabled:opacity-50"
              >
                Tarayıcıdan Giriş Başlat
              </button>
            </div>
            {!approved && (
              <p className="text-[11px] text-amber-700">Başlatmak için yukarıdaki onay kutusunu işaretleyin.</p>
            )}
            {discovery && (
              <div className="space-y-1 rounded bg-white p-2 text-[11px] text-slate-700">
                <div>{discovery.note}</div>
                {discovery.oAuthProviders.length > 0 && (
                  <div>Dış sağlayıcı: {discovery.oAuthProviders.join(', ')}</div>
                )}
                {discovery.candidateUrls.length > 0 && (
                  <div className="flex flex-wrap gap-1">
                    {discovery.candidateUrls.map((u) => (
                      <button
                        key={u}
                        type="button"
                        onClick={() => setLoginUrlInput(u)}
                        className="rounded border bg-white px-1 py-0.5 font-mono break-all"
                      >
                        {u}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>

          {/* İkincil yol: kayıtlı şifreli test hesabıyla otomatik giriş */}
          <details className="rounded-md border border-slate-300 bg-white p-2">
            <summary className="cursor-pointer font-medium text-slate-700">
              Alternatif: Kayıtlı test hesabıyla otomatik giriş
            </summary>
            <div className="mt-2 space-y-2">
              {accounts.filter((a) => a.isActive).length === 0 ? (
                <p className="text-[11px] text-slate-600">
                  Kayıtlı aktif test hesabı yok.{' '}
                  <button
                    type="button"
                    onClick={() => setTab('accounts')}
                    className="font-semibold text-indigo-700 underline"
                  >
                    “Test Hesapları” sekmesinden
                  </button>{' '}
                  kendi kontrolünüzdeki bir hesabı ekleyin. (Google/SSO ile giriş yapılan sitelerde bu yol
                  kullanılamaz — yukarıdaki “Tarayıcıdan Giriş”i kullanın.)
                </p>
              ) : (
                <div className="flex flex-wrap gap-2">
                  <select
                    value={selectedAccountId}
                    onChange={(e) => setSelectedAccountId(e.target.value)}
                    className="rounded border px-2 py-1"
                  >
                    <option value="">Hesap seç</option>
                    {accounts.filter((a) => a.isActive).map((a) => (
                      <option key={a.id} value={a.id}>
                        {a.label} · {a.email || a.username}
                      </option>
                    ))}
                  </select>
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => void startAuthScan()}
                    className="rounded bg-indigo-700 px-3 py-1 font-semibold text-white disabled:opacity-50"
                  >
                    Girişli Tarama Başlat
                  </button>
                </div>
              )}
              <button
                type="button"
                onClick={() => setTab('accounts')}
                className="rounded border bg-white px-2 py-1 text-[11px]"
              >
                Test Hesabı Ekle / Yönet
              </button>
            </div>
          </details>

          <button type="button" disabled={busy} onClick={() => void load()} className="rounded border bg-white px-2 py-1 text-[11px]">
            Yenile
          </button>
        </div>
      )}

      {tab === 'anonymous' && (
        <ObsTable rows={run?.anonymousObservations ?? []} empty="Anonim gözlem yok — önce girişli tarama başlatın." />
      )}
      {tab === 'authenticated' && (
        <ObsTable
          rows={run?.authenticatedObservations ?? []}
          empty="Girişli gözlem yok."
        />
      )}
      {tab === 'compare' && (
        <div className="space-y-2">
          <p className="text-slate-600">
            Giriş sonrası sayfanın açılması veya üyenin /admin üzerinde 403 alması açık değildir. DemonstratedImpact
            olmadan SubmissionEligible kalmaz.
          </p>
          <ObsTable rows={run?.comparisons ?? []} empty="Karşılaştırma yok." showCompare />
        </div>
      )}

      {tab === 'accounts' && (
        <div className="space-y-3">
          <div className="rounded border bg-white p-2 space-y-1">
            <div className="font-semibold">Mevcut Test Hesabını Tanımla</div>
            <input
              className="w-full rounded border px-2 py-1"
              placeholder="Test e-posta (kendi kontrolünüz)"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
            <input
              className="w-full rounded border px-2 py-1"
              placeholder="Kullanıcı adı (opsiyonel)"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
            <input
              className="w-full rounded border px-2 py-1"
              type="password"
              placeholder="Parola (vault’a şifreli)"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
            <label className="flex items-center gap-2">
              <input type="checkbox" checked={ownership} onChange={(e) => setOwnership(e.target.checked)} />
              OwnershipConfirmed — bu benim güvenlik test hesabım
            </label>
            <label className="flex items-center gap-2">
              <input type="checkbox" checked={testingPerm} onChange={(e) => setTestingPerm(e.target.checked)} />
              TestingPermissionConfirmed — program kapsamında test için kullanacağım
            </label>
            <button
              type="button"
              disabled={busy}
              onClick={() => void registerExisting()}
              className="rounded bg-slate-800 px-2 py-1 text-white disabled:opacity-50"
            >
              Kaydet
            </button>
          </div>

          {accounts.length === 0 ? (
            <p className="text-slate-500">Kayıtlı test hesabı yok.</p>
          ) : (
            <ul className="space-y-2">
              {accounts.map((a) => (
                <li key={a.id} className="rounded border bg-white p-2">
                  <div className="font-semibold">{a.label}</div>
                  <div>
                    {a.email || a.username} · status={String(a.accountStatus)} · verify={String(a.verificationStatus)} ·{' '}
                    {a.isActive ? 'aktif' : 'pasif'}
                  </div>
                  <div className="text-[11px] text-slate-500">
                    Son giriş: {a.lastSuccessfulLoginAt || '—'} · Son girişli tarama: {a.lastAuthenticatedScanAt || '—'}
                  </div>
                  <div className="mt-1 flex flex-wrap gap-1">
                    <button type="button" className="rounded border px-1.5 py-0.5" onClick={() => void reveal(a.id, false)}>
                      Parolayı göster
                    </button>
                    <button type="button" className="rounded border px-1.5 py-0.5" onClick={() => void reveal(a.id, true)}>
                      Kopyala
                    </button>
                    <button type="button" className="rounded border px-1.5 py-0.5" onClick={() => void disable(a.id)}>
                      Devre dışı
                    </button>
                    <button type="button" className="rounded border border-rose-300 px-1.5 py-0.5 text-rose-700" onClick={() => void wipeVault(a.id)}>
                      Vault sil
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}
          {revealed && (
            <div className="rounded border border-amber-300 bg-amber-50 px-2 py-1 font-mono">
              Parola (audit’li): {revealed}
            </div>
          )}
        </div>
      )}

      {tab === 'audit' && (
        <p className="text-slate-600">
          Hesap oluşturma, onay, takeover, giriş, secret görüntüleme ve vault silme işlemleri BugBounty audit log’a
          yazılır. Parola/cookie/token audit’e eklenmez.
        </p>
      )}
    </div>
  );
}

function ObsTable({
  rows,
  empty,
  showCompare
}: {
  rows: Observation[];
  empty: string;
  showCompare?: boolean;
}) {
  if (rows.length === 0) return <p className="text-slate-500">{empty}</p>;
  return (
    <div className="overflow-x-auto rounded border bg-white">
      <table className="w-full text-[11px]">
        <thead className="bg-slate-50 text-left">
          <tr>
            <th className="px-2 py-1">URL</th>
            <th className="px-2 py-1">Status</th>
            <th className="px-2 py-1">Sinyaller</th>
            {showCompare && <th className="px-2 py-1">Karşılaştırma</th>}
          </tr>
        </thead>
        <tbody>
          {rows.map((r, i) => (
            <tr key={`${r.url}-${i}`} className="border-t">
              <td className="px-2 py-1 font-mono">{r.url}</td>
              <td className="px-2 py-1">{r.statusCode}</td>
              <td className="px-2 py-1">
                {r.loginDetected && 'login '}
                {r.accessDeniedDetected && 'denied '}
                {r.authenticationConfirmed && 'authOK '}
                {r.maskedAccountLabel || ''}
              </td>
              {showCompare && <td className="px-2 py-1">{String(r.comparisonResult ?? '—')}</td>}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
