# Development Workflow

## TDD (MANDATORY)

1. **RED:** Write failing test
2. **GREEN:** Minimal implementation to pass
3. **REFACTOR:** Improve code, tests still pass
4. **VERIFY:** 80%+ coverage

**Rule:** Write test BEFORE implementation.

## Security Checklist (Before ANY commit)

- [ ] No hardcoded secrets (API keys, passwords, tokens)
- [ ] All user inputs validated (DTOs, Zod schemas)
- [ ] SQL injection prevention (EF parameterized queries)
- [ ] XSS prevention (sanitized HTML, React auto-escapes)
- [ ] CSRF protection enabled
- [ ] Auth/authz verified on endpoints
- [ ] Rate limiting on public endpoints
- [ ] Error messages don't leak sensitive data

**Secret Management:**
```ts
// ❌ NEVER
const key = "sk-proj-xxxxx"

// ✅ ALWAYS
const key = process.env.API_KEY
if (!key) throw new Error('API_KEY not configured')
```

## Git Workflow

**Commits:**
- Conventional format: `type(scope): description`
- Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`
- Co-Authored-By: `Claude Sonnet 4.5 <noreply@anthropic.com>`

**Pull Requests:**
- Create from feature branch
- Run tests before PR
- Title: short (<70 chars)
- Body: Summary + Test plan
- Link JIRA issue

## Testing

**Coverage:** 80% minimum

**Types:**
- **Unit:** Functions, utilities, components
- **Integration:** API endpoints, DB operations
- **E2E:** Critical flows (Playwright)

**Backend:**
```bash
dotnet test
dotnet test /p:CollectCoverage=true
```

**Frontend:**
```bash
npm run test           # Watch mode
npm run test:coverage  # Coverage report
```

## Code Quality

**Before marking work complete:**
- [ ] Code is readable, well-named
- [ ] Functions <50 lines
- [ ] Files <800 lines
- [ ] No deep nesting (>4 levels)
- [ ] Proper error handling
- [ ] No console.log statements
- [ ] No hardcoded values
- [ ] No mutation (immutable patterns)
- [ ] Tests pass
- [ ] 80%+ coverage

## SOLID Principles

- **S:** Controllers route only. Services handle logic. Components render only. Hooks handle logic.
- **O:** Use interfaces (C#) / props (React) to extend behavior.
- **L:** Subclasses/components must be interchangeable with base/types.
- **I:** Many small interfaces > one "God" interface.
- **D:** Use DI (.NET) / Context/Props (React).

## Feature Implementation

1. **Plan:** Clarify requirements. Identify dependencies. Break into phases.
2. **TDD:** Write tests first.
3. **Implement:** Minimal code to pass tests.
4. **Refactor:** Improve code while tests pass.
5. **Review:** Address issues.
6. **Commit:** Detailed message, conventional format.

## Avoid Over-Engineering

- Don't add features beyond what's requested
- Don't refactor surrounding code unless necessary
- Don't add comments where code is self-evident
- Don't add error handling for impossible scenarios
- Don't create abstractions for one-time operations
- Don't design for hypothetical future requirements

**Rule:** Minimum complexity needed for current task.
