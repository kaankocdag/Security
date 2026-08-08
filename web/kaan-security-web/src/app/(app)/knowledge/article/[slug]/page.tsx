import Link from 'next/link';
import { notFound } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { requireSession } from '@/lib/session';
import { formatDateTr } from '@/lib/utils';

interface ArticleDetail {
  id: string;
  slug: string;
  title: string;
  summary: string;
  bodyMarkdown: string;
  bodyHtml: string;
  categoryName: string;
  categorySlug: string;
  cweCode?: string;
  owaspCategory?: string;
  cveCode?: string;
  difficultyLevel: string;
  estimatedReadMinutes: number;
  tags: string[];
  sourceAttribution?: string;
  sourceUrl?: string;
  publishedAt?: string | null;
  mediaAssets: {
    id: string;
    publicUrl: string;
    caption?: string;
    altText?: string;
    displayOrder: number;
  }[];
  references: { id: string; url: string; title: string; description?: string }[];
}

export default async function ArticlePage({
  params
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const { accessToken } = await requireSession();
  let article: ArticleDetail;
  try {
    article = await apiFetch<ArticleDetail>(`/api/knowledge/articles/${slug}`, {
      accessToken,
      serverSide: true
    });
  } catch {
    notFound();
  }

  return (
    <article className="mx-auto max-w-3xl space-y-6">
      <div>
        <Link
          href="/knowledge"
          className="text-xs font-semibold uppercase tracking-widest text-[color:var(--color-brand-600)]"
        >
          ← Bilgi Bankası
        </Link>
        <div className="mt-2 flex flex-wrap gap-2 text-[11px] text-slate-500">
          <span className="rounded-full bg-slate-100 px-2 py-0.5">{article.categoryName}</span>
          <span className="rounded-full bg-slate-100 px-2 py-0.5">{article.difficultyLevel}</span>
          <span className="rounded-full bg-slate-100 px-2 py-0.5">
            ~{article.estimatedReadMinutes} dk
          </span>
          {article.cweCode && (
            <span className="rounded-full bg-amber-50 px-2 py-0.5 text-amber-800">
              {article.cweCode}
            </span>
          )}
          {article.owaspCategory && (
            <span className="rounded-full bg-orange-50 px-2 py-0.5 text-orange-800">
              {article.owaspCategory}
            </span>
          )}
        </div>
        <h1 className="mt-2 text-3xl font-bold text-slate-900">{article.title}</h1>
        <p className="mt-2 text-sm text-slate-600">{article.summary}</p>
        {article.publishedAt && (
          <div className="mt-1 text-xs text-slate-500">
            Yayın: {formatDateTr(article.publishedAt)}
          </div>
        )}
      </div>

      <div
        className="prose prose-slate max-w-none text-sm"
        dangerouslySetInnerHTML={{ __html: article.bodyHtml || `<pre>${escapeHtml(article.bodyMarkdown)}</pre>` }}
      />

      {article.mediaAssets.length > 0 && (
        <section>
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wider text-slate-500">
            Görseller
          </h2>
          <div className="grid gap-3 md:grid-cols-2">
            {article.mediaAssets
              .sort((a, b) => a.displayOrder - b.displayOrder)
              .map((m) => (
                <figure key={m.id} className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={m.publicUrl}
                    alt={m.altText ?? m.caption ?? article.title}
                    className="h-56 w-full object-cover"
                  />
                  {m.caption && (
                    <figcaption className="p-2 text-xs text-slate-600">{m.caption}</figcaption>
                  )}
                </figure>
              ))}
          </div>
        </section>
      )}

      {article.references.length > 0 && (
        <section>
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wider text-slate-500">
            Kaynaklar
          </h2>
          <ul className="space-y-1 text-sm">
            {article.references.map((r) => (
              <li key={r.id}>
                <a
                  href={r.url}
                  target="_blank"
                  rel="noreferrer"
                  className="text-[color:var(--color-brand-700)] hover:underline"
                >
                  {r.title}
                </a>
                {r.description && (
                  <span className="ml-2 text-xs text-slate-500">— {r.description}</span>
                )}
              </li>
            ))}
          </ul>
        </section>
      )}

      {article.sourceAttribution && (
        <div className="rounded-md border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600">
          Kaynak: {article.sourceAttribution}
          {article.sourceUrl && (
            <>
              {' · '}
              <a
                className="text-[color:var(--color-brand-700)] hover:underline"
                href={article.sourceUrl}
                target="_blank"
                rel="noreferrer"
              >
                {article.sourceUrl}
              </a>
            </>
          )}
        </div>
      )}
    </article>
  );
}

function escapeHtml(input: string) {
  return input
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}
