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

## Feature Implementation

1. **Plan:** Clarify requirements. Identify dependencies. Break into phases.
2. **TDD:** Write tests first.
3. **Implement:** Minimal code to pass tests.
4. **Refactor:** Improve code while tests pass.
5. **Review:** Address issues.
6. **Commit:** Detailed message, conventional format.
