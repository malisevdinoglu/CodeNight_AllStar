from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_health_api_response_zarfina_uyar():
    response = client.get("/health")
    assert response.status_code == 200
    body = response.json()
    assert body["success"] is True
    assert body["data"]["service"] == "ai"
    assert body["error"] is None
