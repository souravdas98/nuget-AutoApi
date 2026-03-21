# Changelog

All notable changes to **AutoApi** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- (nothing yet)

---

## [1.0.0] - 2026-03-21

### Added
- `[AutoApi]` attribute — decorate any model class to generate full CRUD endpoints automatically
- `AutoApiOptions` — configure global route prefix, assemblies to scan, and per-model store overrides
- `IAutoApiStore<T>` — storage contract; bring any data source (EF Core, Dapper, MongoDB, …)
- `InMemoryStore<T>` — zero-config thread-safe in-memory store for development and testing
- `EFCoreAutoApiStore<T, TContext>` — production-ready EF Core backed store
- `AddAutoApi()` service collection extension — registers stores and options
- `MapAutoApiEndpoints()` endpoint route builder extension — wires all routes at startup
- `AllowedMethods` on `[AutoApi]` — restrict which HTTP verbs are exposed per model
- `Route` on `[AutoApi]` — override the auto-generated route per model
- `Tag` on `[AutoApi]` — control Swagger/OpenAPI grouping per model
- Multi-assembly scanning via `AutoApiOptions.Assemblies`
- GitHub Actions CI pipeline (zero-warning build, test, coverage)
- GitHub Actions NuGet release pipeline (tag-triggered + manual dispatch with dry-run)
- PR template

---

<!-- Links -->
[Unreleased]: https://github.com/souravdas98/nuget-AutoApi/compare/v1.0.0...HEAD
[1.0.0]:      https://github.com/souravdas98/nuget-AutoApi/releases/tag/v1.0.0
