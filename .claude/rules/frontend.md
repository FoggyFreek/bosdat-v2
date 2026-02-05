# Frontend Rules (React 18 / TypeScript)

## Naming
- Components/Types: PascalCase | Functions/vars: camelCase | CSS: kebab-case
- Structure: `features/[domain]/` → components, types, context
- Generic components `components/[component]` 

## API enum properties
- API returns enum values as strings, never numbers. Check the relevant API controller before writing code. 

## Immutability
State should be treated as immutable. Never change existing objects or arrays directly—always create and use a new copy when updating state or data

## Code Splitting
```tsx
// ✅ Direct import (separate chunk)
lazy(() => import('./pages/StudentsPage'))

// ❌ Barrel import (single chunk)
lazy(() => import('./pages').then(m => ({ default: m.StudentsPage })))
```

## Context Memoization (REQUIRED)
Memoize context values and providers so their references change only when the actual data changes, preventing unnecessary re-renders of consumers.

```tsx
const login = useCallback(async (data) => { ... }, [])
const value = useMemo(() => ({ user, login }), [user, login])
<AuthContext.Provider value={value}>
```

## Null Safety
```tsx
// Arrays: Always use fallbacks
const students = step2.students ?? []
{students.map(s => <Card key={s.id} />)}
```

## Conditional Rendering
Keep conditional rendering flat and readable—avoid deeply nested conditionals; prefer early returns or simple expression
```tsx
// Flat (scannable)
{isLoading && <Spinner />}
{!isLoading && items.length === 0 && <Empty />}
{!isLoading && items.length > 0 && <List items={items} />}
```

## Keys & Accessibility
- **Lists:** Stable IDs, never index: `key={item.id}` not `key={i}`
- **Interactive divs:** Add `role="button"`, `tabIndex={0}`, `onKeyDown` (Enter/Space)

## React Router
**Required:** `<BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>`
**Locations:** `main.tsx`, `test/utils.tsx`, all test BrowserRouters

## Testing
- Import from `@/test/utils` (NOT `@testing-library/react`)
- Fresh `QueryClient` per test (`createTestQueryClient()`)
- Co-locate in `__tests__/` folders

## Component Rules
- UI in components, logic in hooks
- shadcn/ui + Tailwind only (no custom CSS/inline styles)
- 200-400 lines typical, 800 max
