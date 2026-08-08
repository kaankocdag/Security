'use client';

import { useState, useTransition } from 'react';
import Link from 'next/link';
import { loginAction } from '@/lib/auth-actions';

export default function LoginPage() {
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const handleSubmit = (formData: FormData) => {
    setError(null);
    startTransition(async () => {
      const result = await loginAction(formData);
      if (result?.error) setError(result.error);
    });
  };

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <h1 className="text-xl font-bold text-slate-900">Giriş yap</h1>
      <p className="mt-1 text-sm text-slate-600">
        Hesabınız onaylı değilse yalnızca profilinizi görüntüleyebilirsiniz.
      </p>
      <form action={handleSubmit} className="mt-6 space-y-4">
        <Field label="E-posta" name="email" type="email" required autoComplete="email" />
        <Field
          label="Şifre"
          name="password"
          type="password"
          required
          autoComplete="current-password"
        />
        {error && (
          <div className="rounded-md border border-rose-200 bg-rose-50 p-2 text-xs text-rose-700">
            {error}
          </div>
        )}
        <button
          type="submit"
          disabled={isPending}
          className="w-full rounded-lg bg-[color:var(--color-brand-600)] px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-[color:var(--color-brand-700)] disabled:opacity-50"
        >
          {isPending ? 'Giriş yapılıyor...' : 'Giriş yap'}
        </button>
      </form>
      <p className="mt-4 text-sm text-slate-600">
        Hesabın yok mu?{' '}
        <Link
          href="/register"
          className="font-semibold text-[color:var(--color-brand-700)] hover:underline"
        >
          Üye ol
        </Link>
      </p>
    </div>
  );
}

function Field({
  label,
  name,
  type,
  required,
  autoComplete
}: {
  label: string;
  name: string;
  type: string;
  required?: boolean;
  autoComplete?: string;
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-medium text-slate-700">{label}</span>
      <input
        name={name}
        type={type}
        required={required}
        autoComplete={autoComplete}
        className="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-[color:var(--color-brand-600)] focus:outline-none focus:ring-1 focus:ring-[color:var(--color-brand-600)]"
      />
    </label>
  );
}
