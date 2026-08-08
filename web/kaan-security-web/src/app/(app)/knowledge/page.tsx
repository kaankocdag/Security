import Link from 'next/link';
import { apiFetch } from '@/lib/api';
import { requireSession } from '@/lib/session';

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
  categorySlug: string;
  difficultyLevel: string;
  estimatedReadMinutes: number;
  tags: string[];
  isFeatured: boolean;
  coverMediaUrl?: string | null;
}

export default async function KnowledgePage() {
  const { accessToken } = await requireSession();
  let categories: Category[] = [];
  let articles: Article[] = [];
  try {
    [categories, articles] = await Promise.all([
      apiFetch<Category[]>('/api/knowledge/categories', { accessToken, serverSide: true }),
      apiFetch<Article[]>('/api/knowledge/articles', { accessToken, serverSide: true })
    ]);
  } catch {
    // ignore
  }
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Zafiyet Bilgi Bankası</h1>
        <p className="mt-1 text-sm text-slate-600">
          Yaygın web güvenlik açıkları, örnek zafiyetler ve çözüm rehberleri.
        </p>
      </div>

      <section>
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wider text-slate-500">
          Kategoriler
        </h2>
        <div className="grid gap-3 md:grid-cols-3">
          {categories.length === 0 ? (
            <div className="rounded-md border border-dashed border-slate-200 p-4 text-sm text-slate-500">
              Henüz kategori eklenmemiş.
            </div>
          ) : (
            categories.map((c) => (
              <div
                key={c.id}
                className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm"
              >
                <div className="text-sm font-semibold text-slate-800">{c.name}</div>
                <div className="mt-1 text-xs text-slate-500">
                  {c.publishedArticleCount} makale
                </div>
                {c.description && (
                  <p className="mt-2 text-xs text-slate-600">{c.description}</p>
                )}
              </div>
            ))
          )}
        </div>
      </section>

      <section>
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wider text-slate-500">
          Makaleler
        </h2>
        <div className="grid gap-3 md:grid-cols-2">
          {articles.length === 0 ? (
            <div className="rounded-md border border-dashed border-slate-200 p-4 text-sm text-slate-500">
              Henüz yayınlanmış makale yok.
            </div>
          ) : (
            articles.map((a) => (
              <Link
                key={a.id}
                href={`/knowledge/article/${a.slug}`}
                className="group flex flex-col rounded-2xl border border-slate-200 bg-white p-4 shadow-sm hover:border-[color:var(--color-brand-500)]"
              >
                <div className="text-[11px] uppercase tracking-widest text-[color:var(--color-brand-600)]">
                  {a.categoryName}
                </div>
                <div className="mt-1 text-base font-semibold text-slate-800 group-hover:text-[color:var(--color-brand-700)]">
                  {a.title}
                </div>
                <p className="mt-1 line-clamp-2 text-xs text-slate-600">{a.summary}</p>
                <div className="mt-3 flex flex-wrap gap-1 text-[11px]">
                  {a.tags?.slice(0, 4).map((t) => (
                    <span
                      key={t}
                      className="rounded-full bg-slate-100 px-2 py-0.5 text-slate-700"
                    >
                      #{t}
                    </span>
                  ))}
                </div>
                <div className="mt-2 text-[11px] text-slate-500">
                  ~{a.estimatedReadMinutes} dk · {a.difficultyLevel}
                </div>
              </Link>
            ))
          )}
        </div>
      </section>
    </div>
  );
}
