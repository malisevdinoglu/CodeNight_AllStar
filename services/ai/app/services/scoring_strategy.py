"""AI Service — uzman atama skor formülü (Strategy pattern).

Mali.md §6 / Core_Principles §2.5: "skor = uzmanlik_eslesme*0.5 + bosluk_orani*0.3 + performans*0.2
(Strategy pattern: formül sınıfı değiştirilebilir)". `/api/v1/ai/assign` bu stratejiyi çağırır,
sonucu azalan skora göre sıralar (jüri: "bu skor nasıl çıktı" sorusuna canlı cevap).

Not: `candidate.performance_score`, Campaign.Application.Commands.AssignExpert tarafında
kapasiteden türetilmiş dokümante edilmiş bir sinyaldir (Identity'de henüz gerçek bir performans
metriği yok) - AI bunu olduğu gibi ağırlıklandırır, kaynağıyla ilgilenmez (arayüz sözleşmesi).
"""

from __future__ import annotations

from abc import ABC, abstractmethod

from app.schemas import CaseSummary, Candidate


class AssignmentScoringStrategy(ABC):
    """Değiştirilebilir atama skor formülü sözleşmesi."""

    @abstractmethod
    def score(self, case: CaseSummary, candidate: Candidate) -> float:
        """0-1 aralığında bir atama uygunluk skoru döner (yüksek = daha uygun)."""


class WeightedAssignmentScoringStrategy(AssignmentScoringStrategy):
    """Varsayılan/case dokümanındaki formül: ağırlıklı doğrusal kombinasyon.

    - uzmanlık_eşleşme (0.5): case.segment, candidate.expertise içinde mi (ikili sinyal).
    - boşluk_oranı (0.3): candidate.active_case_count ne kadar düşükse o kadar yüksek - üst sınır
      Campaign.Application.Commands.AssignExpert.AssignExpertCommandHandler.MaxActiveCasesPerExpert
      (=5) ile SENKRON dokümante edilmiş varsayımdır (AI bu değeri tel üzerinden almaz; iki tarafta
      da sabit kod olarak tutulur - değişirse iki dosya birlikte güncellenir).
    - performans (0.2): candidate.performance_score doğrudan kullanılır.
    """

    EXPERTISE_WEIGHT = 0.5
    CAPACITY_WEIGHT = 0.3
    PERFORMANCE_WEIGHT = 0.2

    ASSUMED_MAX_ACTIVE_CASES_PER_EXPERT = 5

    def score(self, case: CaseSummary, candidate: Candidate) -> float:
        expertise_match = 1.0 if case.segment in candidate.expertise else 0.0

        capacity_ratio = max(
            0.0,
            1.0 - candidate.active_case_count / self.ASSUMED_MAX_ACTIVE_CASES_PER_EXPERT,
        )

        performance = min(1.0, max(0.0, candidate.performance_score))

        total = (
            expertise_match * self.EXPERTISE_WEIGHT
            + capacity_ratio * self.CAPACITY_WEIGHT
            + performance * self.PERFORMANCE_WEIGHT
        )
        return round(total, 4)


def get_scoring_strategy() -> AssignmentScoringStrategy:
    """DI noktası: formül değişirse (A/B testi, öğrenen model vb.) sadece burası değişir."""
    return WeightedAssignmentScoringStrategy()
