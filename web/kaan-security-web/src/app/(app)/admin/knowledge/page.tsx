import { requireSession, isSystemAdmin } from '@/lib/session';
import { redirect } from 'next/navigation';
import { apiFetch } from '@/lib/api';
import { AdminKnowledgeClient } from './admin-knowledge-client';

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

export default async function AdminKnowledgePage() {
  const { accessToken, user } = await requireSession();
  if (!isSystemAdmin(user)) {
    redirect('/dashboard');
  }
  let categories: Category[] = [];
  let articles: Article[] = [];
  try {
    [categories, articles] = await Promise.all([
      apiFetch<Category[]>('/api/admin/knowledge/categories', {
        accessToken,
        serverSide: true
      }),
      apiFetch<Article[]>('/api/admin/knowledge/articles', { accessToken, serverSide: true })
    ]);
  } catch {
    // ignore
  }
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Bilgi Bankası Yönetimi</h1>
        <p className="mt-1 text-sm text-slate-600">
          Kategorileri ve makaleleri yönetin, medya (Instagram görselleri dahil) yükleyin.
        </p>
      </div>
      <AdminKnowledgeClient initialCategories={categories} initialArticles={articles} />
    </div>
  );
}
