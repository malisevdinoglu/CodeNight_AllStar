using Campaign.Application.Common;
using Campaign.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Campaign.Infrastructure.Persistence;

/// <summary>
/// Iskender.md §2: campaign_number_seq / case_number_seq üzerinden nextval. Postgres sequence
/// atomik olduğundan (her çağrı benzersiz bir değer döner) yarış koşulu riski yoktur.
/// Core_Principles §10 SQL injection maddesi geregi: sorgu metni HER ZAMAN sabit literal'dir,
/// hicbir calisma-zamani degeri (kullanici girdisi dahil) string interpolasyonu/birlestirmesiyle
/// SQL'e karismaz - iki cagri siteside AYRI, tamamen sabit metinler kullanilir (tek, parametreli
/// bir yardimci metod yerine) ki statik kod taramasinda da supheli bir `$"..."` deseni GORUNMESIN.
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
        var results = await _dbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('campaign_number_seq') AS \"Value\"")
            .ToListAsync(cancellationToken);

        return NumberFormatter.FormatCampaignNumber(_dateTimeProvider.UtcNow.Year, results[0]);
    }

    public async Task<string> NextCaseNumberAsync(CancellationToken cancellationToken = default)
    {
        var results = await _dbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('case_number_seq') AS \"Value\"")
            .ToListAsync(cancellationToken);

        return NumberFormatter.FormatCaseNumber(_dateTimeProvider.UtcNow.Year, results[0]);
    }
}
