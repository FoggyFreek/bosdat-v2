import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import type { ReactNode } from 'react'
import { FormDirtyProvider } from '@/context/FormDirtyContext'
import { EmailSettingsSection } from '../EmailSettingsSection'
import { settingsApi } from '@/features/settings/api'
import type { SystemSetting } from '@/features/settings/types'

vi.mock('@/features/settings/api', () => ({
  settingsApi: {
    getByKey: vi.fn(),
    update: vi.fn(),
  },
}))

vi.mock('@/hooks/use-toast', () => ({
  useToast: () => ({
    toast: vi.fn(),
  }),
}))

const mockSubjectSetting: SystemSetting = {
  key: 'email_invoice_subject_template',
  value: 'Factuur {{InvoiceNumber}} - {{SchoolName}}',
}

const mockBodySetting: SystemSetting = {
  key: 'email_invoice_body_template',
  value: '<p>Beste @Model.StudentFirstName</p>',
}

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
          <FormDirtyProvider>{children}</FormDirtyProvider>
        </BrowserRouter>
      </QueryClientProvider>
    )
  }
}

describe('EmailSettingsSection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(settingsApi.getByKey).mockImplementation(async (key: string) => {
      if (key === 'email_invoice_subject_template') return mockSubjectSetting
      if (key === 'email_invoice_body_template') return mockBodySetting
      throw new Error(`Unknown key: ${key}`)
    })
  })

  it('renders subject and body templates after loading', async () => {
    render(<EmailSettingsSection />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByDisplayValue(mockSubjectSetting.value)).toBeInTheDocument()
    })
    expect(screen.getByDisplayValue(mockBodySetting.value)).toBeInTheDocument()
  })

  it('renders section title and description', async () => {
    render(<EmailSettingsSection />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('settings.sections.email')).toBeInTheDocument()
    })
    expect(screen.getByText('settings.email.description')).toBeInTheDocument()
  })

  it('enables save button when content is modified', async () => {
    const user = userEvent.setup()
    render(<EmailSettingsSection />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByDisplayValue(mockSubjectSetting.value)).toBeInTheDocument()
    })

    const subjectInput = screen.getByDisplayValue(mockSubjectSetting.value)
    await user.clear(subjectInput)
    await user.type(subjectInput, 'New subject')

    const saveButton = screen.getByRole('button', { name: /common\.actions\.save/i })
    expect(saveButton).not.toBeDisabled()
  })

  it('calls settingsApi.update on save', async () => {
    const user = userEvent.setup()
    vi.mocked(settingsApi.update).mockResolvedValue(undefined as never)

    render(<EmailSettingsSection />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByDisplayValue(mockSubjectSetting.value)).toBeInTheDocument()
    })

    const subjectInput = screen.getByDisplayValue(mockSubjectSetting.value)
    await user.clear(subjectInput)
    await user.type(subjectInput, 'Updated subject')

    const saveButton = screen.getByRole('button', { name: /common\.actions\.save/i })
    await user.click(saveButton)

    await waitFor(() => {
      expect(settingsApi.update).toHaveBeenCalledWith(
        'email_invoice_subject_template',
        'Updated subject'
      )
    })
  })
})
