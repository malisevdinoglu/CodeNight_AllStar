# Identity Service

CampaignCell — Identity mikroservisi (.NET 8, Clean Architecture).

- `src/Identity.Domain` — Entity'ler, enum'lar, domain kurallari (bagimsiz katman)
- `src/Identity.Application` — CQRS Command/Query + Handler (MediatR), interface'ler, DTO'lar
- `src/Identity.Infrastructure` — EF Core DbContext, repository'ler, MassTransit, dis servisler
- `src/Identity.Api` — Controller/endpoint, middleware, DI (ince katman)
- `tests/Identity.UnitTests` — xUnit + FluentAssertions + Moq

Migration + seed uygulama acilisinda otomatik calisir. Detaylar: kok `README.md` ve `Core_Principles.md`.
