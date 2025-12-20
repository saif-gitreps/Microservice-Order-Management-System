## Order Management System

A microservices-based Order Management System built with .NET 9, demonstrating event-driven architecture, distributed systems, and real-world business logic.

## Key Features

- **Repository Pattern**: All services use repository pattern for data access
- **Event-Driven Architecture**: Services communicate via RabbitMQ events
- **JWT Authentication**: Secure token-based authentication
- **Distributed Caching**: Redis for performance optimization
- **API Gateway**: Single entry point with Ocelot
- **Docker Support**: All services containerized

## Architecture Components

### 1. API Gateway (Ocelot)
- **Purpose**: Single entry point for all client requests
- **Technology**: Ocelot
- **Port**: 5010 (HTTPS), 5011 (HTTP)
- **Responsibilities**:
  - Routes requests to appropriate backend services
  - Provides unified API interface
  - Can be extended with authentication, rate limiting, load balancing

### 2. User/Auth Service
- **Purpose**: Authentication and authorization
- **Database**: PostgreSQL (userservice_db)
- **Port**: 5000 (HTTP), 5001 (HTTPS)
- **Technologies**:
  - Entity Framework Core Identity
  - JWT Bearer Authentication
  - PostgreSQL
  - Redis (caching)
- **Endpoints**:
  - `POST /api/auth/register` - Register new user
  - `POST /api/auth/login` - Login user
  - `GET /api/auth/me` - Get current user (authenticated)

### 3. Order Service
- **Purpose**: Order creation and management
- **Database**: PostgreSQL (orderservice_db)
- **Port**: 5002 (HTTP), 5003 (HTTPS)
- **Technologies**:
  - Entity Framework Core
  - Repository Pattern
  - RabbitMQ (event publishing/subscribing)
  - Redis (caching)
- **Endpoints**:
  - `POST /api/order` - Create order (authenticated)
  - `GET /api/order` - Get user orders (authenticated)
  - `GET /api/order/{id}` - Get order by ID (authenticated)
- **Events**:
  - Publishes: `OrderCreatedEvent`
  - Subscribes: `PaymentProcessedEvent`, `PaymentFailedEvent`, `InventoryReservationFailedEvent`

### 4. Inventory Service
- **Purpose**: Stock management and inventory tracking
- **Database**: PostgreSQL (inventoryservice_db)
- **Port**: 5004 (HTTP), 5005 (HTTPS)
- **Technologies**:
  - Entity Framework Core
  - Repository Pattern
  - RabbitMQ (event publishing/subscribing)
  - Redis (caching)
- **Endpoints**:
  - `GET /api/inventory/products` - Get all products
  - `GET /api/inventory/products/{id}` - Get product by ID
- **Events**:
  - Subscribes: `OrderCreatedEvent`
  - Publishes: `InventoryReservedEvent`, `InventoryReservationFailedEvent`

### 5. Payment Service
- **Purpose**: Payment processing simulation
- **Database**: PostgreSQL (paymentservice_db)
- **Port**: 5006 (HTTP), 5007 (HTTPS)
- **Technologies**:
  - Entity Framework Core
  - Repository Pattern
  - RabbitMQ (event publishing/subscribing)
- **Endpoints**: None (event-driven only)
- **Events**:
  - Subscribes: `InventoryReservedEvent`
  - Publishes: `PaymentProcessedEvent`, `PaymentFailedEvent`

### 6. Notification Service
- **Purpose**: Email/SMS notifications (simulated)
- **Database**: None (stateless)
- **Port**: 5008 (HTTP), 5009 (HTTPS)
- **Technologies**:
  - RabbitMQ (event subscribing)
- **Endpoints**: None (event-driven only)
- **Events**:
  - Subscribes: `PaymentProcessedEvent`, `PaymentFailedEvent`, `InventoryReservationFailedEvent`

## Event Flow

```
1. User creates order
   ↓
2. Order Service creates order (status: Pending)
   ↓
3. Order Service publishes OrderCreatedEvent
   ↓
4. Inventory Service receives OrderCreatedEvent
   ├─→ Checks stock availability
   ├─→ If available: Reserves inventory
   │   └─→ Publishes InventoryReservedEvent
   └─→ If unavailable: Publishes InventoryReservationFailedEvent
       └─→ Order Service updates order status to Cancelled
   ↓
5. Payment Service receives InventoryReservedEvent
   ├─→ Processes payment
   ├─→ If successful: Publishes PaymentProcessedEvent
   │   ├─→ Order Service updates order status to Confirmed
   │   └─→ Notification Service sends confirmation email
   └─→ If failed: Publishes PaymentFailedEvent
       ├─→ Order Service updates order status to Cancelled
       └─→ Notification Service sends failure email
```

## Design Patterns

### 1. Repository Pattern
- **Purpose**: Abstracts data access logic
- **Benefits**: 
  - Easier testing (mock repositories)
  - Swappable data sources
  - Separation of concerns
- **Implementation**: All services use repository interfaces and implementations

### 2. Event-Driven Architecture
- **Purpose**: Loose coupling between services
- **Benefits**:
  - Services can be developed independently
  - Scalability (services can scale independently)
  - Resilience (if one service is down, others continue)
- **Implementation**: RabbitMQ message bus with topic exchanges

### 3. API Gateway Pattern
- **Purpose**: Single entry point for clients
- **Benefits**:
  - Hides microservices complexity
  - Centralized authentication/authorization
  - Request routing and aggregation
- **Implementation**: Ocelot API Gateway

## Data Flow

### Order Creation Flow
1. Client → API Gateway → Order Service
2. Order Service validates request and creates order
3. Order Service publishes `OrderCreatedEvent` to RabbitMQ
4. Inventory Service consumes event and checks stock
5. If stock available, Inventory Service reserves items and publishes `InventoryReservedEvent`
6. Payment Service consumes `InventoryReservedEvent` and processes payment
7. Payment Service publishes `PaymentProcessedEvent` or `PaymentFailedEvent`
8. Order Service updates order status based on payment result
9. Notification Service sends email based on final order status

## Database Design

Each service has its own PostgreSQL database:
- **userservice_db**: User accounts, authentication data
- **orderservice_db**: Orders, order items
- **inventoryservice_db**: Products, inventory levels
- **paymentservice_db**: Payment transactions

This ensures:
- Data independence
- Independent scaling
- No shared database bottlenecks
- Service-specific optimizations

## Security

- **JWT Authentication**: All services validate JWT tokens
- **Token Issuer**: User Service
- **Token Validation**: Shared secret key across services
- **HTTPS**: All services support HTTPS (development uses self-signed certificates)

## Scalability

- **Horizontal Scaling**: Each service can be scaled independently
- **Load Balancing**: Kubernetes services provide load balancing
- **Caching**: Redis reduces database load
- **Async Processing**: Event-driven architecture enables async processing

## Deployment

### Docker Compose
- All services containerized
- Infrastructure services (PostgreSQL, RabbitMQ, Redis) included
- Single command deployment: `docker-compose up`