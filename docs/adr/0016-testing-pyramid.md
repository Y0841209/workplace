# ADR-0016: Testing Pyramid with Contract Tests

## Status
Accepted

## Context
Testing requirements:
- High confidence in business logic (reservations, conflicts, permissions)
- Fast feedback for developers
- CI/CD pipeline integration
- Contract stability between frontend/backend
- Security testing in pipeline
- Load testing for peak concurrency

## Decision
Implement **Testing Pyramid** with emphasis on unit/integration, plus contract, E2E, security, and load tests.

### Test Distribution

| Level | Target % | Tools | Scope |
|-------|----------|-------|-------|
| **Unit** | 70% | xUnit, Moq, Vitest, RTL | Domain logic, Use Cases, Components |
| **Integration** | 20% | Testcontainers, WebApplicationFactory | DB, Auth, External APIs, Repositories |
| **Contract** | 5% | Pact / Specmatic | API consumer/provider contracts |
| **E2E** | 3% | Playwright | Critical user journeys |
| **Security** | 1% | CodeQL, Dependabot, OWASP ZAP | SAST, SCA, DAST |
| **Load** | 1% | k6 | Peak concurrency scenarios |

### Backend Testing

```csharp
// Unit Test - Domain Logic
public class ReservationTests
{
    [Fact]
    public void Create_ShouldFail_WhenDurationLessThanOneHour()
    {
        var result = Reservation.Create(
            resourceId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            date: DateOnly.FromDateTime(DateTime.Today),
            start: new TimeOnly(10, 0),
            end: new TimeOnly(10, 30), // 30 min - invalid
            createdBy: Guid.NewGuid()
        );
        
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("minimum 1 hour");
    }
}

// Unit Test - Use Case Handler
public class CreateReservationHandlerTests
{
    private readonly Mock<IReservationRepository> _repo;
    private readonly Mock<IPolicyService> _policy;
    private readonly Mock<IAvailabilityService> _availability;
    private readonly CreateReservationHandler _handler;

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenResourceUnavailable()
    {
        // Arrange
        _availability.Setup(x => x.HasAvailabilityAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(validCommand, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
    }
}

// Integration Test - With Testcontainers
public class ReservationIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly WebApplicationFactory<Program> _factory;

    public async Task InitializeAsync()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("booking_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
        await _dbContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DB connection with test container
                });
            });
    }

    [Fact]
    public async Task CreateReservation_ShouldPersistToDatabase()
    {
        var client = _factory.CreateClient();
        // ... test full HTTP flow
    }
}
```

### Frontend Testing

```tsx
// Unit Test - Component (Vitest + RTL)
describe('ReservationForm', () => {
  it('should show error when end time before start time', async () => {
    render(<ReservationForm />);
    
    await userEvent.selectOptions(screen.getByLabelText('Start'), '10:00');
    await userEvent.selectOptions(screen.getByLabelText('End'), '09:00');
    
    await expect(screen.getByText('End time must be after start time')).toBeInTheDocument();
  });
});

// Hook Test
describe('useReservations', () => {
  it('should fetch and cache reservations', async () => {
    const wrapper = createWrapper();
    const { result } = renderHook(() => useReservations(), { wrapper });
    
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data).toHaveLength(3);
  });
});
```

### Contract Testing (Pact)

```typescript
// Frontend - Consumer Test (Pact)
describe('Reservations API Contract', () => {
  const provider = new Pact({
    consumer: 'BookingFrontend',
    provider: 'BookingApi',
    port: 1234,
  });

  beforeAll(() => provider.setup());
  afterAll(() => provider.finalize());

  it('should return available resources', async () => {
    await provider.addInteraction({
      state: 'resources exist',
      uponReceiving: 'a request for available resources',
      withRequest: {
        method: 'GET',
        path: '/api/v1/availability',
        query: 'type=OPEN_WORKSPACE&date=2026-01-15&start=09:00&end=11:00',
        headers: { Accept: 'application/json' },
      },
      willRespondWith: {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
        body: eachLike({
          id: uuid(),
          code: 'P03-OA-001',
          name: 'Oficina abierta P03 001',
          type: 'OPEN_WORKSPACE',
        }),
      },
    });

    const resources = await api.resources.getAvailability({ type: 'OPEN_WORKSPACE', ... });
    expect(resources).toHaveLength(1);
  });
});
```

```csharp
// Backend - Provider Verification (PactNet)
[Fact]
public void VerifyProvider()
{
    var config = new PactVerifierConfig
    {
        ProviderVersion = "1.0.0",
        PublishVerificationResults = true,
        PactBrokerUrl = "https://pact-broker.company.com",
    };

    PactVerifier.Verify(config, (verifier) =>
    {
        verifier
            .ServiceProvider("BookingApi", "https://api-staging.company.com")
            .WithHttpClientFactory(() => new HttpClient())
            .WithStateHandlers(new Dictionary<string, Action>
            {
                ["resources exist"] = () => SeedTestData()
            });
    });
}
```

### CI/CD Pipeline

```yaml
# .github/actions/test.yml
jobs:
  unit-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - run: dotnet test src/backend/tests --configuration Release --collect:"XPlat Code Coverage"

  unit-frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
      - run: npm ci
      - run: npm run test:unit -- --coverage

  integration:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16
        env: { POSTGRES_DB: test, POSTGRES_PASSWORD: test }
        ports: [5432:5432]
    steps:
      - uses: actions/checkout@v4
      - run: dotnet test src/backend/tests --filter "Category=Integration"

  contract:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: npm run test:contract

  e2e:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: microsoft/playwright-github-action@v1
      - run: npm run test:e2e

  security-sast:
    runs-on: ubuntu-latest
    steps:
      - uses: github/codeql-action/analyze@v3

  security-sca:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/setup-dotnet@v4
      - run: dotnet list package --vulnerable --include-transitive

  security-dast:
    runs-on: ubuntu-latest
    steps:
      - uses: zaproxy/action-full-scan@v0.5.0
        with:
          target: 'https://staging.booking.company.com'

  load:
    runs-on: ubuntu-latest
    if: github.event_name == 'schedule' || github.event_name == 'workflow_dispatch'
    steps:
      - uses: actions/checkout@v4
      - run: k6 run load/smoke.js
```

## Consequences

### Positive
- **Confidence**: Multi-layer testing catches different defect types
- **Fast Feedback**: Unit tests run in seconds, integration in minutes
- **Contract Safety**: Frontend/backend changes detected before deploy
- **Security**: Automated SAST/SCA/DAST in pipeline
- **Performance**: Load tests prevent regressions

### Negative
- **Maintenance**: Test code requires maintenance alongside production code
- **Flakiness**: Integration/E2E tests can be flaky (mitigate with retries)
- **CI Time**: Full pipeline ~15-20 minutes

### Neutral
- Testcontainers requires Docker in CI (GitHub Actions supports)
- Pact requires Pact Broker for team sharing (or file-based)

## Alternatives Considered

1. **Only Unit Tests**
   - Rejected: Misses integration issues, DB constraints, API contracts

2. **Only E2E Tests**
   - Rejected: Slow, brittle, hard to debug, poor coverage granularity

3. **No Contract Tests**
   - Rejected: Frontend/backend drift causes runtime errors

## References
- [Testing Pyramid](https://martinfowler.com/articles/practical-test-pyramid.html)
- [Pact Contract Testing](https://docs.pact.io/)
- [Testcontainers](https://dotnet.testcontainers.org/)
- [Playwright](https://playwright.dev/)
- [k6 Load Testing](https://k6.io/)