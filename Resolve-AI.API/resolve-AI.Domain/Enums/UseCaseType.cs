namespace resolve_AI.Domain.Enums;

public enum UseCaseType
{
    PasswordExpired = 1,
    ForgottenPassword = 2,
    IncorrectHostname = 3,
    DuplicateComputerAccountOnADRejoin = 4,
    DisabledUserAccount = 5,
    MfaResetAfterMobilePhoneChange = 6,
    Microsoft365LicenseNotAssigned = 7,
    MailboxCapacityThresholdAlert = 8,
    MfaNotConfigured = 9,
    HostnameNotBoundToADUser = 10,
    AccountLockout = 11
}