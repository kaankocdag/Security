'use client';

import { useState, useTransition } from 'react';
import Link from 'next/link';
import { registerAction } from '@/lib/auth-actions';

export default function RegisterPage() {
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const handleSubmit = (formData: FormData) => {
    setError(null);
    setInfo(null);
    startTransition(async () => {
      const result = await registerAction(formData);
      if (result?.error) setError(result.error);
      if (result?.info) setInfo(result.info);
    });
  };

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <h1 className="text-xl font-bold text-slate-900">Firmam için üye ol</h1>
      <p className="mt-1 text-sm text-slate-600">
        Kayıt sonrası hesabınız Kaan Security ekibi tarafından onaylanır.
      </p>
      <form action={handleSubmit} className="mt-6 space-y-4">
        <Field label="Ad soyad" name="fullName" required />
        <Field label="E-posta" name="email" type="email" required />
        <Field label="Şifre (en az 10 karakter)" name="password" type="password" required />
        <Field label="Firma adı" name="companyName" required />
        <Field label="Firma domaini (opsiyonel)" name="companyDomain" placeholder="example.com" />
        {error && (
          <div className="rounded-md border border-rose-200 bg-rose-50 p-2 text-xs text-rose-700">
            {error}
          </div>
        )}
        {info && (
          <div className="rounded-md border border-emerald-200 bg-emerald-50 p-2 text-xs text-emerald-800">
            {info}
          </div>
        )}
        <button
          type="submit"
          disabled={isPending}
          className="w-full rounded-lg bg-[color:var(--color-brand-600)] px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-[color:var(--color-brand-700)] disabled:opacity-50"
        >
          {isPending ? 'Kaydediliyor...' : 'Başvuruyu gönder'}
        </button>
      </form>
      <p className="mt-4 text-sm text-slate-600">
        Zaten üye misin?{' '}
        <Link
          href="/login"
          className="font-semibold text-[color:var(--color-brand-700)] hover:underline"
        >
          Giriş yap
        </Link>
      </p>
    </div>
  );
}

function Field({
  label,
  name,
  type = 'text',
  required,
  placeholder
}: {
  label: string;
  name: string;
  type?: string;
  required?: boolean;
  placeholder?: string;
}) {
  return (
    <label className="block text-sm">
      <span className="mb-1 block font-medium text-slate-700">{label}</span>
      <input
        name={name}
        type={type}
        required={required}
        placeholder={placeholder}
        className="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-[color:var(--color-brand-600)] focus:outline-none focus:ring-1 focus:ring-[color:var(--color-brand-600)]"
      />
    </label>
  );
}
