namespace Campaign.Application.Common;

/// <summary>Iskender.md §2: campaign_number_seq / case_number_seq (DB sequence, nextval).</summary>
public interface INumberSequenceProvider
{
    Task<string> NextCampaignNumberAsync(CancellationToken cancellationToken = default);

    Task<string> NextCaseNumberAsync(CancellationToken cancellationToken = default);
}
