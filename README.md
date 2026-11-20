# Canducci.EntityFrameworkCore.Relationships

![Canducci.EntityFrameworkCore.Relationships package icon](https://raw.githubusercontent.com/fulviocanducci/Canducci.EntityFrameworkCore.Relationships/docs-tests-foundation/icon.png)

[![NuGet](https://img.shields.io/nuget/v/Canducci.EntityFrameworkCore.Relationships.svg)](https://www.nuget.org/packages/Canducci.EntityFrameworkCore.Relationships)
[![Downloads](https://img.shields.io/nuget/dt/Canducci.EntityFrameworkCore.Relationships.svg)](https://www.nuget.org/packages/Canducci.EntityFrameworkCore.Relationships)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A lightweight extension library for synchronizing entity relationship collections with Entity Framework Core.

The library lets you provide the IDs that should be associated with an entity. Existing relationships that are not in the supplied ID list are removed, and missing relationships are added.

## Features

- Generic relationship synchronization using entity primary keys.
- Synchronous and asynchronous APIs.
- Extension methods for both `DbContext` and `DbSet<TEntity>`.
- Reuses related entities already tracked by the current `DbContext`.
- Accepts an already tracked collection of related entities as an alternative to supplying IDs.
- Validates that every supplied related entity is tracked by the same `DbContext`.
- Loads the selected navigation collection when it has not been loaded.
- Leaves persistence to the application: the library does not call `SaveChanges` or `SaveChangesAsync`.

## Compatibility

The project multi-targets the following frameworks and Entity Framework Core versions:

| Target framework | Entity Framework Core |
|---|---:|
| `netstandard2.0` | 3.1.32 |
| `netstandard2.1` | 5.0.17 |
| `net6.0` | 7.0.20 |
| `net8.0` | 9.0.0 |
| `net10.0` | 10.0.0 |
| `net11.0` | 11.0.0 |

**Note:** EF Core introduced skip navigations for implicit many-to-many relationships in EF Core 5.0. With EF Core 3.1, configure and use a relationship/navigation model supported by that version.

## Installation

Once the package is published, install it with:

```bash
dotnet add package Canducci.EntityFrameworkCore.Relationships
```

Add the namespace:

```csharp
using Canducci.EntityFrameworkCore.Relationships.Extensions;
```

## Example model

The following example uses an implicit many-to-many relationship (EF Core 5.0 or later):

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}
```

Configure the relationship in your context:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>()
        .HasMany(x => x.Roles)
        .WithMany(x => x.Users);
}
```

## Usage

### Using DbContext

Synchronous:

```csharp
var user = context.Users.Find(1);
var roleIds = new[] { 1, 2, 3 };

if (user != null)
{
    context.Synchronize(user, roleIds, x => x.Roles);
    context.SaveChanges();
}
```

Asynchronous:

```csharp
var user = await context.Users.FindAsync(1);
var roleIds = new[] { 1, 2, 3 };

if (user != null)
{
    await context.SynchronizeAsync(user, roleIds, x => x.Roles);
    await context.SaveChangesAsync();
}
```

### Using DbSet<TEntity>

The `DbSet<TEntity>` overloads provide the same behavior while obtaining the associated `DbContext` internally:

Synchronous:

```csharp
var user = context.Users.Find(1);
var roleIds = new[] { 1, 2, 3 };

if (user != null)
{
    context.Users.Synchronize(user, roleIds, x => x.Roles);
    context.SaveChanges();
}
```

Asynchronous:

```csharp
var user = await context.Users.FindAsync(1);
var roleIds = new[] { 1, 2, 3 };

if (user != null)
{
    await context.Users.SynchronizeAsync(user, roleIds, x => x.Roles);
    await context.SaveChangesAsync();
}
```

### Synchronizing with tracked entities

Version 1.0.2 also accepts a collection of related entities. Every item supplied to this overload must already be tracked by the same `DbContext`. If at least one item is detached, the method throws an `InvalidOperationException` before changing the navigation collection.

Synchronous:

```csharp
var user = context.Users.Find(1);
var roles = context.Roles
    .Where(x => rolesIds.Contains(x.Id))
    .ToList();

if (user != null)
{
    context.Synchronize(user, roles, x => x.Roles);
    context.SaveChanges();
}
```

Asynchronous:

```csharp
var user = await context.Users.FindAsync(1);
var roles = await context.Roles
    .Where(x => rolesIds.Contains(x.Id))
    .ToListAsync();

if (user != null)
{
    await context.SynchronizeAsync(user, roles, x => x.Roles);
    await context.SaveChangesAsync();
}
```

The same overloads are available through `DbSet<TEntity>`, for example `context.Users.Synchronize(user, roles, x => x.Roles)`.

This overload does not attach, insert, or update the supplied related entities. It only synchronizes the relationship collection. The existing overloads that receive IDs remain available and keep their original behavior.

## Synchronization behavior

Given the requested IDs:

- Relationships already present and included in the requested IDs are preserved.
- Requested IDs that are not currently related are added.
- Existing relationships whose IDs are not requested are removed.
- Duplicate IDs in the input are ignored.
- Related entities already tracked by the context are reused.
- Requested IDs that do not exist in the database cause a `KeyNotFoundException`.

The navigation collection is loaded when EF Core reports that it has not been loaded.

## Requirements and limitations

- The entity passed to the method must be tracked by the supplied `DbContext`.
- The navigation expression must refer to a direct entity property, such as `x => x.Roles`.
- The navigation property must be typed as `ICollection<TRelated>`.
- The related entity must be part of the current EF Core model.
- When using the entity-collection overload, every supplied related entity must be tracked by the same `DbContext`; detached entities cause an `InvalidOperationException`.
- The related entity must have a single-column primary key.
- `TKey` must match the CLR type of that primary key.
- The navigation collection must not be null.
- The library changes the tracked relationship collection but does not save changes automatically. Call `SaveChanges` or `SaveChangesAsync` on your context.

## License

This project is licensed under the [MIT License](LICENSE).

---

# Português

Biblioteca de extensões para sincronizar coleções de relacionamentos de entidades usando Entity Framework Core.

Informe os IDs que devem permanecer relacionados à entidade. Os relacionamentos existentes que não estiverem nessa lista serão removidos, e os relacionamentos ausentes serão adicionados.

## Recursos

- Sincronização genérica de relacionamentos usando as chaves primárias das entidades.
- APIs síncronas e assíncronas.
- Extensões para `DbContext` e `DbSet<TEntity>`.
- Reutilização de entidades relacionadas que já estejam rastreadas pelo contexto.
- Carregamento automático da coleção de navegação quando ainda não estiver carregada.
- A persistência fica sob responsabilidade da aplicação: a biblioteca não chama `SaveChanges` nem `SaveChangesAsync`.

## Exemplo rápido

Com a entidade carregada e rastreada pelo contexto:

```csharp
var user = context.Users.Find(1);
var roleIds = new[] { 1, 2, 3 };

if (user != null)
{
    context.Synchronize(user, roleIds, x => x.Roles);
    context.SaveChanges();
}
```

Também é possível chamar a extensão diretamente no `DbSet`:

```csharp
var user = context.Users.Find(1);
var roleIds = new[] { 1, 2, 3 };

if (user != null)
{
    context.Users.Synchronize(user, roleIds, x => x.Roles);
    context.SaveChanges();
}
```

## Sincronização com entidades rastreadas

**Disponível a partir da versão 1.0.2.**

Além de informar os IDs, agora é possível passar uma coleção de entidades relacionadas. Todas as entidades da coleção precisam estar previamente rastreadas pelo mesmo `DbContext`. Se algum item estiver `Detached`, será lançada uma `InvalidOperationException` antes de qualquer alteração na coleção de navegação.

Exemplo síncrono:

```csharp
var user = context.Users.Find(1);
var roleIds = new[] { 1, 2, 3 };
var roles = context.Roles
    .Where(x => roleIds.Contains(x.Id))
    .ToList();

if (user != null)
{
    context.Synchronize(user, roles, x => x.Roles);
    context.SaveChanges();
}
```

Exemplo assíncrono:

```csharp
var user = await context.Users.FindAsync(1);
var roleIds = new[] { 1, 2, 3 };
var roles = await context.Roles
    .Where(x => roleIds.Contains(x.Id))
    .ToListAsync();

if (user != null)
{
    await context.SynchronizeAsync(user, roles, x => x.Roles);
    await context.SaveChangesAsync();
}
```

A mesma sobrecarga está disponível diretamente no `DbSet<TEntity>`, por exemplo: `context.Users.Synchronize(user, roles, x => x.Roles)`.

Essa sobrecarga não anexa, insere nem atualiza as entidades recebidas. Ela apenas sincroniza os relacionamentos. As sobrecargas que recebem IDs continuam disponíveis e mantêm o comportamento anterior.

Consulte as seções em inglês acima para a matriz de compatibilidade, configuração, comportamento e limitações.
