# i18n Namespace Structure

This project uses **react-i18next** with a namespace-based organization for translations.

## 📁 Structure Overview

```
i18n/
├── config.ts           # i18next configuration
├── locales/
│   ├── nl.json        # Dutch translations (default)
│   └── en.json        # English translations
└── README.md          # This file
```

## 🎯 Namespace Organization

### 1. **common** - Shared Terms
Frequently used terms across the entire application.

#### Subnamespaces:
- **`common.actions`** - Action verbs (save, cancel, edit, delete, etc.)
- **`common.entities`** - Domain entities (student, teacher, course, etc.)
- **`common.status`** - Status labels (active, inactive, pending, etc.)
- **`common.states`** - UI states (loading, error, success, etc.)
- **`common.form`** - Form-related labels and validation messages
- **`common.time`** - Date/time related terms (days, months, etc.)

#### Examples:
```tsx
import { useTranslation } from 'react-i18next'

const { t } = useTranslation()

// Actions
<Button>{t('common.actions.save')}</Button>  // "Opslaan" (nl) / "Save" (en)
<Button>{t('common.actions.cancel')}</Button>  // "Annuleren" (nl) / "Cancel" (en)

// Entities
<h1>{t('common.entities.students')}</h1>  // "Leerlingen" (nl) / "Students" (en)

// Status
<Badge>{t('common.status.active')}</Badge>  // "Actief" (nl) / "Active" (en)

// Form validation
<Error>{t('common.form.validation.required')}</Error>  // "Dit veld is verplicht" (nl)
```

### 2. **navigation** - Navigation Items
Top-level navigation menu items.

```tsx
<Link to="/students">{t('navigation.students')}</Link>  // "Leerlingen" (nl)
```

### 3. **auth** - Authentication
Login/logout related strings.

```tsx
<Button>{t('auth.loginButton')}</Button>  // "Inloggen" (nl) / "Sign In" (en)
<p>{t('auth.errors.invalidCredentials')}</p>  // "Ongeldige inloggegevens" (nl)
```

### 4. **dashboard** - Dashboard Page
Dashboard-specific content.

```tsx
<h1>{t('dashboard.title')}</h1>  // "Dashboard"
<p>{t('dashboard.subtitle')}</p>  // "Welkom bij BosDAT Muziekschool Beheer" (nl)

// With interpolation
<p>{t('dashboard.stats.totalStudents', { count: 25 })}</p>  // "25 totaal leerlingen" (nl)
```

### 5. **students** - Student Management
Student-related pages and components.

```tsx
<h1>{t('students.title')}</h1>  // "Leerlingen" (nl)
<Button>{t('students.addStudent')}</Button>  // "Leerling Toevoegen" (nl)

// Form fields
<Label>{t('students.form.firstName')}</Label>  // "Voornaam" (nl)

// Sections
<Tab>{t('students.sections.enrollments')}</Tab>  // "Inschrijvingen" (nl)
```

### 6. **teachers** - Teacher Management
Teacher-related pages and components.

```tsx
<h1>{t('teachers.title')}</h1>  // "Docenten" (nl)
<Label>{t('teachers.form.specialization')}</Label>  // "Specialisatie" (nl)
```

### 7. **courses** - Course Management
Course-related pages and components.

```tsx
<h1>{t('courses.title')}</h1>  // "Cursussen" (nl)
<Badge>{t('courses.weekParity.odd')}</Badge>  // "Oneven" (nl)
```

### 8. **lessons** - Lesson Management
Lesson-related pages and components.

```tsx
<Dialog>
  <DialogTitle>{t('lessons.dialogs.cancel.title')}</DialogTitle>
  <DialogDescription>{t('lessons.dialogs.cancel.description')}</DialogDescription>
</Dialog>
```

### 9. **enrollments** - Enrollment Process
Enrollment workflow and stepper.

```tsx
<Step>{t('enrollments.stepper.lessonDetails')}</Step>  // "Les Details" (nl)
```

### 10. **invoices** - Invoice Management
Invoice-related pages.

```tsx
<h1>{t('invoices.title')}</h1>  // "Facturen" (nl)
<Label>{t('invoices.form.dueDate')}</Label>  // "Vervaldatum" (nl)
```

### 11. **settings** - Settings
Settings page and configuration.

