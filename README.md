# Microservice_2 (Student Style)

Multiple ASP.NET Core microservices with **RabbitMQ** and **API Gateway** for admin and user dashboards.

## Services

| Service     | Port | Description                          |
|------------|------|--------------------------------------|
| APIGateway | 5000 | YARP reverse proxy (use this for UI) |
| User       | 8080 | User management, JWT, admin request signup |
| ADMIN      | 5087 | Request verify/grant/revoke, product verify via RabbitMQ |
| Product    | 5088 | Products, auction scheduling         |
| Verify     | 5089 | Product verification by admin        |

## RabbitMQ (all services)

RabbitMQ is used across the application where required.

### Events (publisher → consumer)

- **request.created** (User) → **ADMIN** consumes – User publishes when admin/request signup; ADMIN creates Request record.
- **product.create** (Product) → **Verify** consumes – creates unverified product record.
- **product.verify** (ADMIN) → **Verify** consumes – marks product verified.
- **admin.unverify** (ADMIN) → **Verify** consumes – unverifies product, publishes product.unverified.
- **product.deleted** (Product) → **Verify** consumes – removes verification record when a product is deleted.
- **product.unverified** (Verify) → **Product** consumes – clears auction dates when admin unverifies a product.

### Run RabbitMQ

```bash
docker compose up -d
```

- Management UI: `http://localhost:15672` (guest / guest)

### Run all services

1. Start RabbitMQ: `docker compose up -d`
2. In separate terminals (from repo root):

```bash
dotnet run --project APIGateway/APIGateway.csproj
dotnet run --project User/User.csproj
dotnet run --project ADMIN/ADMIN.csproj
dotnet run --project Product/Product.csproj
dotnet run --project Verify/Verify.csproj
```

Gateway runs at **http://localhost:5000**. Point your dashboard/frontend to the gateway.

### Postman Testing

Import `APIGateway/Postman/MACUTION_Gateway.postman_collection.json` into Postman. All requests go through the Gateway. **Recommended flow:**

1. **User flow**: Create User (SELLER) → Login → Create Product → Get Product, Update, etc.
2. **Admin flow**: Request Signup (ADMIN) → Admin Login (existing verified admin) → Verify Request → Verify Product.
3. **Request** endpoints use `requestId` = userId of the admin candidate (from Request Signup).
4. **Product** needs `sellerToken` (from Create User/Login as SELLER).
5. **Verify** needs `adminToken` (from Admin Login). Product verify can use RabbitMQ (`/api/Request/admin/verify`) or direct (`/api/verify/product`).
6. **Clear auction** can be done by the product owner (SELLER/USER) or by an ADMIN.
7. All responses include IDs (`id`, `userId`, `productId`, `requestId`) so frontend can chain calls without extra lookups.
8. `allproducts?isVerified=true` filters server-side to reduce unnecessary data.

Seed at least one verified admin in DB for first-run (Admin Login requires a verified request).

## Dashboard endpoints (showcase)

Call via **Gateway** (`http://localhost:5000`) so admin and user dashboards get proper data.

### User dashboard

- `GET /api/user/dashboard` – **Authorize** – profile summary for current user.
- `GET /api/product/dashboard` – **Authorize (SELLER,USER)** – my product count.

### Admin dashboard

- `GET /api/admin/dashboard` – **Authorize (ADMIN)** – pending request count, verified-by-me list.
- `GET /api/Request/dashboard` – **AllowAnonymous** – pending and verified request counts.
- `GET /api/verify/dashboard` – **Authorize (ADMIN)** – verified count, verified-by-me count, unverified count.

Use the same JWT for user vs admin; role is in the token. CORS allows `localhost:5000`, `5087`, `8080`, `3000`.

## RabbitMQ RPC (request-response)

Inter-service calls use **RabbitMQ RPC** (exchange `rpc`) instead of HTTP where possible. No queue name conflicts with existing event queues.

| Queue | Consumer | Callers | Purpose |
|-------|----------|---------|---------|
| `rpc.admin.details` | ADMIN | User, Verify | Get request details by user id |
| `rpc.admin.userlist` | ADMIN | User | List requests verified by admin |
| `rpc.admin.pending` | ADMIN | User | List pending requests |
| `rpc.user.get` | User | Product, Verify | Get user by id |
| `rpc.verify.status` | Verify | Product | Get product verification status |

**Remaining HTTP:** Verify → Product (`/api/product/allproducts`) is still HTTP so the gateway auth token can be forwarded. All other cross-service calls use RPC.

## Notes

- Each service that publishes events has a `Messaging` folder (User, ADMIN, Product, Verify).
- ProductDeletedConsumer: fixes `message.productId` (event property) for correct lookup.
