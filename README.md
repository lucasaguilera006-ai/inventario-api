# InventarioAPI

REST API para gestión de inventario desarrollada con ASP.NET Core (.NET 9), Entity Framework Core y autenticación JWT.

## Tecnologías

- ASP.NET Core .NET 9
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- Swagger / OpenAPI

## Endpoints

### Auth
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | /api/auth/register | Registrar usuario |
| POST | /api/auth/login | Obtener token JWT |

### Productos (requieren token)
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | /api/productos | Listar productos |
| GET | /api/productos/{id} | Obtener producto |
| POST | /api/productos | Crear producto |
| PUT | /api/productos/{id} | Actualizar producto |
| DELETE | /api/productos/{id} | Eliminar producto |

## Cómo usar

**1. Registrar usuario**
```json
POST /api/auth/register
{
  "username": "lucas",
  "password": "tuPassword"
}
```

**2. Obtener token**
```json
POST /api/auth/login
{
  "username": "lucas",
  "password": "tuPassword"
}
// Respuesta: { "token": "eyJ..." }
```

**3. Usar token en requests**
```
Authorization: Bearer eyJ...
```

## Configuración local

```bash
git clone https://github.com/lucasaguilera006-ai/inventario-api
cd inventario-api
dotnet restore
dotnet ef database update
dotnet run
```

Swagger disponible en `http://localhost:5135/swagger`