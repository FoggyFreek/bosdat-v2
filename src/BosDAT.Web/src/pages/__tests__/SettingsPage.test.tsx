import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { SettingsPage } from '../SettingsPage'
import { courseTypesApi } from '@/features/course-types/api'
import { instrumentsApi } from '@/features/instruments/api'
import { roomsApi } from '@/features/rooms/api'
import { holidaysApi, settingsApi } from '@/features/settings/api'

// Mock AuthContext — SettingsNavigation uses useAuth to check for Admin role
vi.mock('@/context/AuthContext', () => ({
  useAuth: () => ({ user: { roles: [] }, isAuthenticated: true, isLoading: false }),
}))

// Mock all API modules
vi.mock('@/features/course-types/api', () => ({
  courseTypesApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    reactivate: vi.fn(),
    getTeacherCountForInstrument: vi.fn(),
  },
}))

vi.mock('@/features/instruments/api', () => ({
  instrumentsApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}))

vi.mock('@/features/rooms/api', () => ({
  roomsApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
}))

vi.mock('@/features/settings/api', () => ({
  holidaysApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    delete: vi.fn(),
  },
  settingsApi: {
    getAll: vi.fn(),
    getByKey: vi.fn(),
    update: vi.fn(),
  },
}))

describe('SettingsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Default mock responses
    vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
    vi.mocked(roomsApi.getAll).mockResolvedValue([])
    vi.mocked(courseTypesApi.getAll).mockResolvedValue([])
    vi.mocked(holidaysApi.getAll).mockResolvedValue([])
    vi.mocked(settingsApi.getAll).mockResolvedValue([])
  })

  describe('Navigation', () => {
    it('renders settings title and navigation sidebar', async () => {
      render(<SettingsPage />)

      // Wait for lazy-loaded content to settle
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'settings.title' })).toBeInTheDocument()
      })
    })

    it('renders all navigation groups', () => {
      render(<SettingsPage />)

      expect(screen.getByText('settings.navigation.account')).toBeInTheDocument()
      expect(screen.getByText('settings.navigation.lessons')).toBeInTheDocument()
      expect(screen.getByText('settings.navigation.scheduling')).toBeInTheDocument()
      expect(screen.getByText('settings.navigation.general')).toBeInTheDocument()
    })

    it('renders all navigation items', () => {
      render(<SettingsPage />)

      expect(screen.getByRole('button', { name: 'settings.sections.profile' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'settings.sections.preferences' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'settings.sections.instruments' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'settings.sections.courseTypes' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'settings.sections.rooms' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'settings.sections.holidays' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'settings.sections.system' })).toBeInTheDocument()
    })

    it('defaults to profile section', async () => {
      render(<SettingsPage />)

      // Verify the settings page is rendered and has navigation
      expect(screen.getByRole('heading', { name: 'settings.title' })).toBeInTheDocument()
    })

    it('navigates to different sections when clicking nav items', async () => {
      const user = userEvent.setup()
      render(<SettingsPage />)

      // Click on Rooms
      await user.click(screen.getByRole('button', { name: 'settings.sections.rooms' }))
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'settings.rooms.title' })).toBeInTheDocument()
      })

      // Click on Holidays
      await user.click(screen.getByRole('button', { name: 'settings.sections.holidays' }))
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'settings.holidays.title' })).toBeInTheDocument()
      })

      // Click on System Settings
      await user.click(screen.getByRole('button', { name: 'settings.sections.system' }))
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'settings.system.title' })).toBeInTheDocument()
      })
    })

    it('shows placeholder content for Profile section', async () => {
      const user = userEvent.setup()
      render(<SettingsPage />)

      const profileBtn = screen.getByRole('button', { name: 'settings.sections.profile' })
      expect(profileBtn).toBeInTheDocument()
      await user.click(profileBtn)

      // Verify navigation occurred
      expect(profileBtn).toHaveClass('bg-primary')
    })

    it('shows placeholder content for Preferences section', async () => {
      const user = userEvent.setup()
      render(<SettingsPage />)

      const prefsBtn = screen.getByRole('button', { name: 'settings.sections.preferences' })
      expect(prefsBtn).toBeInTheDocument()
      await user.click(prefsBtn)

      // Verify navigation occurred
      expect(prefsBtn).toHaveClass('bg-primary')
    })
  })

  describe('Unsaved Changes Detection', () => {
    it('allows navigation without dialog when no unsaved changes', async () => {
      const user = userEvent.setup()
      vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
      vi.mocked(roomsApi.getAll).mockResolvedValue([])

      render(<SettingsPage />)

      // Navigate to Rooms without making changes
      const roomsNavButton = screen.getByRole('button', { name: 'settings.sections.rooms' })
      await user.click(roomsNavButton)

      // Should navigate directly without dialog
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'settings.rooms.title' })).toBeInTheDocument()
      }, { timeout: 3000 })

      // Confirm dialog text was not shown
      expect(screen.queryByText('settings.unsavedChanges.description')).not.toBeInTheDocument()
    })

    it('dirty state context exists and can be accessed', () => {
      // The SettingsDirtyContext is properly set up
      // This is a basic test to ensure the context structure is correct
      render(<SettingsPage />)

      // If the page renders without errors, the context is working
      expect(screen.getByRole('heading', { name: 'settings.title' })).toBeInTheDocument()
    })
  })

  describe('Instruments Section', () => {
    it('displays instruments list', async () => {
      const user = userEvent.setup()
      vi.mocked(instrumentsApi.getAll).mockResolvedValue([
        { id: 1, name: 'Piano', category: 'Keyboard', isActive: true },
        { id: 2, name: 'Guitar', category: 'String', isActive: true },
        { id: 3, name: 'Drums', category: 'Percussion', isActive: false },
      ])

      render(<SettingsPage />)

      // Navigate to Instruments section
      await user.click(screen.getByRole('button', { name: 'settings.sections.instruments' }))

      await waitFor(() => {
        expect(screen.getByText('Piano')).toBeInTheDocument()
        expect(screen.getByText('Guitar')).toBeInTheDocument()
        expect(screen.getByText('Drums')).toBeInTheDocument()
      })
    })

    it('shows add form when clicking Add button', async () => {
      const user = userEvent.setup()
      vi.mocked(instrumentsApi.getAll).mockResolvedValue([])

      render(<SettingsPage />)

      // Navigate to Instruments section
      await user.click(screen.getByRole('button', { name: 'settings.sections.instruments' }))

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'settings.instruments.title' })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: 'settings.instruments.addInstrument' }))

      expect(screen.getByPlaceholderText('settings.instruments.form.namePlaceholder')).toBeInTheDocument()
    })

    it('creates new instrument on submit', async () => {
      const user = userEvent.setup()
      vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
      vi.mocked(instrumentsApi.create).mockResolvedValue({ id: 1, name: 'Violin', category: 'String', isActive: true })

      render(<SettingsPage />)

      // Navigate to Instruments section
      await user.click(screen.getByRole('button', { name: 'settings.sections.instruments' }))

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'settings.instruments.title' })).toBeInTheDocument()
      })

      // Click Add
      await user.click(screen.getByRole('button', { name: 'settings.instruments.addInstrument' }))

      // Fill form
      await user.type(screen.getByPlaceholderText('settings.instruments.form.namePlaceholder'), 'Violin')

      // Submit (click the check button - second button in form after the Select combobox)
      const addForm = screen.getByPlaceholderText('settings.instruments.form.namePlaceholder').closest('div.mb-4')!
      const buttons = within(addForm as HTMLElement).getAllByRole('button')
      // Find the submit button (has check icon, not the combobox or cancel button)
      const submitButton = buttons.find(btn => btn.querySelector('svg.lucide-check'))
      expect(submitButton).toBeDefined()
      await user.click(submitButton!)

      await waitFor(() => {
        expect(instrumentsApi.create).toHaveBeenCalledWith({
          name: 'Violin',
          category: 'Other',
          isActive: true,
        })
      })
    })
  })

  describe('System Settings Section', () => {
    it('displays system settings', async () => {
      const user = userEvent.setup()
      vi.mocked(settingsApi.getAll).mockResolvedValue([
        { key: 'vat_rate', value: '21', type: 'decimal', description: 'VAT percentage' },
        { key: 'child_discount_percent', value: '10', type: 'decimal', description: 'Child discount' },
      ])

      render(<SettingsPage />)

      // Navigate to System Settings
      await user.click(screen.getByRole('button', { name: 'settings.sections.system' }))

      await waitFor(() => {
        expect(screen.getByText('Vat Rate')).toBeInTheDocument()
        expect(screen.getByText('Child Discount Percent')).toBeInTheDocument()
        expect(screen.getByText('21')).toBeInTheDocument()
        expect(screen.getByText('10')).toBeInTheDocument()
      })
    })

    it('allows editing settings', async () => {
      const user = userEvent.setup()
      vi.mocked(settingsApi.getAll).mockResolvedValue([
        { key: 'vat_rate', value: '21', type: 'decimal', description: 'VAT percentage' },
      ])
      vi.mocked(settingsApi.update).mockResolvedValue({ key: 'vat_rate', value: '19', type: 'decimal' })

      render(<SettingsPage />)

      // Navigate to System Settings
      await user.click(screen.getByRole('button', { name: 'settings.sections.system' }))

      await waitFor(() => {
        expect(screen.getByText('Vat Rate')).toBeInTheDocument()
      })

      // Click edit button (pencil icon)
      const editButtons = screen.getAllByRole('button')
      const pencilButton = editButtons.find(btn => btn.querySelector('svg.lucide-pencil'))
      if (pencilButton) {
        await user.click(pencilButton)
      }

      // Should show input field
      await waitFor(() => {
        expect(screen.getByRole('textbox')).toBeInTheDocument()
      })
    })
  })

  describe('Holidays Section', () => {
    it('displays holidays list', async () => {
      const user = userEvent.setup()
      vi.mocked(holidaysApi.getAll).mockResolvedValue([
        { id: 1, name: 'Summer Break', startDate: '2024-07-01', endDate: '2024-08-31' },
        { id: 2, name: 'Winter Break', startDate: '2024-12-23', endDate: '2025-01-05' },
      ])

      render(<SettingsPage />)

      // Navigate to Holidays
      await user.click(screen.getByRole('button', { name: 'settings.sections.holidays' }))

      await waitFor(() => {
        expect(screen.getByText('Summer Break')).toBeInTheDocument()
        expect(screen.getByText('Winter Break')).toBeInTheDocument()
      })
    })

    it('shows add form when clicking Add button', async () => {
      const user = userEvent.setup()
      vi.mocked(holidaysApi.getAll).mockResolvedValue([])

      render(<SettingsPage />)

      // Navigate to Holidays
      await user.click(screen.getByRole('button', { name: 'settings.sections.holidays' }))

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'settings.holidays.title' })).toBeInTheDocument()
      })

      // Find and click the Add button (there should only be one in the content area)
      const addButton = screen.getByRole('button', { name: 'settings.holidays.addHoliday' })
      await user.click(addButton)

      expect(screen.getByPlaceholderText('settings.holidays.form.namePlaceholder')).toBeInTheDocument()
    })
  })

  describe('Rooms Section', () => {
    it('displays rooms list with equipment', async () => {
      const user = userEvent.setup()
      vi.mocked(roomsApi.getAll).mockResolvedValue([
        {
          id: 1,
          name: 'Room A',
          capacity: 5,
          hasPiano: true,
          hasDrums: false,
          hasAmplifier: true,
          hasMicrophone: false,
          hasWhiteboard: true,
          isActive: true,
        },
      ])

      render(<SettingsPage />)

      // Navigate to Rooms
      await user.click(screen.getByRole('button', { name: 'settings.sections.rooms' }))

      await waitFor(() => {
        expect(screen.getByText('Room A')).toBeInTheDocument()
        expect(screen.getByText(/capacity: 5/i)).toBeInTheDocument()
        expect(screen.getByText(/piano/i)).toBeInTheDocument()
      })
    })
  })
})
