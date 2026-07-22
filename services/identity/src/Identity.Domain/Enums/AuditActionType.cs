namespace Identity.Domain.Enums;

/// <summary>
/// Case dokümanı §3.4'te sayılan olaylar + akışı tamamlayan birkaç ek tip
/// (LOGOUT, TOKEN_REFRESHED, TOKEN_THEFT_DETECTED, STAFF_CREATED — case'in
/// listesi kapalı küme değil, "aşağıdaki işlemler" örnek verir).
/// </summary>
public enum AuditActionType
{
    LOGIN_SUCCESS,
    LOGIN_FAILED,
    ACCOUNT_LOCKED,
    ROLE_CHANGED,
    ACCESS_DENIED,
    CAMPAIGN_DELETED,
    STATUS_CHANGED_CRITICAL,
    LOGOUT,
    TOKEN_REFRESHED,
    TOKEN_THEFT_DETECTED,
    STAFF_CREATED
}
