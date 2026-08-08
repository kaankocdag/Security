'use client';

import { useState, useTransition } from 'react';

interface Category {
  id: string;
  slug: string;
  name: string;
  description?: string;
  publishedArticleCount: number;
}

interface Article {
  id: string;
  slug: string;
  title: string;
  summary: string;
  categoryName: string;
  isPublished: boolean;
}

export function AdminKnowledgeClient({
  initialCategories,
  initialArticles
}: {
  initialCategories: Category[];
  initialArticles: Article[];
}) {
  const [categories, setCategories] = useState(initialCategories);
  const [articles, setArticles] = useState(initialArticles);
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);

  const [catSlug, setCatSlug] = useState('');
  const [catName, setCatName] = useState('');

  const [articleSlug, setArticleSlug] = useState('');
  const [articleTitle, setArticleTitle] = useState('');
  const [articleSummary, setArticleSummary] = useState('');
  const [articleBody, setArticleBody] = useState('');
  const [articleCategory, setArticleCategory] = useState('');
  const [articleTags, setArticleTags] = useState('');
  const [articlePublished, setArticlePublished] = useState(true);

  const [mediaArticleId, setMediaArticleId] = useState<string>('');
  const [mediaCaption, setMediaCaption] = useState('');

  const call = async (path: string, method: string, body?: unknown) => {
    setError(null);
    const res = await fetch(`/api/backend/${path}`, {
      method,
      headers: body ? { 'Content-Type': 'application/json' } : undefined,
      body: body ? JSON.stringify(body) : undefined
    });
    if (!res.ok) {
      const problem = await res.json().catch(() => undefined);
      setError(problem?.detail ?? 'İşlem başarısız.');
      throw new Error('failed');
    }
    return res.status === 204 ? null : await res.json();
  };

  const addCategory = () =>
    startTransition(async () => {
      try {
        const created = await call('api/admin/knowledge/categories', 'POST', {
          slug: catSlug,
          name: catName,
          description: null,
          iconName: null,
          parentCategoryId: null,
          displayOrder: 0
        });
        setCategories((c) => [...c, created as Category]);
        setCatSlug('');
        setCatName('');
      } catch {
        // handled
      }
    });

  const addArticle = () =>
    startTransition(async () => {
      try {
        const created = await call('api/admin/knowledge/articles', 'POST', {
          slug: articleSlug,
          title: articleTitle,
          summary: articleSummary,
          bodyMarkdown: articleBody,
          categoryId: articleCategory,
          cweCode: null,
          owaspCategory: null,
          cveCode: null,
          difficultyLevel: 'Beginner',
          estimatedReadMinutes: Math.max(1, Math.round(articleBody.length / 800)),
          tags: articleTags,
          sourceAttribution: null,
          sourceUrl: null,
          isPublished: articlePublished,
          isFeatured: false
        });
        setArticles((a) => [
          ...a,
          {
            id: (created as { id: string; slug: string; title: string; summary: string }).id,
            slug: articleSlug,
            title: articleTitle,
            summary: articleSummary,
            categoryName:
              categories.find((c) => c.id === articleCategory)?.name ?? '—',
            isPublished: articlePublished
          }
        ]);
        setArticleSlug('');
        setArticleTitle('');
        setArticleSummary('');
        setArticleBody('');
        setArticleTags('');
      } catch {
        // handled
      }
    });

  const uploadMedia = async (file: File) => {
    if (!mediaArticleId) {
      setError('Önce bir makale seçin.');
      return;
    }
    setError(null);
    const form = new FormData();
    form.append('file', file);
    if (mediaCaption) form.append('caption', mediaCaption);
    const res = await fetch(
      `/api/backend/api/admin/knowledge/articles/${mediaArticleId}/media`,
      { method: 'POST', body: form }
    );
    if (!res.ok) {
      const problem = await res.json().catch(() => undefined);
      setError(problem?.detail ?? 'Yükleme başarısız.');
      return;
    }
    setMediaCaption('');
    alert('Görsel yüklendi.');
  };

  return (
    <div className="space-y-6">
      {error && (
        <div className="rounded-md border border-rose-200 bg-rose-50 p-2 text-xs text-rose-700">
          {error}
        </div>
      )}

      <section className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <h2 className="text-sm font-semibold text-slate-800">Kategori Ekle</h2>
        <div className="mt-3 grid grid-cols-1 gap-2 md:grid-cols-3">
          <input
            placeholder="slug"
            value={catSlug}
            onChange={(e) => setCatSlug(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm"
          />
          <input
            placeholder="Ad"
            value={catName}
            onChange={(e) => setCatName(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm"
          />
          <button
            disabled={pending || !catSlug || !catName}
            onClick={addCategory}
            className="rounded-md bg-[color:var(--color-brand-600)] px-3 py-2 text-sm font-semibold text-white hover:bg-[color:var(--color-brand-700)] disabled:opacity-50"
          >
            Ekle
          </button>
        </div>
        <ul className="mt-3 space-y-1 text-sm">
          {categories.map((c) => (
            <li key={c.id} className="rounded-md border border-slate-100 bg-slate-50 px-3 py-2">
              <span className="font-semibold">{c.name}</span>{' '}
              <span className="text-xs text-slate-500">/{c.slug}</span>
              <span className="ml-2 text-xs text-slate-500">
                · {c.publishedArticleCount} yayında
              </span>
            </li>
          ))}
        </ul>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <h2 className="text-sm font-semibold text-slate-800">Makale Ekle</h2>
        <div className="mt-3 grid grid-cols-1 gap-2 md:grid-cols-2">
          <input
            placeholder="slug"
            value={articleSlug}
            onChange={(e) => setArticleSlug(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm"
          />
          <input
            placeholder="Başlık"
            value={articleTitle}
            onChange={(e) => setArticleTitle(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm"
          />
          <select
            value={articleCategory}
            onChange={(e) => setArticleCategory(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm md:col-span-2"
          >
            <option value="">Kategori seçin</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
          <input
            placeholder="Özet"
            value={articleSummary}
            onChange={(e) => setArticleSummary(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm md:col-span-2"
          />
          <textarea
            placeholder="Gövde (Markdown)"
            value={articleBody}
            onChange={(e) => setArticleBody(e.target.value)}
            rows={6}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm md:col-span-2"
          />
          <input
            placeholder="Etiketler (virgülle)"
            value={articleTags}
            onChange={(e) => setArticleTags(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm md:col-span-2"
          />
          <label className="col-span-2 flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={articlePublished}
              onChange={(e) => setArticlePublished(e.target.checked)}
            />
            Hemen yayınla
          </label>
          <button
            disabled={pending || !articleSlug || !articleTitle || !articleCategory}
            onClick={addArticle}
            className="rounded-md bg-[color:var(--color-brand-600)] px-3 py-2 text-sm font-semibold text-white hover:bg-[color:var(--color-brand-700)] disabled:opacity-50 md:col-span-2"
          >
            Makaleyi ekle
          </button>
        </div>
        <ul className="mt-3 space-y-1 text-sm">
          {articles.map((a) => (
            <li key={a.id} className="rounded-md border border-slate-100 bg-slate-50 px-3 py-2">
              <span className="font-semibold">{a.title}</span>{' '}
              <span className="text-xs text-slate-500">/{a.slug}</span>{' '}
              <span
                className={`ml-2 rounded-full px-2 py-0.5 text-[11px] ${
                  a.isPublished
                    ? 'bg-emerald-100 text-emerald-800'
                    : 'bg-slate-200 text-slate-700'
                }`}
              >
                {a.isPublished ? 'Yayında' : 'Taslak'}
              </span>
              <span className="ml-2 text-xs text-slate-500">· {a.categoryName}</span>
            </li>
          ))}
        </ul>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <h2 className="text-sm font-semibold text-slate-800">Görsel Yükle</h2>
        <p className="mt-1 text-xs text-slate-500">
          Instagram görselleri ve diğer medyalar (JPEG, PNG, WEBP, GIF – 20 MB üst sınır) buradan
          yüklenir.
        </p>
        <div className="mt-3 grid grid-cols-1 gap-2 md:grid-cols-3">
          <select
            value={mediaArticleId}
            onChange={(e) => setMediaArticleId(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm md:col-span-2"
          >
            <option value="">Makale seçin</option>
            {articles.map((a) => (
              <option key={a.id} value={a.id}>
                {a.title}
              </option>
            ))}
          </select>
          <input
            placeholder="Açıklama (opsiyonel)"
            value={mediaCaption}
            onChange={(e) => setMediaCaption(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-2 text-sm"
          />
          <input
            type="file"
            accept="image/*"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) uploadMedia(file);
            }}
            className="text-sm md:col-span-3"
          />
        </div>
      </section>
    </div>
  );
}
