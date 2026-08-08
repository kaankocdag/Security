'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';
import {
  LayoutDashboard,
  FolderKanban,
  Globe,
  Radar,
  ShieldAlert,
  FileText,
  BookOpen,
  Users,
  ShieldCheck,
  ClipboardList,
  Rocket,
  FlaskConical,
  CircleHelp,
  Bug
} from 'lucide-react';
import { HelpTip } from '@/components/ui/help-tip';

interface Item {
  href: string;
  label: string;
  tip: string;
  icon: React.ComponentType<{ size?: number; className?: string }>;
  adminOnly?: boolean;
}

const nav: Item[] = [
  {
    href: '/dashboard',
    label: 'Panel',
    tip: 'Firma güvenlik özeti: projeler, skor ve son taramalar.',
    icon: LayoutDashboard
  },
  {
    href: '/site-test',
    label: 'Public Passive Assessment',
    tip: 'Kamuya açık siteye yalnızca GET/HEAD pasif tarama. SystemAdmin. Domain doğrulama yok.',
    icon: Rocket,
    adminOnly: true
  },
  {
    href: '/projects',
    label: 'Projeler',
    tip: 'Sitelerinizi gruplayan güvenlik projeleri (Production, Staging…).',
    icon: FolderKanban
  },
  {
    href: '/domains',
    label: 'Domainler',
    tip: 'Taranacak alan adları. SSRF kurallarına uymalıdır.',
    icon: Globe
  },
  {
    href: '/scans',
    label: 'Taramalar',
    tip: 'Başlatılmış pasif tarama işleri ve ilerleme durumu.',
    icon: Radar
  },
  {
    href: '/findings',
    label: 'Bulgular',
    tip: 'Türkçe açıklamalı güvenlik bulguları ve düzeltme önerileri.',
    icon: ShieldAlert
  },
  {
    href: '/reports',
    label: 'Raporlar',
    tip: 'HTML ve firmaya iletilebilir uzun TXT güvenlik raporları.',
    icon: FileText
  },
  {
    href: '/knowledge',
    label: 'Bilgi Bankası',
    tip: 'Bulgularla ilgili eğitim makaleleri ve örnekler.',
    icon: BookOpen
  },
  {
    href: '/admin/users',
    label: 'Üye Onayları',
    tip: 'Yeni üye ve firma kayıtlarını onaylayın veya reddedin.',
    icon: Users,
    adminOnly: true
  },
  {
    href: '/admin/knowledge',
    label: 'KB Yönetimi',
    tip: 'Bilgi bankası kategorileri, makaleler ve medya yönetimi.',
    icon: ClipboardList,
    adminOnly: true
  },
  {
    href: '/admin/lab',
    label: 'Isolated Security Lab',
    tip: 'Allowlist hedef + imzalı senaryo. Serbest URL/payload yok. Step-up parola gerekir.',
    icon: FlaskConical,
    adminOnly: true
  },
  {
    href: '/hackerone',
    label: 'HackerOne',
    tip: 'Bug bounty workspace: adaylar, rapor builder, kopyala/aç. API submit kapalı varsayılan.',
    icon: Bug,
    adminOnly: true
  },
  {
    href: '/help',
    label: 'Kullanım rehberi',
    tip: 'Admin ve kullanıcı için adım adım nasıl kullanılır.',
    icon: CircleHelp
  }
];

export function Sidebar({ isSystemAdmin }: { isSystemAdmin: boolean }) {
  const pathname = usePathname();
  return (
    <aside className="hidden w-64 shrink-0 border-r border-slate-200 bg-white/70 py-4 md:block">
      <div className="mb-4 flex items-center gap-2 px-4 text-[15px] font-bold text-slate-900">
        <ShieldCheck className="text-[color:var(--color-brand-600)]" size={20} />
        Kaan Security
      </div>
      <nav className="space-y-1 px-2">
        {nav
          .filter((item) => !item.adminOnly || isSystemAdmin)
          .map((item) => {
            const Icon = item.icon;
            const active =
              pathname === item.href ||
              (item.href !== '/dashboard' && pathname?.startsWith(item.href));
            return (
              <div
                key={item.href}
                className={cn(
                  'flex items-center gap-1 rounded-md pr-1 transition',
                  active
                    ? 'bg-[color:var(--color-brand-50)] text-[color:var(--color-brand-700)]'
                    : 'text-slate-600 hover:bg-slate-100'
                )}
              >
                <Link
                  href={item.href}
                  className="flex min-w-0 flex-1 items-center gap-2 px-3 py-2 text-sm font-medium"
                >
                  <Icon size={16} className="shrink-0" />
                  <span className="truncate">{item.label}</span>
                </Link>
                <HelpTip text={item.tip} side="right" className="mr-1" />
              </div>
            );
          })}
      </nav>
    </aside>
  );
}
