import { requireSession, isSystemAdmin } from '@/lib/session';
import { SiteTestWizardClient } from './site-test-client';

export default async function SiteTestWizardPage() {
  const { user } = await requireSession();
  if (!isSystemAdmin(user)) {
    return (
      <div className="mx-auto max-w-xl rounded-lg border border-amber-200 bg-amber-50 p-6 text-sm text-amber-900">
        <h1 className="text-lg font-semibold">PublicPassiveAssessment</h1>
        <p className="mt-2">
          Kamuya açık pasif tarama yalnızca SystemAdmin onayıyla başlatılabilir.
          Doğrulanmış domainlerde AuthorizedExternalAssessment bulgu detayından başlatılabilir.
        </p>
      </div>
    );
  }

  return <SiteTestWizardClient />;
}
