# Changelog

Format inspired by [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

**Versioning status:** the repository has a single commit (`eccc045`, 2026-11-09, "First commit"), with no tags and no version number defined in the code. There are therefore no versions or earlier changes to document; everything below belongs to the first delivery. Dates are given only when they appear in the repository.

## [Unversioned] — 2026-11-09

### Added

**Product.Api**
- 9 endpoints under `/api/products`: list, get by id, create, update, delete, increase stock, decrease stock, search by name and search by stock range.
- Cursor (*keyset*) pagination on listings and searches.
- JWT Bearer authentication and role-based authorization (`Admin` on `POST` and `DELETE`).
- Errors as `ProblemDetails` with `code` and `traceId`; global exception handling.
- Swagger with a Bearer scheme (`Development` only).

**App.IdentityServer**
- `POST /api/auth/login` on top of Duende IdentityServer + ASP.NET Identity (seeded `Admin` and `Manager` users, lockout after 5 failures).

**Persistence**
- Initial migrations: `InitialIdentity` (2026-11-09) for the IdentityServer and `InitialCreate` (2026-11-09) for products; both applied on startup.
- Seed of 10 products with stock and initial movements.
- Stock movement log (`StockMovement`) and soft delete of products.

**Operations and quality**
- Dockerfiles, `docker-compose.yml` (Postgres, IdentityServer, Product.Api) and `.env.example`.
- `scripts/smoke-test.ps1` for end-to-end verification.
- 139 unit tests (xUnit v3 + Moq) covering the API, handlers, validators, behaviors, mapping and error handling.
- README with run instructions, and this documentation in `docs/`.

### Architecture

Initial architecture: Clean Architecture + CQRS in `Product.*`, a separate IdentityServer, one PostgreSQL database per service. Described in [Architecture_EN.md](Architecture_EN.md). No architectural changes are recorded after the first delivery.

### Fixed / Improved

No information available: the repository has no history of fixes or improvements.

### Breaking changes

None identifiable: there is no previous version.
