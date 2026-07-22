using Campaign.Application.Dtos;
using Campaign.Domain.Enums;
using MediatR;

namespace Campaign.Application.Commands.CreateCampaign;

/// <summary>frontend/src/api/campaign.api.ts CreateCampaignRequest ile birebir (PERSONEL/SUPERVIZOR/ADMIN - MUSTERI degil, bkz. CampaignsController).</summary>
public sealed record CreateCampaignCommand(
    string Title,
    CampaignType Type,
    SegmentType TargetSegment,
    string? Description) : IRequest<CreateCampaignResult>;