```tsx
<Tab>{t('settings.sections.instruments')}</Tab>  // "Instrumenten" (nl)
<Button>{t('settings.instruments.addInstrument')}</Button>  // "Instrument Toevoegen" (nl)
```

## 🚀 Usage Guidelines

### Basic Usage

```tsx
import { useTranslation } from 'react-i18next'

export function MyComponent() {
  const { t } = useTranslation()

  return (
    <div>
      <h1>{t('dashboard.title')}</h1>
      <Button>{t('common.actions.save')}</Button>
    </div>
  )
}
```

### Interpolation (Variables)

```tsx
// Translation with placeholder
// nl.json: "totalStudents": "{{count}} totaal leerlingen"

<p>{t('dashboard.stats.totalStudents', { count: students.length })}</p>
```

### Pluralization

```tsx
// For complex pluralization, use count-based logic
const count = items.length
const key = count === 1 ? 'common.entities.student' : 'common.entities.students'
<p>{count} {t(key)}</p>
```

### Conditional Rendering

```tsx
{isLoading && <Spinner>{t('common.states.loading')}</Spinner>}
{error && <Alert>{t('common.states.error')}</Alert>}
```

## 📝 Adding New Translations

### 1. Choose the Right Namespace

- **Common terms** (used in 3+ places) → `common.*`
- **Feature-specific** (used only in one domain) → Feature namespace (e.g., `students.*`)
- **Page-specific** (used only on one page) → Page namespace (e.g., `dashboard.*`)

### 2. Add to Both Language Files

Always update **both** `nl.json` and `en.json`:

```json
// nl.json
{
  "students": {
    "newKey": "Nederlandse vertaling"
  }
}

// en.json
{
  "students": {
    "newKey": "English translation"
  }
}
```

**Verify consistency:**
```bash
npm run check:i18n
```

This script validates that all keys exist in both language files. It will:
- ✅ Show success if translations are consistent
- ❌ List any missing keys if inconsistencies are found
- Display total number of translation keys

### 3. Use Descriptive Keys

```tsx
// ❌ Bad (unclear)
t('students.btn1')
t('students.text')

// ✅ Good (descriptive)
t('students.addStudent')
t('students.form.firstName')
```

### 4. Organize Hierarchically

```json
{
  "students": {
    "title": "Leerlingen",
    "form": {
      "firstName": "Voornaam",
      "lastName": "Achternaam"
    },
    "sections": {
      "profile": "Profiel",
      "enrollments": "Inschrijvingen"
    }
  }
}
```

## 🔍 Finding the Right Key

### Step 1: Identify the Scope
- Is it used across multiple features? → `common.*`
- Is it specific to one feature? → Feature namespace

### Step 2: Check Existing Keys
- Look in `locales/nl.json` or `locales/en.json`
- Use your editor's search (Ctrl+F / Cmd+F)

### Step 3: Add if Missing
- Follow the namespace structure
- Add to both language files
- Use descriptive, hierarchical keys

## 🌍 Language Switching

The app includes a `<LanguageSwitcher />` component in the Layout header.

- **Default language**: Dutch (nl)
- **Fallback language**: Dutch (nl)
- **Persistence**: localStorage (`i18nextLng`)

### Programmatic Language Change

```tsx
import { useTranslation } from 'react-i18next'

const { i18n } = useTranslation()

// Change language
i18n.changeLanguage('en')  // Switch to English
i18n.changeLanguage('nl')  // Switch to Dutch

// Current language
console.log(i18n.language)  // "nl" or "en"
```

## ✅ Best Practices

1. **Always use namespaces** - Don't flatten translations
2. **Be consistent** - Follow the established hierarchy
3. **Use common.* for shared terms** - Avoid duplication
4. **Keep keys descriptive** - `addStudent` not `btn1`
5. **Update both languages** - Always add keys to nl.json AND en.json
6. **Use interpolation** - For dynamic values like counts
7. **Group related keys** - Use nested objects for organization

## 🔧 Configuration

The i18n instance is configured in `config.ts`:

```ts
i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: 'nl',
    debug: import.meta.env.DEV,
    interpolation: {
      escapeValue: false,
    },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
    },
  })
```

- **Language detection**: localStorage first, then browser settings
- **Fallback**: Dutch (nl)
- **Debug mode**: Enabled in development
- **Auto-escaping**: Disabled (React handles it)

## 📚 Resources

- [react-i18next documentation](https://react.i18next.com/)
- [i18next documentation](https://www.i18next.com/)
