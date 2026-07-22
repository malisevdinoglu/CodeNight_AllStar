# Gamification Service

CampaignCell — Gamification mikroservisi (.NET 8, Clean Architecture).

- `src/Gamification.Domain` — Entity'ler, enum'lar, domain kurallari (bagimsiz katman)
- `src/Gamification.Application` — CQRS Command/Query + Handler (MediatR), interface'ler, DTO'lar
- `src/Gamification.Infrastructure` — EF Core DbContext, repository'ler, MassTransit, dis servisler
- `src/Gamification.Api` — Controller/endpoint, middleware, DI (ince katman)
- `tests/Gamification.UnitTests` — xUnit + FluentAssertions + Moq

Migration + seed uygulama acilisinda otomatik calisir. Detaylar: kok `README.md` ve `Core_Principles.md`.
