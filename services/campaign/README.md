# Campaign Service

CampaignCell — Campaign mikroservisi (.NET 8, Clean Architecture).

- `src/Campaign.Domain` — Entity'ler, enum'lar, domain kurallari (bagimsiz katman)
- `src/Campaign.Application` — CQRS Command/Query + Handler (MediatR), interface'ler, DTO'lar
- `src/Campaign.Infrastructure` — EF Core DbContext, repository'ler, MassTransit, dis servisler
- `src/Campaign.Api` — Controller/endpoint, middleware, DI (ince katman)
- `tests/Campaign.UnitTests` — xUnit + FluentAssertions + Moq

Migration + seed uygulama acilisinda otomatik calisir. Detaylar: kok `README.md` ve `Core_Principles.md`.
