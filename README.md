# Job Finder ADS

Aplicativo desktop completo para Windows que coleta, pontua e acompanha vagas de tecnologia aderentes ao perfil informado no projeto.

## Arquitetura

- `frontend/`: cliente Flutter Desktop, Material 3, Riverpod e GoRouter.
- `src/JobFinder.Domain`: entidades e regras de dominio.
- `src/JobFinder.Application`: casos de uso, filtros, score e contratos.
- `src/JobFinder.Infrastructure`: provedores de fontes e extensoes de automacao.
- `src/JobFinder.Persistence`: EF Core, SQLite e migrations.
- `src/JobFinder.API`: REST API e worker hospedado.
- `src/JobFinder.Worker`: coletor em processo separado.
- `tests/JobFinder.Tests`: testes xUnit e EF Core.

O prompt solicita .NET 10, mas o ambiente disponivel possui apenas o SDK .NET 9. A solucao usa `net9.0` e esta pronta para retargeting quando o SDK 10 estiver instalado.

Backend Clean Architecture:

- `JobFinder.Domain`: job and candidate-action entities.
- `JobFinder.Application`: repository/source contracts, filtering, scoring, and application services.
- `JobFinder.Infrastructure`: sample feed providers, source settings, and the extension point for public-page/Playwright readers.
- `JobFinder.Persistence`: EF Core SQLite persistence with versioned migrations.
- `JobFinder.API`: REST API for the desktop client.
- `JobFinder.Worker`: periodic collection service with bounded retries.
- `JobFinder.Tests`: focused xUnit tests.

## Run locally

Requires the .NET 9 SDK. From the repository root:

```powershell
dotnet restore JobFinder.sln
dotnet build JobFinder.sln
dotnet test JobFinder.sln
dotnet run --project src/JobFinder.API
```

The API listens on the URL printed at startup (typically `http://localhost:5xxx`). On first run it creates `jobfinder.db` in the API content directory and applies the EF Core migrations. The Gupy provider collects real public listings; the other source entries seed demonstration records with `example.com` links until they are replaced. The API hosts the collector by default; the standalone `dotnet run --project src/JobFinder.Worker` is available for split deployments. When using the separate worker, set `JobFinder:EnableBackgroundCollection` to `false` for the API, and set the same absolute `ConnectionStrings__JobFinder` value for both processes so the client sees collected listings.

## API

