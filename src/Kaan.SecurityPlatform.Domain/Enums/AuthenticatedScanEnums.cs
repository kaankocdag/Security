namespace Kaan.SecurityPlatform.Domain.Enums;

public enum TestAccountStatus
{
    PendingVerification = 0,
    Active = 1,
    Disabled = 2,
    FailedRegistration = 3
}

public enum TestAccountVerificationStatus
{
    NotVerified = 0,
    EmailPending = 1,
    Verified = 2,
    ManualReview = 3
}

public enum AuthScanComparisonResult
{
    PublicInBothModes = 0,
    LoginRequired = 1,
    AuthenticatedOnly = 2,
    AnonymousAccessCandidate = 3,
    DifferentContentAfterLogin = 4,
    AccessDeniedAsExpected = 5,
    Inconclusive = 6,
    ManualReviewRequired = 7
}

public enum RegistrationFormFieldKind
{
    Email = 0,
    Username = 1,
    FirstName = 2,
    LastName = 3,
    DisplayName = 4,
    Password = 5,
    ConfirmPassword = 6,
    Country = 7,
    BirthDate = 8,
    Checkbox = 9,
    TermsAcceptance = 10,
    NewsletterConsent = 11,
    Captcha = 12,
    Mfa = 13,
    VerificationCode = 14,
    UnknownRequiredField = 15
}

public enum ManualTakeoverReason
{
    None = 0,
    Captcha = 1,
    Mfa = 2,
    EmailVerification = 3,
    PhoneVerification = 4,
    TermsAcceptance = 5,
    UnknownRequiredField = 6,
    OAuth = 7,
    BotProtection = 8,
    PaymentOrSubscription = 9,
    SideEffectRisk = 10,
    UserRequested = 11
}

public enum AuthenticatedScanRunStatus
{
    NotStarted = 0,
    AwaitingApproval = 1,
    Registering = 2,
    AwaitingManualTakeover = 3,
    LoggingIn = 4,
    Scanning = 5,
    Completed = 6,
    Stopped = 7,
    Failed = 8,
    BlockedByPolicy = 9
}
