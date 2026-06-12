# InventarioAPI

API REST de gestión de inventario desarrollada con ASP.NET Core (.NET 9), Entity Framework y SQL Server.

## Tecnologías

- ASP.NET Core 9
- Entity Framework Core
- SQL Server
- JWT Authentication
- Swagger / OpenAPI

## Funcionalidades

- CRUD completo de productos
- Gestión de stock y categorías
- Autenticación con JWT (en desarrollo)
- Documentación interactiva con Swagger

## Estructura del proyecto
InventarioAPI/

├── Controllers/    # Endpoints de la API

├── Models/         # Entidades de la base de datos

├── DTOs/           # Objetos de transferencia de datos

├── Data/           # DbContext y configuración de EF

└── Migrations/     # Migraciones de la base de datos

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | /api/Productos | Listar todos los productos |
| GET | /api/Productos/{id} | Obtener producto por ID |
| POST | /api/Productos | Crear producto |
| PUT | /api/Productos/{id} | Actualizar producto |
| DELETE | /api/Productos/{id} | Eliminar producto |

## Cómo ejecutar

1. Clonar el repositorio
2. Configurar la cadena de conexión en `appsettings.json`
3. Ejecutar las migraciones: `dotnet ef database update`
4. Correr la API: `dotnet run`
5. Acceder a Swagger: `http://localhost:5135/swagger`