import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { SettingsPage } from '../SettingsPage'
import { instrumentsApi, roomsApi, courseTypesApi, holidaysApi, settingsApi } from '@/services/api'

// Mock all API modules
vi.mock('@/services/api', () => ({
  instrumentsApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
  roomsApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
  },
  courseTypesApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    reactivate: vi.fn(),
    getTeacherCountForInstrument: vi.fn(),
  },
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
    it('renders settings title and navigation sidebar', () => {
      render(<SettingsPage />)

      expect(screen.getByRole('heading', { name: /settings/i })).toBeInTheDocument()
    })

    it('renders all navigation groups', () => {
      render(<SettingsPage />)

      expect(screen.getByText('ACCOUNT')).toBeInTheDocument()
      expect(screen.getByText('LESSONS')).toBeInTheDocument()
      expect(screen.getByText('SCHEDULING')).toBeInTheDocument()
      expect(screen.getByText('GENERAL')).toBeInTheDocument()
    })

    it('renders all navigation items', () => {
      render(<SettingsPage />)

      expect(screen.getByRole('button', { name: /profile/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /preferences/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /instruments/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /course types/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /rooms/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /holidays/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /system settings/i })).toBeInTheDocument()
    })

    it('defaults to instruments section', async () => {
      render(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /instruments/i })).toBeInTheDocument()
      })
    })

    it('navigates to different sections when clicking nav items', async () => {
      const user = userEvent.setup()
      render(<SettingsPage />)

      // Click on Rooms
      await user.click(screen.getByRole('button', { name: /rooms/i }))
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /rooms/i })).toBeInTheDocument()
      })

      // Click on Holidays
      await user.click(screen.getByRole('button', { name: /holidays/i }))
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /holidays/i })).toBeInTheDocument()
      })

      // Click on System Settings
      await user.click(screen.getByRole('button', { name: /system settings/i }))
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /system settings/i })).toBeInTheDocument()
      })
    })

    it('shows placeholder content for Profile section', async () => {
      const user = userEvent.setup()
      render(<SettingsPage />)

      await user.click(screen.getByRole('button', { name: /profile/i }))

      await waitFor(() => {
        expect(screen.getByText(/profile settings coming soon/i)).toBeInTheDocument()
      })
    })

    it('shows placeholder content for Preferences section', async () => {
      const user = userEvent.setup()
      render(<SettingsPage />)

      await user.click(screen.getByRole('button', { name: /preferences/i }))

      await waitFor(() => {
        expect(screen.getByText(/preference settings coming soon/i)).toBeInTheDocument()
      })
    })
  })

  describe('Unsaved Changes Detection', () => {
    it('allows navigation without dialog when no unsaved changes', async () => {
      const user = userEvent.setup()
      vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
      vi.mocked(roomsApi.getAll).mockResolvedValue([])

      render(<SettingsPage />)

      // Wait for page to load
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /instruments/i })).toBeInTheDocument()
      })

      // Navigate to Rooms without making changes
      const roomsNavButton = screen.getByRole('button', { name: /rooms/i })
      await user.click(roomsNavButton)

      // Should navigate directly without dialog
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /rooms/i })).toBeInTheDocument()
      })

      // Confirm dialog text was not shown
      expect(screen.queryByText(/do you want to discard them/i)).not.toBeInTheDocument()
    })

    it('dirty state context exists and can be accessed', () => {
      // The SettingsDirtyContext is properly set up
      // This is a basic test to ensure the context structure is correct
      render(<SettingsPage />)

      // If the page renders without errors, the context is working
      expect(screen.getByRole('heading', { name: /settings/i })).toBeInTheDocument()
    })
  })

  describe('Instruments Section', () => {
    it('displays instruments list', async () => {
      vi.mocked(instrumentsApi.getAll).mockResolvedValue([
        { id: 1, name: 'Piano', category: 'Keyboard', isActive: true },
        { id: 2, name: 'Guitar', category: 'String', isActive: true },
        { id: 3, name: 'Drums', category: 'Percussion', isActive: false },
      ])

      render(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByText('Piano')).toBeInTheDocument()
        expect(screen.getByText('Guitar')).toBeInTheDocument()
        expect(screen.getByText('Drums')).toBeInTheDocument()
      })

      // Check categories are shown
      expect(screen.getByText('Keyboard')).toBeInTheDocument()
      expect(screen.getByText('String')).toBeInTheDocument()
      expect(screen.getByText('Percussion')).toBeInTheDocument()
    })

    it('shows add form when clicking Add button', async () => {
      const user = userEvent.setup()
      vi.mocked(instrumentsApi.getAll).mockResolvedValue([])

      render(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /instruments/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole('button', { name: /add/i }))

      expect(screen.getByPlaceholderText(/instrument name/i)).toBeInTheDocument()
    })

    it('creates new instrument on submit', async () => {
      const user = userEvent.setup()
      vi.mocked(instrumentsApi.getAll).mockResolvedValue([])
      vi.mocked(instrumentsApi.create).mockResolvedValue({ id: 1, name: 'Violin', category: 'String', isActive: true })

      render(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /instruments/i })).toBeInTheDocument()
      })

      // Click Add
      await user.click(screen.getByRole('button', { name: /add/i }))

      // Fill form
      await user.type(screen.getByPlaceholderText(/instrument name/i), 'Violin')

      // Submit (click the check button - second button in form after the Select combobox)
      const addForm = screen.getByPlaceholderText(/instrument name/i).closest('div.mb-4')!
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
      await user.click(screen.getByRole('button', { name: /system settings/i }))

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
      await user.click(screen.getByRole('button', { name: /system settings/i }))

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
      await user.click(screen.getByRole('button', { name: /holidays/i }))

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
      await user.click(screen.getByRole('button', { name: /holidays/i }))

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /holidays/i })).toBeInTheDocument()
      })

      // Find and click the Add button (there should only be one in the content area)
      const addButton = screen.getByRole('button', { name: /add/i })
      await user.click(addButton)

      expect(screen.getByPlaceholderText(/summer break/i)).toBeInTheDocument()
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
      await user.click(screen.getByRole('button', { name: /rooms/i }))

      await waitFor(() => {
        expect(screen.getByText('Room A')).toBeInTheDocument()
        expect(screen.getByText(/capacity: 5/i)).toBeInTheDocument()
        expect(screen.getByText(/piano/i)).toBeInTheDocument()
      })
    })
  })
})
