# ApiImplementer

> **Zero-code CRUD APIs for ASP.NET Core.**  
> Decorate your model with `[AutoApi]` — get fully working REST endpoints instantly. No controllers. No boilerplate.

[![CI](https://github.com/souravdas98/nuget-AutoApi/actions/workflows/ci.yml/badge.svg)](https://github.com/souravdas98/nuget-AutoApi/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/ApiImplementer.svg?label=NuGet)](https://www.nuget.org/packages/ApiImplementer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ApiImplementer.svg)](https://www.nuget.org/packages/ApiImplementer)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Table of Contents

- [What is ApiImplementer?](#what-is-apiimplementer)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Generated Endpoints](#generated-endpoints)
- [Configuration](#configuration)
  - [Route Prefix](#route-prefix)
  - [Custom Route per Model](#custom-route-per-model)
  - [Restrict HTTP Methods](#restrict-http-methods)
  - [Swagger / OpenAPI Tags](#swagger--openapi-tags)
- [Stores](#stores)
  - [In-Memory Store (default)](#in-memory-store-default)
  - [EF Core Store](#ef-core-store)
  - [Repository Pattern (Custom Store)](#repository-pattern-custom-store)
- [Common Mistakes](#common-mistakes)
- [FAQ](#faq)
- [Contributing](#contributing)
- [License](#license)

---

## What is ApiImplementer?

ApiImplementer scans your assemblies at startup for classes decorated with `[AutoApi]` and automatically registers full **GET / GET by id / POST / PUT / DELETE** minimal-API endpoints — wired up to a data store of your choice.

```
Your model  +  [AutoApi]  →  5 REST endpoints, zero controllers
```

Works with any data source through the `IAutoApiStore<T>` interface — comes with a built-in **in-memory store** for rapid prototyping and an **EF Core store** for production.

---

## Installation

```bash
dotnet add package ApiImplementer
```

> Requires .NET 8 or later.

---

## Quick Start

**Step 1 — Decorate your model:**

```csharp
using AutoApi.Core.Attributes;

[AutoApi]
public class Product
{
    public int     Id    { get; set; }
    public string  Name  { get; set; } = "";
    public decimal Price { get; set; }
}
```

**Step 2 — Register in `Program.cs`:**

```csharp
using AutoApi.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoApi();      // scan + register stores

var app = builder.Build();

app.MapAutoApiEndpoints();          // wire up all routes

app.Run();
```

That's it. Your API is live with no extra code:

```
GET     /api/product        → list all
GET     /api/product/{id}   → get by id
POST    /api/product        → create
PUT     /api/product/{id}   → update
DELETE  /api/product/{id}   → delete
```

---

## Generated Endpoints

| Method   | Route                  | Description              | Success | Error |
|----------|------------------------|--------------------------|---------|-------|
| `GET`    | `/api/{model}`         | List all records         | `200`   | —     |
| `GET`    | `/api/{model}/{id}`    | Get single record by id  | `200`   | `404` |
| `POST`   | `/api/{model}`         | Create a new record      | `201`   | —     |
| `PUT`    | `/api/{model}/{id}`    | Update existing record   | `200`   | `404` |
| `DELETE` | `/api/{model}/{id}`    | Delete a record          | `204`   | —     |

---

## Configuration

All configuration lives inside the `AddAutoApi` options callback.

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
// Read-only endpoint — only GET and GET-by-id are registered
[AutoApi(AllowedMethods = new[] { "GET" })]
public class Report { ... }

// No delete allowed
[AutoApi(AllowedMethods = new[] { "GET", "POST", "PUT" })]
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

## Stores

### In-Memory Store (default)

When no custom store is registered, ApiImplementer uses a thread-safe in-memory store automatically.

```
✅ Zero setup       ✅ Great for prototyping and testing
❌ Not persistent   ❌ Not suitable for production
```

> Your model **must** have a `public int Id { get; set; }` property.

```csharp
[AutoApi]
public class Product
{
    public int     Id    { get; set; }  // required
    public string  Name  { get; set; } = "";
    public decimal Price { get; set; }
}
```

---

### EF Core Store

For production use, swap in the included `EFCoreAutoApiStore<T, TContext>`:

```csharp
// AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
}
```

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAutoApi(o =>
{
    o.UseStore<Product, EFCoreAutoApiStore<Product, AppDbContext>>();
});
```

```
✅ Persistent   ✅ Works with any EF Core provider (SQL Server, PostgreSQL, SQLite …)
```

---

### Repository Pattern (Custom Store)

The recommended approach for production apps. Implement `IAutoApiStore<T>` in your own repository class — you stay in full control of queries, caching, validation, and business rules.

#### Step 1 — Create the repository

```csharp
// Repositories/ProductRepository.cs
using AutoApi.Core.Abstractions;

public class ProductRepository : IAutoApiStore<Product>
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _db.Products
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(int id, Product incoming)
    {
        var existing = await _db.Products.FindAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");

        existing.Name  = incoming.Name;
        existing.Price = incoming.Price;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _db.Products.FindAsync(id);
        if (existing is not null)
        {
            _db.Products.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }
}
```

#### Step 2 — Register it in `Program.cs`

```csharp
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddAutoApi(o =>
{
    o.UseStore<Product, ProductRepository>();   // 👈 plug in your repo
});
```

#### Step 3 — That's it

`[AutoApi]` still handles all the routing. Your repository handles all the data access. No controllers needed.

```
GET  /api/product       → ProductRepository.GetAllAsync()
GET  /api/product/{id}  → ProductRepository.GetByIdAsync(id)
POST /api/product       → ProductRepository.CreateAsync(product)
PUT  /api/product/{id}  → ProductRepository.UpdateAsync(id, product)
DEL  /api/product/{id}  → ProductRepository.DeleteAsync(id)
```

#### Want an interface for testability?

```csharp
// Define your interface
public interface IProductRepository : IAutoApiStore<Product>
{
    Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal min, decimal max);
}

// Implement it
public class ProductRepository : IProductRepository
{
    // ... all IAutoApiStore<Product> methods +
    public async Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal min, decimal max)
        => await _db.Products.Where(p => p.Price >= min && p.Price <= max).ToListAsync();
}

// Register — map the store interface to the same class
builder.Services.AddAutoApi(o =>
{
    o.UseStore<Product, ProductRepository>();
});

// Also register the full interface for your own usage (e.g. in other services)
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

You can now inject `IProductRepository` anywhere in your app for custom queries while `[AutoApi]` handles the standard CRUD.

---

## Common Mistakes

#### ❌ Registering the assembly twice

```csharp
// WRONG — entry assembly is already added automatically
builder.Services.AddAutoApi(o =>
{
    o.Assemblies.Add(typeof(Product).Assembly); // duplicate! causes "Duplicate endpoint name" error
});
```

```csharp
// CORRECT — just call it without options if models are in the same project
builder.Services.AddAutoApi();
```

Only add to `Assemblies` when models live in a **different** project/assembly.

---

#### ❌ Model without an `Id` property

```csharp
// WRONG — InMemoryStore and EFCoreAutoApiStore require a public int Id
[AutoApi]
public class Product
{
    public int     ProductId { get; set; }  // ❌ must be named "Id"
    public string  Name      { get; set; } = "";
}
```

```csharp
// CORRECT
[AutoApi]
public class Product
{
    public int     Id    { get; set; }  // ✅
    public string  Name  { get; set; } = "";
}
```

> Custom repositories (`IAutoApiStore<T>`) have no such restriction — you control the key yourself.

---

#### ❌ Calling `MapAutoApiEndpoints()` before `app.Build()`

```csharp
// WRONG
var app = builder.Build();
builder.Services.AddAutoApi();   // ❌ too late — services already built
app.MapAutoApiEndpoints();
```

```csharp
// CORRECT — registration before Build(), mapping after
builder.Services.AddAutoApi();   // ✅ before Build()
var app = builder.Build();
app.MapAutoApiEndpoints();       // ✅ after Build()
```

---

#### ❌ Using `UseStore` without registering the DbContext

```csharp
// WRONG — missing AddDbContext causes runtime DI failure
builder.Services.AddAutoApi(o =>
{
    o.UseStore<Product, EFCoreAutoApiStore<Product, AppDbContext>>();
});
// ❌ AppDbContext is never registered
```

```csharp
// CORRECT — DbContext must be registered first
builder.Services.AddDbContext<AppDbContext>(opt =>     // ✅
    opt.UseSqlServer(connectionString));

builder.Services.AddAutoApi(o =>
{
    o.UseStore<Product, EFCoreAutoApiStore<Product, AppDbContext>>();
});
```

---

#### ❌ Implementing `UpdateAsync` by replacing the whole entity

```csharp
// WRONG — overwrites navigation properties, concurrency tokens, etc.
public async Task<Product> UpdateAsync(int id, Product incoming)
{
    _db.Entry(incoming).State = EntityState.Modified;  // ❌ dangerous
    await _db.SaveChangesAsync();
    return incoming;
}
```

```csharp
// CORRECT — fetch, patch only what you need, save
public async Task<Product> UpdateAsync(int id, Product incoming)
{
    var existing = await _db.Products.FindAsync(id)
        ?? throw new KeyNotFoundException($"Product {id} not found.");

    existing.Name  = incoming.Name;   // ✅ patch only the fields you own
    existing.Price = incoming.Price;

    await _db.SaveChangesAsync();
    return existing;
}
```

---

## FAQ

**Does my model need an `Id` property?**  
Only when using the built-in `InMemoryStore` or `EFCoreAutoApiStore`. Custom repositories have no restriction on key naming.

**Can I use it alongside regular controllers?**  
Yes. ApiImplementer only registers minimal-API routes — it doesn't touch MVC controllers or any other middleware.

**Can I add authentication to the generated endpoints?**  
Yes. Wrap `MapAutoApiEndpoints()` with an authenticated route group:

```csharp
app.MapGroup("/").RequireAuthorization().MapAutoApiEndpoints();
```

**Can it handle relationships / nested routes?**  
Not out of the box — `[AutoApi]` is designed for flat CRUD. For nested resources write a regular minimal-API endpoint or controller alongside it.

**What happens if two models resolve to the same route?**  
The app will throw at startup with a "Duplicate endpoint name" error. Fix it by setting a unique `Route` on the attribute:

```csharp
[AutoApi(Route = "catalog/products")]
public class Product { ... }

[AutoApi(Route = "catalog/items")]
public class Item { ... }
```

---

## Contributing

Pull requests are welcome! Please read the [PR template](.github/PULL_REQUEST_TEMPLATE/pull_request_template.md) before opening one.

1. Fork the repo
2. Create a branch: `git checkout -b feature/my-feature`
3. Make your changes with tests
4. Push and open a PR against `main`

The CI pipeline enforces **zero warnings** on every PR.

---

## License

[MIT](LICENSE) © Sourav Das

[![CI](https://github.com/souravdas98/nuget-AutoApi/actions/workflows/ci.yml/badge.svg)](https://github.com/souravdas98/nuget-AutoApi/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/ApiImplementer.svg?label=NuGet)](https://www.nuget.org/packages/ApiImplementer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ApiImplementer.svg)](https://www.nuget.org/packages/ApiImplementer)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Table of Contents

- [What is ApiImplementer?](#what-is-apiimplementer)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Generated Endpoints](#generated-endpoints)
- [Configuration](#configuration)
  - [Route Prefix](#route-prefix)
  - [Custom Route per Model](#custom-route-per-model)
  - [Restrict HTTP Methods](#restrict-http-methods)
  - [Swagger / OpenAPI Tags](#swagger--openapi-tags)
- [Stores](#stores)
  - [In-Memory Store (default)](#in-memory-store-default)
  - [EF Core Store](#ef-core-store)
  - [Repository Pattern (Custom Store)](#repository-pattern-custom-store)
- [Common Mistakes](#common-mistakes)
- [FAQ](#faq)
- [Contributing](#contributing)
- [License](#license)

---

## What is ApiImplementer?

ApiImplementer scans your assemblies at startup for classes decorated with `[ApiImplementer]` and automatically registers full **GET / GET by id / POST / PUT / DELETE** minimal-API endpoints — wired up to a data store of your choice.

```
Your model  +  [ApiImplementer]  →  5 REST endpoints, zero controllers
```

Works with any data source through the `IApiImplementerStore<T>` interface — comes with a built-in **in-memory store** for rapid prototyping and an **EF Core store** for production.

---

## Installation

```bash
dotnet add package ApiImplementer
```

> Requires .NET 8 or later.

---

## Quick Start

**Step 1 — Decorate your model:**

```csharp
using ApiImplementer.Core.Attributes;

[ApiImplementer]
public class Product
{
    public int     Id    { get; set; }
    public string  Name  { get; set; } = "";
    public decimal Price { get; set; }
}
```

**Step 2 — Register in `Program.cs`:**

```csharp
using ApiImplementer.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiImplementer();   // scan + register stores

var app = builder.Build();

app.MapApiImplementerEndpoints();       // wire up all routes

app.Run();
```

That's it. Your API is live with no extra code:

```
GET     /api/product        → list all
GET     /api/product/{id}   → get by id
POST    /api/product        → create
PUT     /api/product/{id}   → update
DELETE  /api/product/{id}   → delete
```

---

## Generated Endpoints

| Method   | Route                  | Description              | Success | Error |
|----------|------------------------|--------------------------|---------|-------|
| `GET`    | `/api/{model}`         | List all records         | `200`   | —     |
| `GET`    | `/api/{model}/{id}`    | Get single record by id  | `200`   | `404` |
| `POST`   | `/api/{model}`         | Create a new record      | `201`   | —     |
| `PUT`    | `/api/{model}/{id}`    | Update existing record   | `200`   | `404` |
| `DELETE` | `/api/{model}/{id}`    | Delete a record          | `204`   | —     |

---

## Configuration

All configuration lives inside the `AddApiImplementer` options callback.

### Route Prefix

```csharp
builder.Services.AddApiImplementer(o =>
{
    o.RoutePrefix = "v1/api";   // → /v1/api/product
    // o.RoutePrefix = "";      // → /product  (no prefix)
});
```

Default: `"api"` → `/api/{classname}`

---

### Custom Route per Model

```csharp
[ApiImplementer(Route = "catalog/products")]
public class Product { ... }
// → /catalog/products
```

---

### Restrict HTTP Methods

```csharp
// Read-only endpoint — only GET and GET-by-id are registered
[ApiImplementer(AllowedMethods = new[] { "GET" })]
public class Report { ... }

// No delete allowed
[ApiImplementer(AllowedMethods = new[] { "GET", "POST", "PUT" })]
public class Order { ... }
```

---

### Swagger / OpenAPI Tags

```csharp
[ApiImplementer(Tag = "Inventory")]
public class Product { ... }
// All product endpoints appear under the "Inventory" group in Swagger UI
```

Default: the class name.

---

## Stores

### In-Memory Store (default)

When no custom store is registered, ApiImplementer uses a thread-safe in-memory store automatically.

```
✅ Zero setup       ✅ Great for prototyping and testing
❌ Not persistent   ❌ Not suitable for production
```

> Your model **must** have a `public int Id { get; set; }` property.

```csharp
[ApiImplementer]
public class Product
{
    public int     Id    { get; set; }  // required
    public string  Name  { get; set; } = "";
    public decimal Price { get; set; }
}
```

---

### EF Core Store

For production use, swap in the included `EFCoreApiImplementerStore<T, TContext>`:

```csharp
// AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
}
```

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddApiImplementer(o =>
{
    o.UseStore<Product, EFCoreApiImplementerStore<Product, AppDbContext>>();
});
```

```
✅ Persistent   ✅ Works with any EF Core provider (SQL Server, PostgreSQL, SQLite …)
```

---

### Repository Pattern (Custom Store)

The recommended approach for production apps. Implement `IApiImplementerStore<T>` in your own repository class — you stay in full control of queries, caching, validation, and business rules.

#### Step 1 — Create the repository

```csharp
// Repositories/ProductRepository.cs
using ApiImplementer.Core.Abstractions;

public class ProductRepository : IApiImplementerStore<Product>
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _db.Products
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(int id, Product incoming)
    {
        var existing = await _db.Products.FindAsync(id)
            ?? throw new KeyNotFoundException($"Product {id} not found.");

        existing.Name  = incoming.Name;
        existing.Price = incoming.Price;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _db.Products.FindAsync(id);
        if (existing is not null)
        {
            _db.Products.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }
}
```

#### Step 2 — Register it in `Program.cs`

```csharp
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddApiImplementer(o =>
{
    o.UseStore<Product, ProductRepository>();   // 👈 plug in your repo
});
```

#### Step 3 — That's it

`[ApiImplementer]` still handles all the routing. Your repository handles all the data access. No controllers needed.

```
GET  /api/product       → ProductRepository.GetAllAsync()
GET  /api/product/{id}  → ProductRepository.GetByIdAsync(id)
POST /api/product       → ProductRepository.CreateAsync(product)
PUT  /api/product/{id}  → ProductRepository.UpdateAsync(id, product)
DEL  /api/product/{id}  → ProductRepository.DeleteAsync(id)
```

#### Want an interface for testability?

```csharp
// Define your interface
public interface IProductRepository : IApiImplementerStore<Product>
{
    Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal min, decimal max);
}

// Implement it
public class ProductRepository : IProductRepository
{
    // ... all IApiImplementerStore<Product> methods +
    public async Task<IEnumerable<Product>> GetByPriceRangeAsync(decimal min, decimal max)
        => await _db.Products.Where(p => p.Price >= min && p.Price <= max).ToListAsync();
}

// Register — map the store interface to the same class
builder.Services.AddApiImplementer(o =>
{
    o.UseStore<Product, ProductRepository>();
});

// Also register the full interface for your own usage (e.g. in other services)
builder.Services.AddScoped<IProductRepository, ProductRepository>();
```

You can now inject `IProductRepository` anywhere in your app for custom queries while `[ApiImplementer]` handles the standard CRUD.

---

## Common Mistakes

#### ❌ Registering the assembly twice

```csharp
// WRONG — entry assembly is already added automatically
builder.Services.AddApiImplementer(o =>
{
    o.Assemblies.Add(typeof(Product).Assembly); // duplicate! causes "Duplicate endpoint name" error
});
```

```csharp
// CORRECT — just call it without options if models are in the same project
builder.Services.AddApiImplementer();
```

Only add to `Assemblies` when models live in a **different** project/assembly.

---

#### ❌ Model without an `Id` property

```csharp
// WRONG — InMemoryStore and EFCoreStore require a public int Id
[ApiImplementer]
public class Product
{
    public int     ProductId { get; set; }  // ❌ must be named "Id"
    public string  Name      { get; set; } = "";
}
```

```csharp
// CORRECT
[ApiImplementer]
public class Product
{
    public int     Id    { get; set; }  // ✅
    public string  Name  { get; set; } = "";
}
```

> Custom repositories (`IApiImplementerStore<T>`) have no such restriction — you control the key yourself.

---

#### ❌ Calling `MapApiImplementerEndpoints()` before `app.Build()`

```csharp
// WRONG
var app = builder.Build();
builder.Services.AddApiImplementer();   // ❌ too late — services already built
app.MapApiImplementerEndpoints();
```

```csharp
// CORRECT — registration before Build(), mapping after
builder.Services.AddApiImplementer();   // ✅ before Build()
var app = builder.Build();
app.MapApiImplementerEndpoints();       // ✅ after Build()
```

---

#### ❌ Using `UseStore` without registering the DbContext

```csharp
// WRONG — missing AddDbContext causes runtime DI failure
builder.Services.AddApiImplementer(o =>
{
    o.UseStore<Product, EFCoreApiImplementerStore<Product, AppDbContext>>();
});
// ❌ AppDbContext is never registered
```

```csharp
// CORRECT — DbContext must be registered first
builder.Services.AddDbContext<AppDbContext>(opt =>     // ✅
    opt.UseSqlServer(connectionString));

builder.Services.AddApiImplementer(o =>
{
    o.UseStore<Product, EFCoreApiImplementerStore<Product, AppDbContext>>();
});
```

---

#### ❌ Implementing `UpdateAsync` by replacing the whole entity

```csharp
// WRONG — overwrites navigation properties, concurrency tokens, etc.
public async Task<Product> UpdateAsync(int id, Product incoming)
{
    _db.Entry(incoming).State = EntityState.Modified;  // ❌ dangerous
    await _db.SaveChangesAsync();
    return incoming;
}
```

```csharp
// CORRECT — fetch, patch only what you need, save
public async Task<Product> UpdateAsync(int id, Product incoming)
{
    var existing = await _db.Products.FindAsync(id)
        ?? throw new KeyNotFoundException($"Product {id} not found.");

    existing.Name  = incoming.Name;   // ✅ patch only the fields you own
    existing.Price = incoming.Price;

    await _db.SaveChangesAsync();
    return existing;
}
```

---

## FAQ

**Does my model need an `Id` property?**  
Only when using the built-in `InMemoryStore` or `EFCoreApiImplementerStore`. Custom repositories have no restriction on key naming.

**Can I use it alongside regular controllers?**  
Yes. ApiImplementer only registers minimal-API routes — it doesn't touch MVC controllers or any other middleware.

**Can I add authentication to the generated endpoints?**  
Yes. Wrap `MapApiImplementerEndpoints()` with an authenticated route group:

```csharp
app.MapGroup("/").RequireAuthorization().MapApiImplementerEndpoints();
```

**Can it handle relationships / nested routes?**  
Not out of the box — `[ApiImplementer]` is designed for flat CRUD. For nested resources write a regular minimal-API endpoint or controller alongside it.

**What happens if two models resolve to the same route?**  
ApiImplementer will throw at startup with a "Duplicate endpoint name" error. Fix it by setting a unique `Route` on one of the attributes:

```csharp
[ApiImplementer(Route = "catalog/products")]
public class Product { ... }

[ApiImplementer(Route = "catalog/items")]
public class Item { ... }
```

---

## Contributing

Pull requests are welcome! Please read the [PR template](.github/PULL_REQUEST_TEMPLATE/pull_request_template.md) before opening one.

1. Fork the repo
2. Create a branch: `git checkout -b feature/my-feature`
3. Make your changes with tests
4. Push and open a PR against `main`

The CI pipeline enforces **zero warnings** on every PR.

---

## License

[MIT](LICENSE) © Sourav Das


[![CI](https://github.com/souravdas98/nuget-AutoApi/actions/workflows/ci.yml/badge.svg)](https://github.com/souravdas98/nuget-AutoApi/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AutoApi.svg?label=NuGet)](https://www.nuget.org/packages/AutoApi)
[![NuGet Downloads](https://img.shields.io/nuget/dt/AutoApi.svg)](https://www.nuget.org/packages/AutoApi)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## Table of Contents

- [AutoApi](#autoapi)
  - [Table of Contents](#table-of-contents)
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
dotnet add package ApiImplementer
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
