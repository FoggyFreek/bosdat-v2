# Production Readiness Checklist

Configuration TODOs before deploying to production.

## 1. Secrets Management

### Backend (.NET)
- [ ] Move JWT secret to secure storage (Azure Key Vault, AWS Secrets Manager, or similar)
- [ ] Generate production JWT secret (min 256 bits / 32 bytes, use `openssl rand -base64 32`)
- [ ] Move database password to secrets manager
- [ ] Configure User Secrets for local dev instead of appsettings
- [ ] Remove any hardcoded secrets from configuration files
- [ ] Add `.env` to `.gitignore` if using environment files

**Implementation:**
```csharp
// Program.cs
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{keyVaultName}.vault.azure.net/"),
        new DefaultAzureCredential());
}
```

### Frontend (React)
- [ ] Move API URL to environment variables
- [ ] Create `.env.production` file (not committed to git)
- [ ] Validate all required env vars at build time
- [ ] Use build-time variable replacement for sensitive configs

## 2. Database Configuration

- [ ] Use connection pooling with appropriate pool size
- [ ] Configure command timeout for long-running queries
- [ ] Enable SSL/TLS for database connections
- [ ] Set up read replicas for read-heavy operations
- [ ] Configure automatic backups (daily minimum)
- [ ] Test backup restoration process
- [ ] Set up point-in-time recovery
- [ ] Configure connection retry logic with exponential backoff
- [ ] Implement database migration strategy (zero-downtime deployments)

**Connection String:**
```
Host=prod-db.example.com;Database=bosdat;Username=bosdat_user;Password=<from-secrets>;SSL Mode=Require;Trust Server Certificate=false;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;
```

## 3. Authentication & Security

- [ ] Generate new JWT secret (different from dev/staging)
- [ ] Reduce JWT token expiration time (15-30 min for access tokens)
- [ ] Enable HTTPS only (HTTP Strict Transport Security headers)
- [ ] Configure secure cookie settings (HttpOnly, Secure, SameSite)
- [ ] Implement refresh token rotation
- [ ] Add rate limiting on authentication endpoints (5 attempts/15 min)
- [ ] Enable account lockout after failed login attempts
- [ ] Configure CORS with specific origins (no wildcards)
- [ ] Add security headers (X-Content-Type-Options, X-Frame-Options, CSP)
- [ ] Enable CSRF protection
- [ ] Implement API key rotation strategy
- [ ] Set up 2FA/MFA for admin accounts

**appsettings.Production.json:**
```json
{
  "JwtSettings": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7,
    "Issuer": "https://api.bosdat.production.com",
    "Audience": "https://app.bosdat.production.com"
  },
  "Cors": {
    "AllowedOrigins": ["https://app.bosdat.production.com"]
  }
}
```

## 4. HTTPS/SSL Configuration

- [ ] Obtain SSL certificate (Let's Encrypt, DigiCert, etc.)
- [ ] Configure HTTPS redirect middleware
- [ ] Enable HTTP Strict Transport Security (HSTS)
- [ ] Configure certificate renewal automation
- [ ] Set minimum TLS version to 1.2 (preferably 1.3)
- [ ] Disable weak cipher suites

**Program.cs:**
```csharp
app.UseHsts(); // Enable HSTS
app.UseHttpsRedirection();

// Configure Kestrel for HTTPS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});
```

## 5. Logging & Monitoring

- [ ] Integrate centralized logging (Serilog + Seq, Application Insights, ELK stack)
- [ ] Remove verbose logging (Debug/Trace levels)
- [ ] Add structured logging with correlation IDs
- [ ] Configure log retention policies
- [ ] Set up alerting for critical errors
- [ ] Implement health check endpoints
- [ ] Add performance monitoring (APM tool)
- [ ] Configure database query logging (slow queries only)
- [ ] Implement audit logging for sensitive operations
- [ ] Set up uptime monitoring (Pingdom, UptimeRobot, etc.)
- [ ] Never log sensitive data (passwords, tokens, PII)

**Serilog Configuration:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": { "serverUrl": "https://logs.example.com" }
      }
    ]
  }
}
```

## 6. Performance Optimization

- [ ] Enable response compression (gzip/brotli)
- [ ] Configure response caching for static endpoints
- [ ] Implement output caching for expensive queries
- [ ] Enable HTTP/2 or HTTP/3
- [ ] Configure CDN for static assets (frontend)
- [ ] Optimize database indexes
- [ ] Enable query plan caching
- [ ] Configure connection pooling
- [ ] Add distributed caching (Redis) for session/data caching
- [ ] Implement pagination on all list endpoints
- [ ] Add request size limits
- [ ] Configure memory limits and garbage collection

**Program.cs:**
```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddResponseCaching();
```

## 7. Error Handling

- [ ] Implement global exception handler
- [ ] Return generic error messages (don't expose stack traces)
- [ ] Log detailed errors server-side
- [ ] Configure custom error pages (404, 500, etc.)
- [ ] Set up error tracking (Sentry, Rollbar, Application Insights)
- [ ] Implement circuit breaker pattern for external services
- [ ] Add retry policies with exponential backoff

**Middleware:**
```csharp
app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

## 8. Rate Limiting & Throttling

- [ ] Implement rate limiting on public endpoints
- [ ] Configure stricter limits for authentication endpoints
- [ ] Add IP-based throttling
- [ ] Implement API quotas per user/tenant
- [ ] Configure DDoS protection (Cloudflare, AWS Shield)
- [ ] Add request size limits

