"""REST sözleşme testleri — sadece DB gerektirmeyen uçlar (/classify, /assign).

/recommend ve /metrics/accuracy ai-db'ye (predictions/model_metadata) yazar/okur; bunlar canlı
Postgres ile docker-compose entegrasyon adımında doğrulanır (Faz 4/5'teki EF Core testleriyle
aynı desen — DB-dokunan akışlar unit test yerine gerçek stack'te curl ile teyit edilir).
"""

from uuid import uuid4

from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def _profile_payload(**overrides) -> dict:
    payload = {
        "subscriberId": str(uuid4()),
        "currentPlan": "Platinum 40GB",
        "tenureMonths": 24,
        "avgMonthlyDataGb": 45.0,
        "avgMonthlyCallMinutes": 600,
        "monthlySpendTl": 550.0,
        "packagePurchaseCount": 3,
        "complaintCount": 0,
        "daysSinceLastActivity": 2,
        "pastAcceptanceRate": 0.65,
    }
    payload.update(overrides)
    return payload


def test_classify_returns_camelcase_envelope() -> None:
    response = client.post("/api/v1/ai/classify", json={"subscriberProfile": _profile_payload()})

    assert response.status_code == 200
    body = response.json()
    assert body["success"] is True
    assert body["error"] is None
    assert body["data"]["segment"] in {"YUKSEK_DEGER", "RISKLI_KAYIP", "YENI_ABONE", "PASIF"}
    assert 0.0 <= body["data"]["confidence"] <= 1.0


def test_classify_validation_error_returns_400_envelope() -> None:
    response = client.post("/api/v1/ai/classify", json={"subscriberProfile": {}})

    assert response.status_code == 400
    body = response.json()
    assert body["success"] is False
    assert body["data"] is None
    assert body["error"]["code"] == "AI_400_VALIDATION"
    assert len(body["error"]["details"]) > 0


def test_assign_ranks_matching_expert_first() -> None:
    matching_expert = str(uuid4())
    non_matching_expert = str(uuid4())

    body = {
        "case": {"caseId": str(uuid4()), "segment": "RISKLI_KAYIP", "priority": "YUKSEK"},
        "candidates": [
            {
                "expertId": non_matching_expert,
                "expertise": ["YUKSEK_DEGER"],
                "activeCaseCount": 0,
                "performanceScore": 1.0,
            },
            {
                "expertId": matching_expert,
                "expertise": ["RISKLI_KAYIP"],
                "activeCaseCount": 0,
                "performanceScore": 1.0,
            },
        ],
    }

    response = client.post("/api/v1/ai/assign", json=body)

    assert response.status_code == 200
    data = response.json()["data"]
    assert data[0]["expertId"] == matching_expert
    assert data[0]["score"] > data[1]["score"]


def test_assign_with_no_candidates_returns_empty_list() -> None:
    body = {
        "case": {"caseId": str(uuid4()), "segment": "PASIF", "priority": "DUSUK"},
        "candidates": [],
    }

    response = client.post("/api/v1/ai/assign", json=body)

    assert response.status_code == 200
    assert response.json()["data"] == []


def test_health_still_works_with_router_and_handlers_registered() -> None:
    response = client.get("/health")

    assert response.status_code == 200
    body = response.json()
    assert body["data"]["service"] == "ai"
