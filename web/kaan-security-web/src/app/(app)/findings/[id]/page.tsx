import Link from 'next/link';
import { notFound } from 'next/navigation';
import { AuthorizedExternalPanel } from '@/components/findings/authorized-external-panel';
import { BbEligibleBadge, BbEligibleChime } from '@/components/findings/bb-eligible-badge';
import { FindingEducationActions } from '@/components/findings/finding-education-actions';
import { ValidateFindingPanel } from '@/components/findings/validate-finding-panel';
import { apiFetch } from '@/lib/api';
import { requireSession, isSystemAdmin } from '@/lib/session';
import { severityColor } from '@/lib/utils';

interface FindingDetail {
  id: string;
  title: string;
  description: string;
  technicalDescription?: string;
  businessImpact?: string;
  severity: string;
  technicalSeverity?: string;
  exploitability?: string;
  demonstratedImpact?: boolean;
  requiresManualValidation?: boolean;
  findingClass?: string;
  bugBountyEligible?: boolean;
  eligibilityReason?: string | null;
  programPolicyMatch?: string | null;
  submissionRecommendation?: string;
  policyCategory?: string;
  confidenceLevel: string;
  category: string;
  cweCode?: string;
  owaspCategory?: string;
  affectedUrl?: string;
  affectedParameter?: string;
  evidence?: string;
  remediation?: string;
  remediationExampleConfig?: string;
  turkishExecutiveSummary?: string;
  status: string;
  checkCode: string;
  fingerprint?: string | null;
  confirmedVulnerability?: boolean;
  latestValidationStatus?: string | null;
  submissionEligible?: boolean;
  potentialRewardEligible?: boolean;
  latestValidationRunId?: string | null;
  domainAssetId?: string | null;
  domainHostName?: string | null;
  domainIsVerified?: boolean;
  scanJobId?: string | null;
  knowledgeLinks: {
    articleId: string;
    articleSlug: string;
    articleTitle: string;
    relevanceScore: number;
  }[];
}

