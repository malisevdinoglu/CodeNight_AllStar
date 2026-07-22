namespace Campaign.Domain.Services;

/// <summary>
/// Iskender.md §2: CMP-2026-000123 / OPT-2026-000045 formatı. Sıra numarasının kaynağı
/// (campaign_number_seq/case_number_seq, nextval) Infrastructure'da alınır — biçimlendirme
/// saf bir domain kuralı olduğu için burada yaşar.
/// </summary>
public static class NumberFormatter
{
    public static string FormatCampaignNumber(int year, long sequenceValue) =>
        $"CMP-{year}-{sequenceValue:D6}";

    public static string FormatCaseNumber(int year, long sequenceValue) =>
        $"OPT-{year}-{sequenceValue:D6}";
}
