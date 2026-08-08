import type { Metadata } from 'next';
import { Outfit, Source_Sans_3 } from 'next/font/google';
import './globals.css';

const display = Outfit({
  subsets: ['latin', 'latin-ext'],
  variable: '--font-display',
  display: 'swap'
});

const body = Source_Sans_3({
  subsets: ['latin', 'latin-ext'],
  variable: '--font-body',
  display: 'swap'
});

export const metadata: Metadata = {
  title: 'Kaan Security Platform',
  description:
    'Firmalar için pasif güvenlik doktorluğu: tarama, Türkçe rapor, düzeltme önerileri ve yeniden test.',
  applicationName: 'Kaan Security Platform',
  authors: [{ name: 'Kaan Security' }],
  keywords: ['siber güvenlik', 'security', 'web security', 'kaan'],
  robots: { index: false, follow: false }
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="tr" className={`${display.variable} ${body.variable}`}>
      <body className="font-sans antialiased">{children}</body>
    </html>
  );
}
