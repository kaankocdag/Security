namespace Kaan.SecurityPlatform.Domain.Enums;

public enum ScanType
{
    PassiveWeb = 0,
    Certificate = 1,
    SecurityHeaders = 2,
    Cookie = 3,
    InformationDisclosure = 4,
    FullPassive = 5,
    SourceCode = 10,
    DependencyAudit = 11,
    SecretScan = 12
}