export default async function FindingDetailPage({
  params
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const { accessToken, user } = await requireSession();
  const admin = isSystemAdmin(user);
  let finding: FindingDetail;
  try {
    finding = await apiFetch<FindingDetail>(`/api/findings/${id}`, {
      accessToken,
      serverSide: true
    });
  } catch {
    notFound();
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <div>
        <div className="flex items-center gap-2">
          <span
            className={`rounded-full border px-2 py-0.5 text-xs ${severityColor(finding.severity)}`}
          >
            {finding.severity}
          </span>
          <span className="rounded-full border border-slate-200 bg-white px-2 py-0.5 text-xs text-slate-600">
            {finding.confidenceLevel}
          </span>
          <span className="rounded-full border border-slate-200 bg-white px-2 py-0.5 text-xs text-slate-600">
            {finding.category}
          </span>
        </div>
        <h1 className="mt-2 text-2xl font-bold text-slate-900">{finding.title}</h1>
        <div className="mt-2 flex flex-wrap items-center gap-3 text-sm text-slate-600">
          <span>
            Domain:{' '}
            <strong className="text-slate-900">{finding.domainHostName ?? '—'}</strong>
          </span>
          {finding.scanJobId && (
            <Link
              href={`/scans/${finding.scanJobId}`}
              className="text-[color:var(--color-brand-700)] hover:underline"
            >
              İlgili taramaya git →
            </Link>
          )}
        </div>
      </div>

      <BbEligibleChime play={finding.bugBountyEligible === true} />
      <BbEligibleBadge eligible={finding.bugBountyEligible} />

      {finding.turkishExecutiveSummary && (
        <section className="rounded-2xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-900">
          <div className="mb-1 text-xs font-semibold uppercase">Yönetici özeti</div>
          {finding.turkishExecutiveSummary}
        </section>
      )}

      <section
        className={`rounded-2xl border p-5 shadow-sm ${
          finding.bugBountyEligible
            ? 'border-emerald-400 bg-emerald-50/50'
            : 'border-indigo-200 bg-indigo-50/40'
        }`}
      >
        <h2 className="flex items-center gap-2 text-sm font-semibold text-slate-800">
          Finding Validation &amp; Bug Bounty Eligibility
          {finding.bugBountyEligible && <BbEligibleBadge eligible compact />}
        </h2>
        <p className="mt-1 text-xs text-slate-600">
          Scanner şiddeti ile teknik/BB değerlendirmesi birbirinden bağımsızdır. Politika:{' '}
          {finding.programPolicyMatch ?? 'AmazonVRP'}
        </p>
        <dl className="mt-3 grid grid-cols-1 gap-2 text-sm md:grid-cols-2">
          <Row label="Scanner Severity" value={finding.severity} />
          <Row label="Technical Severity" value={finding.technicalSeverity} />
          <Row label="Finding Class" value={finding.findingClass} />
          <Row label="Exploitability" value={finding.exploitability} />
          <Row
            label="Demonstrated Impact"
            value={finding.demonstratedImpact ? 'true' : 'false'}
          />
          <Row
            label="Requires Manual Validation"
            value={finding.requiresManualValidation ? 'true' : 'false'}
          />
          <Row
            label="Bug Bounty Eligible"
            value={finding.bugBountyEligible ? 'true' : 'false'}
          />
          <Row label="Submission Recommendation" value={finding.submissionRecommendation} />
          <Row label="Policy Category" value={finding.policyCategory} />
          <Row
            label="Confirmed Vulnerability"
            value={finding.confirmedVulnerability ? 'true' : 'false'}
          />
          <Row label="Latest Validation" value={finding.latestValidationStatus ?? '—'} />
          <Row label="Submission Eligible" value={finding.submissionEligible ? 'true' : 'false'} />
          <Row
            label="Potential Reward Eligible"
            value={finding.potentialRewardEligible ? 'true (not guaranteed)' : 'false'}
          />
        </dl>
        {finding.eligibilityReason && (
          <p className="mt-3 rounded-md border border-indigo-100 bg-white px-3 py-2 text-xs text-slate-700">
            {finding.eligibilityReason}
          </p>
        )}
      </section>

      <ValidateFindingPanel
        findingId={finding.id}
        domainAssetId={finding.domainAssetId}
        confirmedVulnerability={finding.confirmedVulnerability}
        demonstratedImpact={finding.demonstratedImpact}
        submissionEligible={finding.submissionEligible}
        potentialRewardEligible={finding.potentialRewardEligible}
        latestValidationStatus={finding.latestValidationStatus}
        findingClass={finding.findingClass}
      />

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <h2 className="text-sm font-semibold text-slate-800">Açıklama</h2>
        <p className="mt-2 whitespace-pre-line text-sm text-slate-700">{finding.description}</p>
        {finding.technicalDescription && (
          <>
            <h3 className="mt-4 text-sm font-semibold text-slate-800">Teknik detay</h3>
            <p className="mt-2 whitespace-pre-line text-sm text-slate-700">
              {finding.technicalDescription}
            </p>
          </>
        )}
      </section>

      <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <h2 className="text-sm font-semibold text-slate-800">Etki ve kanıt</h2>
        <dl className="mt-2 grid grid-cols-1 gap-2 text-sm md:grid-cols-2">
          <Row label="Etkilenen URL" value={finding.affectedUrl} />
          <Row label="Parametre" value={finding.affectedParameter} />
          <Row label="CWE" value={finding.cweCode} />
          <Row label="OWASP" value={finding.owaspCategory} />
        </dl>
        {finding.evidence && (
          <pre className="mt-3 overflow-x-auto rounded-md border border-slate-200 bg-slate-50 p-3 text-xs text-slate-700">
            {finding.evidence}
          </pre>
        )}
      </section>

      <FindingEducationActions fingerprint={finding.fingerprint} />

      <AuthorizedExternalPanel
        findingId={finding.id}
        isSystemAdmin={admin}
        domainAssetId={finding.domainAssetId}
        domainHostName={finding.domainHostName}
        domainIsVerified={finding.domainIsVerified === true}
      />

      {finding.remediation && (
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="text-sm font-semibold text-slate-800">Önerilen çözüm</h2>
          <p className="mt-2 whitespace-pre-line text-sm text-slate-700">{finding.remediation}</p>
          {finding.remediationExampleConfig && (
            <pre className="mt-3 overflow-x-auto rounded-md border border-slate-200 bg-slate-950 p-3 text-xs text-emerald-200">
              {finding.remediationExampleConfig}
            </pre>
          )}
        </section>
      )}

      {finding.knowledgeLinks.length > 0 && (
        <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="text-sm font-semibold text-slate-800">İlgili bilgi bankası</h2>
          <ul className="mt-2 space-y-1 text-sm">
            {finding.knowledgeLinks.map((link) => (
              <li key={link.articleId}>
                <Link
                  href={`/knowledge/article/${link.articleSlug}`}
                  className="text-[color:var(--color-brand-700)] hover:underline"
                >
                  {link.articleTitle}
                </Link>
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}

function Row({ label, value }: { label: string; value?: string }) {
  return (
    <div>
      <dt className="text-xs text-slate-500">{label}</dt>
      <dd className="mt-0.5 text-sm text-slate-800">{value ?? '—'}</dd>
    </div>
  );
}
