# AutoApi

> **Zero-code CRUD APIs for ASP.NET Core.**  
> Decorate your model with `[AutoApi]` — get fully working REST endpoints instantly. No controllers. No boilerplate.

[![CI](https://github.com/souravdas98/nuget-AutoApi/actions/workflows/ci.yml/badge.svg)](https://github.com/souravdas98/nuget-AutoApi/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AutoApi.svg?label=NuGet)](https://www.nuget.org/packages/AutoApi)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AutoApi.svg)](https://www.nuget.org/packages/AutoApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Table of Contents

- [What is AutoApi?](#what-is-autoapi)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Generated Endpoints](#generated-endpoints)
- [Configuration](#configuration)
  - [Route Prefix](#route-prefix)
  - [Custom Route per Model](#custom-route-per-model)
  - [Restrict HTTP Methods](#restrict-http-methods)
  - [Swagger / OpenAPI Tags](#swagger--openapi-tags)
  - [Scan Multiple Assemblies](#scan-multiple-assemblies)
- [Stores](#stores)
  - [In-Memory Store (default)](#in-memory-store-default)
  - [EF Core Store](#ef-core-store)
  - [Custom Store](#custom-store)
- [Full Example](#full-example)
- [FAQ](#faq)
- [Contributing](#contributing)
- [License](#license)

---

## What is AutoApi?

AutoApi scans your assemblies at startup for classes decorated with `[AutoApi]` and automatically registers full **GET / GET by id / POST / PUT / DELETE** minimal-API endpoints — wired up to a data store of your choice.

```
Your model  +  [AutoApi]  →  5 REST endpoints, zero controllers
```

Works with any data source through the `IAutoApiStore<T>` interface — comes with a built-in **in-memory store** for rapid prototyping and an **EF Core store** for production.

---

## Installation

```bash
dotnet add package AutoApi
```

> Requires .NET 8 or later.

---

## Quick Start

**1. Decorate your model:**

```csharp
using AutoApi.Core.Attributes;

[AutoApi]
public class Product
{
    public int    Id    { get; set; }
    public string Name  { get; set; } = "";
    public decimal Price { get; set; }
}
```

**2. Register in `Program.cs`:**

```csharp
using AutoApi.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoApi();          // scan + register stores

var app = builder.Build();

app.MapAutoApiEndpoints();              // wire up all routes

app.Run();
```

That's it. Your API is live:

```
GET     /api/product        → list all products
GET     /api/product/{id}   → get by id
POST    /api/product        → create
PUT     /api/product/{id}   → update
DELETE  /api/product/{id}   → delete
```

---

## Generated Endpoints

| Method   | Route              | Description              | Success | Not Found |
|----------|--------------------|--------------------------|---------|-----------|
| `GET`    | `/api/{model}`     | List all records         | `200`   | —         |
| `GET`    | `/api/{model}/{id}`| Get single record by id  | `200`   | `404`     |
| `POST`   | `/api/{model}`     | Create a new record      | `201`   | —         |
| `PUT`    | `/api/{model}/{id}`| Update existing record   | `200`   | `404`     |
| `DELETE` | `/api/{model}/{id}`| Delete a record          | `204`   | —         |

---

## Configuration

All configuration is done via the `AddAutoApi` options callback.

### Route Prefix

```csharp
builder.Services.AddAutoApi(o =>
{
    o.RoutePrefix = "v1/api";   // → /v1/api/product
    // o.RoutePrefix = "";      // → /product  (no prefix)
});
```

Default: `"api"` → `/api/{classname}`

---

### Custom Route per Model

```csharp
[AutoApi(Route = "catalog/products")]
public class Product { ... }
// → /catalog/products
```

---

### Restrict HTTP Methods

```csharp
[AutoApi(AllowedMethods = new[] { "GET" })]
public class ReadOnlyReport { ... }
// Only GET / GET-by-id are registered
```

```csharp
[AutoApi(AllowedMethods = new[] { "GET", "POST" })]
public class Order { ... }
```

---

### Swagger / OpenAPI Tags

```csharp
[AutoApi(Tag = "Inventory")]
public class Product { ... }
// All product endpoints appear under the "Inventory" group in Swagger UI
```

Default: the class name.

---

### Scan Multiple Assemblies

```csharp
builder.Services.AddAutoApi(o =>
{
    o.Assemblies.Add(typeof(Product).Assembly);
    o.Assemblies.Add(typeof(Order).Assembly);
});
```

By default the entry assembly is scanned automatically.

---

## Stores

### In-Memory Store (default)

When no custom store is registered, AutoApi uses a thread-safe in-memory store automatically.

```
✅ Zero setup       ✅ Great for prototyping / testing
❌ Not persistent   ❌ Not suitable for production
```

> Your model **must** have a `public int Id { get; set; }` property.

---

### EF Core Store

For production use, swap in the included `EFCoreAutoApiStore<T, TContext>`:

```csharp
// 1. Your DbContext
public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
}

// 2. Register in Program.cs
builder.Services.AddAutoApi(o =>
{
    o.UseStore<Product, EFCoreAutoApiStore<Product, AppDbContext>>();
});

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=app.db"));
```

```
✅ Persistent   ✅ Works with any EF Core provider (SQL Server, Postgres, SQLite, etc.)
```

---

### Custom Store

Implement `IAutoApiStore<T>` for any data source (Dapper, MongoDB, Redis, external API, …):

```csharp
public class ProductDapperStore : IAutoApiStore<Product>
{
    private readonly IDbConnection _db;
    public ProductDapperStore(IDbConnection db) => _db = db;

    public Task<IEnumerable<Product>> GetAllAsync() =>
        _db.QueryAsync<Product>("SELECT * FROM Products");

    public Task<Product?> GetByIdAsync(int id) =>
        _db.QueryFirstOrDefaultAsync<Product>(
            "SELECT * FROM Products WHERE Id = @id", new { id });

    public async Task<Product> CreateAsync(Product item) { /* ... */ }
    public async Task<Product> UpdateAsync(int id, Product item) { /* ... */ }
    public async Task DeleteAsync(int id) { /* ... */ }
}

// Register
builder.Services.AddAutoApi(o =>
    o.UseStore<Product, ProductDapperStore>());
```

---

## Full Example

```csharp
// Models/Product.cs
using AutoApi.Core.Attributes;

[AutoApi(Route = "products", Tag = "Catalog", AllowedMethods = new[] { "GET", "POST", "PUT", "DELETE" })]
public class Product
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = "";
    public decimal Price    { get; set; }
    public int     Stock    { get; set; }
}

[AutoApi(AllowedMethods = new[] { "GET" })]   // read-only
public class Category
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";
}
```

```csharp
// Program.cs
using AutoApi.Core.Extensions;
using AutoApi.EFCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=shop.db"));

builder.Services.AddAutoApi(o =>
{
    o.RoutePrefix = "api/v1";
    o.UseStore<Product,  EFCoreAutoApiStore<Product,  AppDbContext>>();
    o.UseStore<Category, EFCoreAutoApiStore<Category, AppDbContext>>();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapAutoApiEndpoints();

app.Run();
```

**Resulting routes:**

```
GET    /api/v1/products         GET    /api/v1/category
GET    /api/v1/products/{id}    GET    /api/v1/category/{id}
POST   /api/v1/products
PUT    /api/v1/products/{id}
DELETE /api/v1/products/{id}
```

---

## FAQ

**Does my model need an `Id` property?**  
Yes — both the built-in `InMemoryStore` and `EFCoreAutoApiStore` key records by `public int Id`. Custom stores have no such restriction.

**Can I use it alongside regular controllers?**  
Absolutely. AutoApi only registers minimal-API routes; it doesn't interfere with controllers or other middleware.

**Can I add auth / rate limiting to the generated endpoints?**  
Yes — wrap `MapAutoApiEndpoints()` with a route group that has auth policies applied, or extend `EndpointBuilder` with your own middleware pipeline.

**Does it support Swagger/OpenAPI?**  
Yes. Every endpoint is tagged (via `WithTags`) and named (`WithName`). Add `AddEndpointsApiExplorer()` + `AddSwaggerGen()` and they appear automatically.

**What happens if two models have the same class name?**  
They'll resolve to the same route, which will cause a conflict at startup. Use the `Route` property on `[AutoApi]` to give each a unique path.

---

## Contributing

Pull requests are welcome! Please read the [PR template](.github/PULL_REQUEST_TEMPLATE/pull_request_template.md) before opening one.

1. Fork the repo
2. Create a branch: `git checkout -b feature/my-feature`
3. Make your changes with tests
4. Push and open a PR against `main`

The CI pipeline enforces **zero warnings** and **full test coverage** on every PR.

---

## License

[MIT](LICENSE) © Sourav Das