All application routes are under `/api`; `/health` is an unauthenticated liveness check.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/dashboard` | Counts, average score, sources, latest update |
| GET | `/api/jobs` | Paginated jobs; query `q`, `category`, `workMode`, `company`, `location`, `minimumScore`, `requiresSpanish`, `inclusiveEmployer`, `favoritesOnly`, `includeHidden`, `status`, `page`, `pageSize` |
| GET | `/api/favorites` | Convenience endpoint for favorite jobs |
| GET | `/api/jobs/{id}` | Job details and compatibility reasons |
| GET | `/api/jobs/{id}/actions` | Candidate action history |
| PUT | `/api/jobs/{id}/favorite` | Body: `{"favorite":true}` |
| PUT | `/api/jobs/{id}/status` | Body: `{"status":"Applied"}`; values are `Unseen`, `Interesting`, `Favorited`, `Applied`, `InReview`, `Interview`, `Offer`, `Rejected`, `Closed` |
| PUT | `/api/jobs/{id}/hidden` | Body: `{"hidden":true}` |
| POST | `/api/jobs/{id}/open` | Records an open action and returns the original URL for the client to launch |
| GET | `/api/sources` | Monitored source list and enabled state |
| PUT | `/api/sources/{name}/enabled` | Body: `{"enabled":false}` |
| GET / PUT | `/api/settings` | Read/update refresh interval, record limit, and theme |
| GET | `/api/statistics` | Grouped source, category, modality, company, and month counts |
| POST | `/api/collect` | Trigger a collection manually |
| GET | `/api/news` | Novidades desde a última visita, incluindo mudanças de score |
| POST | `/api/news/visit` | Persiste a data atual como última visita |
| PUT | `/api/news/jobs/{id}/viewed` | Marca uma vaga como visualizada |

JSON enums are serialized as strings. Lists are paginated (default 50, maximum 200). CORS origins, database, refresh interval, and record limit are configurable through `appsettings.json` or environment variables such as `ConnectionStrings__JobFinder` and `JobFinder__RefreshIntervalMinutes`. Settings changed through the API are in-memory and reset on process restart; the refresh interval and record limit are initialized from configuration. The API applies a fixed-window rate limit (120 requests/minute) and short response caching on dashboard/statistics.

## Sources and scheduling

Gupy uses the public Gupy job-search API and stores real listing URLs. CIEE, Nube, Remotar, Trampos, and Vagas.com.br still use safe, deterministic demo data. `IJobSourceProvider` is the ingestion contract. `PlaywrightSourceAdapter` and `IPublicJobPageReader` are extension points for a compliant public-page reader; no browser, Playwright package, login automation, or unsupported scraping is bundled. Prefer official APIs and respect each site's terms and rate limits when adding adapters.

The hosted collector reads `JobFinder:RefreshIntervalMinutes` (5–1440 minutes), collects on startup, and uses three exponential-backoff attempts per cycle. EF Core migrations initialize and evolve the SQLite database.

## Containers

Build/run the API with `docker build -t jobfinder-api .` and `docker run --rm -p 8080:8080 -v jobfinder-data:/app/data jobfinder-api`. The API collector is sufficient for a single-container deployment. To use the separate collector, configure the API with `JobFinder__EnableBackgroundCollection=false`, build it with `docker build -f Dockerfile.worker -t jobfinder-worker .`, and mount the same named data volume at `/app/data` in both containers.

## Frontend

```powershell
cd frontend
flutter pub get
flutter analyze
flutter test
flutter run -d windows --dart-define=JOB_FINDER_API_URL=http://localhost:5000
```

Consulte [frontend/README.md](frontend/README.md) para requisitos Windows, build e configuracao.

## Migrations

Na inicializacao, a API executa `Database.MigrateAsync()`. Para criar ou aplicar migrations manualmente:

```powershell
dotnet ef migrations add NomeDaMigracao `
  --project src/JobFinder.Persistence/JobFinder.Persistence.csproj `
  --startup-project src/JobFinder.API/JobFinder.API.csproj `
  --context JobFinderDbContext

dotnet ef database update `
  --project src/JobFinder.Persistence/JobFinder.Persistence.csproj `
  --startup-project src/JobFinder.API/JobFinder.API.csproj `
  --context JobFinderDbContext
```

## Fontes

A fonte Gupy usa a API publica de busca `https://portal.gupy.io/api/job-search/jobs` para coletar vagas reais, com titulo, empresa, localidade, modalidade, descricao, data de publicacao e URL HTTPS do dominio Gupy. As consultas procuram vagas relacionadas a estagio, QA, desenvolvimento, dados, DevOps, suporte e seguranca, respeitam limite de resultados por consulta e fazem uma pausa curta entre paginas de busca.

A fonte Nube usa o endpoint publico `/api/portal/buscar_listagem_vagas` e o endpoint publico de detalhes para coletar vagas reais. O provider captura titulo, localidade, modalidade, bolsa, beneficios, descricao e URL `nube.com.br`, remove vagas duplicadas por URL e por titulo+empresa e elimina registros demonstrativos antigos da fonte.

A fonte CIEE usa a vitrine publica de processos seletivos em `api-pp.ciee.org.br`. O provider coleta editais abertos, detalhes, cursos, localidades e requisitos, persistindo URLs reais da vitrine CIEE. Vagas mockadas antigas da CIEE são removidas automaticamente na coleta.

A fonte Trampos usa a API publica `/api/oportunidades` e os detalhes em `/api/v2/opportunities/{id}`. O provider coleta titulo, empresa, localidade, modalidade, salario, descricao, requisitos e URL real da Trampos. Registros mockados antigos e duplicados da Trampos são removidos automaticamente.

A fonte Vagas.com.br usa as páginas públicas de pesquisa e os anúncios individuais em `/vagas/v{id}/...`. O provider coleta título, empresa, localização, tipo de contratação, faixa salarial e descrição, removendo registros mockados e duplicados antes da persistência.

Os demais fontes embarcados (Remotar) continuam usando dados demonstrativos deterministas para exercitar o fluxo sem credenciais, login ou scraping nao autorizado. Eles nao devem ser interpretados como vagas reais. Para producao, implemente `IJobSourceProvider` ou `IPublicJobPageReader` com APIs oficiais ou leitores Playwright que respeitem os termos de uso, limites de requisicoes e politicas de privacidade de cada plataforma.
