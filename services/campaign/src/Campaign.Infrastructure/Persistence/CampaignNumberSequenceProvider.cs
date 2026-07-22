using Campaign.Application.Common;
using Campaign.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence;

/// <summary>
/// Iskender.md §2: campaign_number_seq / case_number_seq üzerinden nextval. Postgres sequence
/// atomik olduğundan (her çağrı benzersiz bir değer döner) yarış koşulu riski yoktur.
/// EF Core 8 "SqlQueryRaw&lt;T&gt;" ile parametresiz skaler sorgu (sequence adı sabit, kullanıcı
/// girdisi değildir - SQL injection riski yok).
/// </summary>
public sealed class CampaignNumberSequenceProvider : INumberSequenceProvider
{
    private readonly CampaignDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CampaignNumberSequenceProvider(CampaignDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<string> NextCampaignNumberAsync(CancellationToken cancellationToken = default)
    {
        var value = await NextSequenceValueAsync("campaign_number_seq", cancellationToken);
        return NumberFormatter.FormatCampaignNumber(_dateTimeProvider.UtcNow.Year, value);
    }

    public async Task<string> NextCaseNumberAsync(CancellationToken cancellationToken = default)
    {
        var value = await NextSequenceValueAsync("case_number_seq", cancellationToken);
        return NumberFormatter.FormatCaseNumber(_dateTimeProvider.UtcNow.Year, value);
    }

    private async Task<long> NextSequenceValueAsync(string sequenceName, CancellationToken cancellationToken)
    {
        var results = await _dbContext.Database
            .SqlQueryRaw<long>($"SELECT nextval('{sequenceName}') AS \"Value\"")
            .ToListAsync(cancellationToken);

        return results[0];
    }
}
