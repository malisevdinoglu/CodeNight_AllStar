"""Strategy pattern doğrulaması: skor = uzmanlik_eslesme*0.5 + bosluk_orani*0.3 + performans*0.2."""

from uuid import uuid4

from app.schemas import CasePriority, CaseSummary, Candidate, SegmentType
from app.services.scoring_strategy import WeightedAssignmentScoringStrategy


def _case(segment: SegmentType = SegmentType.RISKLI_KAYIP) -> CaseSummary:
    return CaseSummary(case_id=uuid4(), segment=segment, priority=CasePriority.YUKSEK)


def _candidate(expertise: list[SegmentType], active_case_count: int, performance_score: float) -> Candidate:
    return Candidate(
        expert_id=uuid4(),
        expertise=expertise,
        active_case_count=active_case_count,
        performance_score=performance_score,
    )


def test_full_match_empty_queue_perfect_performance_gives_max_score() -> None:
    strategy = WeightedAssignmentScoringStrategy()
    case = _case()
    candidate = _candidate([SegmentType.RISKLI_KAYIP], active_case_count=0, performance_score=1.0)

    assert strategy.score(case, candidate) == 1.0


def test_no_expertise_match_drops_the_dominant_weight() -> None:
    strategy = WeightedAssignmentScoringStrategy()
    case = _case()
    matching = _candidate([SegmentType.RISKLI_KAYIP], active_case_count=0, performance_score=1.0)
    non_matching = _candidate([SegmentType.YUKSEK_DEGER], active_case_count=0, performance_score=1.0)

    assert strategy.score(case, matching) > strategy.score(case, non_matching)
    assert strategy.score(case, non_matching) == round(0.3 + 0.2, 4)


def test_full_queue_zeroes_capacity_component() -> None:
    strategy = WeightedAssignmentScoringStrategy()
    case = _case()
    full_queue = _candidate(
        [SegmentType.RISKLI_KAYIP],
        active_case_count=strategy.ASSUMED_MAX_ACTIVE_CASES_PER_EXPERT,
        performance_score=1.0,
    )

    assert strategy.score(case, full_queue) == round(0.5 + 0.0 + 0.2, 4)


def test_over_capacity_never_goes_negative() -> None:
    strategy = WeightedAssignmentScoringStrategy()
    case = _case()
    overloaded = _candidate(
        [SegmentType.RISKLI_KAYIP],
        active_case_count=strategy.ASSUMED_MAX_ACTIVE_CASES_PER_EXPERT * 3,
        performance_score=0.0,
    )

    assert strategy.score(case, overloaded) == round(0.5, 4)


def test_ranking_orders_candidates_by_score_descending() -> None:
    strategy = WeightedAssignmentScoringStrategy()
    case = _case()
    best = _candidate([SegmentType.RISKLI_KAYIP], active_case_count=0, performance_score=1.0)
    worst = _candidate([SegmentType.YUKSEK_DEGER], active_case_count=5, performance_score=0.0)

    ranked = sorted([worst, best], key=lambda c: strategy.score(case, c), reverse=True)

    assert ranked == [best, worst]
