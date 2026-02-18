---
name: i18n
description: "react-i18next patterns: useTranslation hook, translation map pattern for enums/status types, namespace conventions, adding translations to nl/en locale files. Use when implementing i18n in components or adding new translation keys."
---

# i18n (react-i18next)

BosDAT uses **react-i18next** with Dutch (nl) as default, English (en) as secondary. Keys are returned as-is in tests.

| File | Purpose |
|------|---------|
| `src/i18n/config.ts` | Auto-imported in `main.tsx` — do not import in tests |
| `src/i18n/locales/nl.json` | Dutch translations (default + fallback) |
| `src/i18n/locales/en.json` | English translations |
| `src/i18n/README.md` | Complete namespace documentation |

---

## Basic Usage

```ts
import { useTranslation } from 'react-i18next'

const { t } = useTranslation()

// Common shared terms
<button>{t('common.actions.save')}</button>
<button>{t('common.actions.cancel')}</button>
<h1>{t('common.entities.students')}</h1>

// Feature-specific
<label>{t('students.form.firstName')}</label>
<p>{t('courses.status.active')}</p>
```

---

## Translation Map Pattern (enums / status types)

Use a `const` map to translate enum values — avoids switch statements and keeps keys type-safe.

```ts
// types.ts
export type CourseStatus = 'Active' | 'Paused' | 'Completed' | 'Cancelled'

export const courseStatusTranslations = {
  'Active':    'courses.status.active',
  'Paused':    'courses.status.paused',
  'Completed': 'courses.status.completed',
  'Cancelled': 'courses.status.cancelled',
} as const satisfies Record<CourseStatus, string>

// Component usage
const { t } = useTranslation()
{t(courseStatusTranslations[course.status])}
```

---

## Namespace Conventions

| Namespace | Use when |
|-----------|----------|
| `common.*` | Term appears in 3+ places (actions, entities, labels) |
| `students.*` | Student-specific UI |
| `teachers.*` | Teacher-specific UI |
| `courses.*` | Course-specific UI |
| `lessons.*` | Lesson-specific UI |
| `enrollments.*` | Enrollment-specific UI |
| `invoices.*` | Invoice-specific UI |
| `dashboard.*` | Dashboard-specific UI |
| `settings.*` | Settings-specific UI |
| `auth.*` | Login/auth UI |
| `navigation.*` | Sidebar/nav labels |

**Key hierarchy:** `students.form.firstName` not `student_first_name`

---

## Adding Translations (workflow)

1. Choose namespace: `common` if shared in 3+ places, else feature-specific
2. Add key to **both** `locales/nl.json` AND `locales/en.json`
3. Use the key in the component

```json
// nl.json
{
  "students": {
    "form": {
      "firstName": "Voornaam",
      "lastName": "Achternaam"
    },
    "status": {
      "active": "Actief",
      "inactive": "Inactief"
    }
  }
}

// en.json
{
  "students": {
    "form": {
      "firstName": "First name",
      "lastName": "Last name"
    },
    "status": {
      "active": "Active",
      "inactive": "Inactive"
    }
  }
}
```

---

## Testing

`react-i18next` is **globally mocked** in `src/test/setup.ts` — `t('some.key')` returns `'some.key'` as-is.

```ts
// ✅ Assert on translation keys, not translated strings
expect(screen.getByText('students.form.firstName')).toBeInTheDocument()

// ❌ Don't assert on translated strings in tests
expect(screen.getByText('Voornaam')).toBeInTheDocument()
```

**CRITICAL:** Never import `@/i18n/config` in `src/test/utils.tsx` — breaks mock hoisting.

For typing `t` in helper functions:
```ts
import type { TFunction } from 'i18next'

function renderLabel(t: TFunction, status: CourseStatus) {
  return t(courseStatusTranslations[status])
}
```
