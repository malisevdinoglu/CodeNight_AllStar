"""RabbitMQ mesaj yönlendirme mantığı — gerçek bağlantı gerektirmez (dispatch_event pure).

DB katmanı (app.services.persistence) monkeypatch ile sahtelenir; burada test edilen şey
event_type → doğru handler → doğru argümanlarla çağrı zinciri (Core_Principles §8 payload alanları).
"""

from uuid import uuid4

from app.consumers import OFFER_RESPONDED, SEGMENT_OVERRIDDEN, dispatch_event


def test_segment_overridden_writes_classification_feedback(monkeypatch) -> None:
    calls: dict = {}

    def fake_record(session, *, case_id, predicted_segment, corrected_segment, corrected_by):
        calls["args"] = (case_id, predicted_segment, corrected_segment, corrected_by)
        return True

    monkeypatch.setattr("app.consumers.persistence.record_classification_feedback", fake_record)

    case_id = str(uuid4())
    changed_by = str(uuid4())
    dispatch_event(
        None,
        SEGMENT_OVERRIDDEN,
        {
            "case_id": case_id,
            "predicted_segment": "PASIF",
            "corrected_segment": "RISKLI_KAYIP",
            "changed_by": changed_by,
        },
    )

    assert calls["args"] == (case_id, "PASIF", "RISKLI_KAYIP", changed_by)


def test_offer_responded_ret_bumps_penalty(monkeypatch) -> None:
    calls: list[dict] = []

    def fake_bump(session, **kwargs):
        calls.append(kwargs)
        return 0.05

    monkeypatch.setattr("app.consumers.persistence.bump_score_adjustment_penalty", fake_bump)

    subscriber_id = str(uuid4())
    dispatch_event(
        None,
        OFFER_RESPONDED,
        {
            "offer_id": str(uuid4()),
            "subscriber_id": subscriber_id,
            "campaign_id": str(uuid4()),
            "campaign_type": "SADAKAT",
            "response": "RET",
        },
    )

    assert len(calls) == 1
    assert calls[0]["subscriber_id"] == subscriber_id
    assert calls[0]["campaign_type"] == "SADAKAT"


def test_offer_responded_kabul_does_not_bump_penalty(monkeypatch) -> None:
    called = False

    def fake_bump(session, **kwargs):
        nonlocal called
        called = True

    monkeypatch.setattr("app.consumers.persistence.bump_score_adjustment_penalty", fake_bump)

    dispatch_event(
        None,
        OFFER_RESPONDED,
        {
            "offer_id": str(uuid4()),
            "subscriber_id": str(uuid4()),
            "campaign_id": str(uuid4()),
            "campaign_type": "SADAKAT",
            "response": "KABUL",
        },
    )

    assert called is False


def test_unknown_event_type_is_safely_ignored() -> None:
    dispatch_event(None, "some.other.event", {})  # exception firlatmamasi yeterli
