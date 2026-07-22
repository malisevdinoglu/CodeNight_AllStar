"""AI Service — Pydantic v2 sözleşme modelleri.

Core_Principles §4/§5:
- REST JSON alanları camelCase (`alias_generator=to_camel`), Python taraf snake_case (PEP8).
- Enum değerleri Türkçe UPPER_SNAKE ile BİREBİR taşınır — çeviri yok.
- Tüm yanıtlar `ApiResponse` zarfına sarılır (success/data/error).

Bu dosyadaki DTO'lar Campaign.Application.External.AiDtos.cs (C#) ile bire bir eşleşir;
alan isimleri orada PascalCase, burada camelCase (JSON'da aynı sonuca varır) - WireOptions
JsonStringEnumConverter ile enum'ları isim olarak taşıdığı için (bkz. AiServiceClient.cs fix,
Faz 6) enum alanları burada da string olarak tanımlanır, sayısal DEĞİL.
"""

from __future__ import annotations

from enum import Enum
from typing import Generic, TypeVar
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class _CamelModel(BaseModel):
    """Tüm wire DTO'larının ortak temeli: camelCase alias + snake_case Python erişimi."""

    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


# --------------------------------------------------------------------------
# Enum'lar — Core_Principles §4: Türkçe UPPER_SNAKE aynen (case dokümanı/DB/API/UI ortak)
# --------------------------------------------------------------------------


class SegmentType(str, Enum):
    YUKSEK_DEGER = "YUKSEK_DEGER"
    RISKLI_KAYIP = "RISKLI_KAYIP"
    YENI_ABONE = "YENI_ABONE"
    PASIF = "PASIF"
    BELIRSIZ = "BELIRSIZ"  # AI kapalıyken Campaign'in verdiği fallback - AI bunu asla tahmin etmez


class CampaignType(str, Enum):
    EK_PAKET = "EK_PAKET"
    TARIFE_YUKSELTME = "TARIFE_YUKSELTME"
    CIHAZ_FIRSATI = "CIHAZ_FIRSATI"
    SADAKAT = "SADAKAT"


class CasePriority(str, Enum):
    DUSUK = "DUSUK"
    ORTA = "ORTA"
    YUKSEK = "YUKSEK"
    KRITIK = "KRITIK"


# --------------------------------------------------------------------------
# Ortak zarf (Core_Principles §5)
# --------------------------------------------------------------------------

T = TypeVar("T")


class ApiError(BaseModel):
    code: str
    message: str
    details: list[str] = Field(default_factory=list)


class ApiResponse(BaseModel, Generic[T]):
    success: bool
    data: T | None = None
    error: ApiError | None = None


# --------------------------------------------------------------------------
# Ortak: abone profili (AiSubscriberProfileDto.cs karşılığı)
# --------------------------------------------------------------------------


class SubscriberProfile(_CamelModel):
    subscriber_id: UUID
    current_plan: str
    tenure_months: int = Field(ge=0)
    avg_monthly_data_gb: float = Field(ge=0)
    avg_monthly_call_minutes: int = Field(ge=0)
    monthly_spend_tl: float = Field(ge=0)
    package_purchase_count: int = Field(ge=0)
    complaint_count: int = Field(ge=0)
    days_since_last_activity: int = Field(ge=0)
    past_acceptance_rate: float = Field(ge=0, le=1)


# --------------------------------------------------------------------------
# POST /api/v1/ai/recommend
# --------------------------------------------------------------------------


class CampaignSummary(_CamelModel):
    campaign_id: UUID
    type: CampaignType
    discount_rate: float = Field(ge=0)


class RecommendRequest(_CamelModel):
    subscriber_profile: SubscriberProfile
    campaigns: list[CampaignSummary]


class RecommendationResult(_CamelModel):
    campaign_id: UUID
    recommendation_score: float
    conversion_probability: float


# --------------------------------------------------------------------------
# POST /api/v1/ai/classify
# --------------------------------------------------------------------------


class ClassifyRequest(_CamelModel):
    subscriber_profile: SubscriberProfile


class ClassifyResult(_CamelModel):
    segment: SegmentType
    confidence: float


# --------------------------------------------------------------------------
# POST /api/v1/ai/assign
# --------------------------------------------------------------------------


class CaseSummary(_CamelModel):
    case_id: UUID
    segment: SegmentType
    priority: CasePriority


class Candidate(_CamelModel):
    expert_id: UUID
    expertise: list[SegmentType]
    active_case_count: int = Field(ge=0)
    performance_score: float = Field(ge=0, le=1)


class AssignRequest(_CamelModel):
    case: CaseSummary
    candidates: list[Candidate]


class AssignmentScore(_CamelModel):
    expert_id: UUID
    score: float


# --------------------------------------------------------------------------
# GET /api/v1/ai/metrics/accuracy (+3 bonus: kategori kırılımı)
# --------------------------------------------------------------------------


class AccuracyByCategory(_CamelModel):
    segment: str
    accuracy: float
    total: int


class AccuracyMetrics(_CamelModel):
    overall: float
    by_category: list[AccuracyByCategory]
