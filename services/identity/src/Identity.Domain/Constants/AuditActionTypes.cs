namespace Identity.Domain.Constants;

/// <summary>Case §3.4'te sayilan audit islem tipleri — string sabit (yeni tip eklemek migration gerektirmesin).</summary>
public static class AuditActionTypes
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string RoleChanged = "ROLE_CHANGED";
    public const string AccessDenied = "ACCESS_DENIED";
    public const string CampaignDeleted = "CAMPAIGN_DELETED";
    public const string StatusChangedCritical = "STATUS_CHANGED_CRITICAL";
}