**ASP.NET Core Rate Limiting:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Request.Headers.Host.ToString(),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

## 9. Infrastructure

- [ ] Set up reverse proxy (nginx, Traefik, etc.)
- [ ] Configure load balancer for multi-instance deployments
- [ ] Implement health checks for load balancer
- [ ] Set up auto-scaling policies
- [ ] Configure firewall rules (whitelist only necessary ports)
- [ ] Enable network segmentation (VPC, subnets)
- [ ] Set up Web Application Firewall (WAF)
- [ ] Configure container orchestration (Kubernetes, ECS, etc.)
- [ ] Implement blue-green or canary deployments

**Docker Production Configuration:**
```yaml
# docker-compose.prod.yml
services:
  api:
    restart: always
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    deploy:
      replicas: 3
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 1G
```

## 10. Frontend Build Configuration

- [ ] Optimize build for production (`npm run build`)
- [ ] Enable code splitting and lazy loading
- [ ] Configure asset optimization (minification, tree-shaking)
- [ ] Set up CDN for static assets
- [ ] Enable service worker for offline support (if needed)
- [ ] Configure proper cache headers for assets
- [ ] Remove development-only code (console.logs, debug tools)
- [ ] Enable source maps in separate location (not public)
- [ ] Configure bundle size budgets
- [ ] Add Content Security Policy headers

**vite.config.ts:**
```ts
export default defineConfig({
  build: {
    sourcemap: 'hidden', // Generate but don't expose
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom'],
          router: ['react-router-dom']
        }
      }
    }
  }
})
```

## 11. API Documentation

- [ ] Disable Swagger UI in production (or secure it behind auth)
- [ ] Generate OpenAPI spec for internal use
- [ ] Document rate limits in API docs
- [ ] Add versioning strategy to API endpoints

**Program.cs:**
```csharp
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

## 12. Data Protection & Compliance

- [ ] Enable data encryption at rest
- [ ] Enable encryption in transit (TLS 1.2+)
- [ ] Configure data retention policies
- [ ] Implement GDPR compliance features (data export, deletion)
- [ ] Add user consent management
- [ ] Configure audit logging for compliance
- [ ] Implement PII data masking in logs
- [ ] Set up data backup encryption
- [ ] Configure geographic data residency (if required)

## 13. Deployment & CI/CD

- [ ] Set up automated deployment pipeline
- [ ] Implement database migration strategy
- [ ] Configure health checks in deployment pipeline
- [ ] Add smoke tests post-deployment
- [ ] Set up rollback procedures
- [ ] Configure deployment notifications (Slack, email, etc.)
- [ ] Implement feature flags for gradual rollouts
- [ ] Set up staging environment identical to production
- [ ] Test disaster recovery procedures

## 14. Environment Variables Checklist

### Required Backend Env Vars
```bash
# Database
ConnectionStrings__DefaultConnection=<from-secrets>

# JWT
JwtSettings__Secret=<from-secrets>
JwtSettings__Issuer=https://api.bosdat.production.com
JwtSettings__Audience=https://app.bosdat.production.com
JwtSettings__AccessTokenExpirationMinutes=15
JwtSettings__RefreshTokenExpirationDays=7

# CORS
Cors__AllowedOrigins__0=https://app.bosdat.production.com

# Logging
Serilog__WriteTo__0__Args__ServerUrl=<logging-service-url>
Serilog__WriteTo__0__Args__ApiKey=<from-secrets>

# Email (if using)
EmailSettings__SmtpServer=<smtp-server>
EmailSettings__SmtpPort=587
EmailSettings__Username=<from-secrets>
EmailSettings__Password=<from-secrets>
```

### Required Frontend Env Vars
```bash
# API
VITE_API_URL=https://api.bosdat.production.com

# Feature Flags
VITE_ENABLE_ANALYTICS=true

# Monitoring
VITE_SENTRY_DSN=<from-secrets>
```

## 15. Security Scanning

- [ ] Run OWASP dependency check
- [ ] Scan Docker images for vulnerabilities
- [ ] Configure automated security scanning in CI/CD
- [ ] Perform penetration testing
- [ ] Run static code analysis (SonarQube)
- [ ] Implement secrets scanning in git history
- [ ] Set up vulnerability disclosure policy

**Commands:**
```bash
# .NET security scan
dotnet list package --vulnerable

# Frontend security scan
npm audit
npm audit fix

# Docker image scan
docker scan bosdat-api:latest
```

## 16. Testing Before Production

- [ ] Run full test suite (unit, integration, e2e)
- [ ] Verify 80%+ code coverage
- [ ] Performance testing under load
- [ ] Security testing (OWASP Top 10)
- [ ] Test backup and restore procedures
- [ ] Test failover scenarios
- [ ] Verify monitoring and alerting
- [ ] Test rate limiting
- [ ] Validate all environment configurations
- [ ] Run accessibility testing

## Priority Order

**Phase 1 (Critical - Block deployment):**
1. Secrets management (§1)
2. HTTPS/SSL configuration (§4)
3. Authentication & security (§3)
4. Database security (§2)
5. Error handling (§7)

**Phase 2 (High - Deploy with monitoring):**
6. Logging & monitoring (§5)
7. Performance optimization (§6)
8. Rate limiting (§8)
9. Infrastructure setup (§9)

**Phase 3 (Medium - Post-launch):**
10. Data protection & compliance (§12)
11. CI/CD automation (§13)
12. Security scanning (§15)

**Phase 4 (Nice-to-have):**
13. Advanced monitoring
14. Advanced caching strategies
15. CDN optimization
