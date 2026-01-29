import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import type { ReactNode } from 'react'
import { CourseTypesSection } from '../CourseTypesSection'
import { FormDirtyProvider } from '@/context/FormDirtyContext'
import { courseTypesApi, instrumentsApi, settingsApi } from '@/services/api'
import type { CourseType } from '@/features/course-types/types'
import type { Instrument } from '@/features/instruments/types'

vi.mock('@/services/api', () => ({
  courseTypesApi: {
    getAll: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    reactivate: vi.fn(),
    updatePricing: vi.fn(),
    createPricingVersion: vi.fn(),
    getTeacherCountForInstrument: vi.fn(),
  },
  instrumentsApi: {
    getAll: vi.fn(),
  },
  settingsApi: {
    getAll: vi.fn(),
  },
}))

const mockCourseTypes: CourseType[] = [
  {
    id: '1',
    name: 'Piano 30min',
    instrumentId: 1,
    instrumentName: 'Piano',
    durationMinutes: 30,
    type: 'Individual',
    maxStudents: 1,
    isActive: true,
    activeCourseCount: 0,
    hasTeachersForCourseType: true,
    currentPricing: {
      id: 'p1',
      courseTypeId: '1',
      priceAdult: 45.0,
      priceChild: 40.5,
      validFrom: '2024-01-01',
      validUntil: null,
      isCurrent: true,
      createdAt: '2024-01-01',
    },
    pricingHistory: [],
    canEditPricingDirectly: true,
  },
]

const mockInstruments: Instrument[] = [
  { id: 1, name: 'Piano', category: 'Keyboard', isActive: true },
  { id: 2, name: 'Guitar', category: 'String', isActive: true },
]

const mockSettings = [
  { key: 'child_discount_percent', value: '10' },
  { key: 'group_max_students', value: '6' },
  { key: 'workshop_max_students', value: '12' },
]

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter
        future={{
          v7_startTransition: true,
          v7_relativeSplatPath: true,
        }}
      >
        <FormDirtyProvider>{children}</FormDirtyProvider>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

const renderWithProviders = (ui: ReactNode) => {
  const Wrapper = createWrapper()
  return render(<Wrapper>{ui}</Wrapper>)
}

describe('CourseTypesSection', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(courseTypesApi.getAll).mockResolvedValue(mockCourseTypes)
    vi.mocked(instrumentsApi.getAll).mockResolvedValue(mockInstruments)
    vi.mocked(settingsApi.getAll).mockResolvedValue(mockSettings)
    vi.mocked(courseTypesApi.getTeacherCountForInstrument).mockResolvedValue(1)
  })

  describe('rendering', () => {
    it('renders card with title and description', async () => {
      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByText('Course Types')).toBeInTheDocument()
      })
      expect(screen.getByText('Configure types of courses and pricing')).toBeInTheDocument()
    })

    it('renders Add button', async () => {
      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /add/i })).toBeInTheDocument()
      })
    })

    it('renders loading state initially', () => {
      vi.mocked(courseTypesApi.getAll).mockImplementation(() => new Promise(() => {}))

      renderWithProviders(<CourseTypesSection />)

      const spinner = document.querySelector('.animate-spin')
      expect(spinner).toBeInTheDocument()
    })

    it('renders course types after loading', async () => {
      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByText('Piano 30min')).toBeInTheDocument()
      })
    })
  })

  describe('add course type flow', () => {
    it('shows form when Add button is clicked', async () => {
      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /add/i })).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /add/i }))

      expect(screen.getByText('New Course Type')).toBeInTheDocument()
    })

    it('hides form when Cancel is clicked', async () => {
      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /add/i })).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /add/i }))
      expect(screen.getByText('New Course Type')).toBeInTheDocument()

      fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
      expect(screen.queryByText('New Course Type')).not.toBeInTheDocument()
    })

    it('creates course type when form is submitted', async () => {
      vi.mocked(courseTypesApi.create).mockResolvedValue({ id: '2' })

      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /add/i })).toBeInTheDocument()
      })

      // Open the form
      fireEvent.click(screen.getByRole('button', { name: /add/i }))

      // Fill in required fields
      const nameInput = screen.getByPlaceholderText('e.g., Piano 30 min')
      fireEvent.change(nameInput, { target: { value: 'Test Course' } })

      // Select instrument
      const instrumentSelect = screen.getByText('Select instrument').closest('button')
      fireEvent.click(instrumentSelect!)
      fireEvent.click(screen.getByText('Piano'))

      // Fill in price
      const priceInputs = screen.getAllByRole('spinbutton')
      fireEvent.change(priceInputs[0], { target: { value: '50' } })

      // Submit
      fireEvent.click(screen.getByRole('button', { name: 'Create' }))

      await waitFor(() => {
        expect(courseTypesApi.create).toHaveBeenCalled()
      })
    })
  })

  describe('edit course type flow', () => {
    it('shows edit form when Edit button is clicked', async () => {
      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByText('Piano 30min')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByTitle('Edit'))

      expect(screen.getByText('Edit Course Type')).toBeInTheDocument()
    })

    it('populates form with course type data', async () => {
      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByText('Piano 30min')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByTitle('Edit'))

      const nameInput = screen.getByPlaceholderText('e.g., Piano 30 min')
      expect(nameInput).toHaveValue('Piano 30min')
    })

    it('updates course type when form is submitted', async () => {
      vi.mocked(courseTypesApi.update).mockResolvedValue({ id: '1' })

      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByText('Piano 30min')).toBeInTheDocument()
      })

      // Open edit form
      fireEvent.click(screen.getByTitle('Edit'))

      // Change name
      const nameInput = screen.getByPlaceholderText('e.g., Piano 30 min')
      fireEvent.change(nameInput, { target: { value: 'Piano 30min Updated' } })

      // Submit
      fireEvent.click(screen.getByRole('button', { name: 'Save' }))

      await waitFor(() => {
        expect(courseTypesApi.update).toHaveBeenCalled()
      })
    })
  })

  describe('archive/reactivate flow', () => {
    it('archives course type when Archive button is clicked', async () => {
      vi.mocked(courseTypesApi.delete).mockResolvedValue(undefined)

      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByText('Piano 30min')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByTitle('Archive'))

      await waitFor(() => {
        expect(courseTypesApi.delete).toHaveBeenCalledWith('1')
      })
    })

    it('reactivates course type when Reactivate button is clicked', async () => {
      const inactiveCourseTypes: CourseType[] = [
        { ...mockCourseTypes[0], isActive: false },
      ]
      vi.mocked(courseTypesApi.getAll).mockResolvedValue(inactiveCourseTypes)
      vi.mocked(courseTypesApi.reactivate).mockResolvedValue({})

      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByText('Archived')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByTitle('Reactivate'))

      await waitFor(() => {
        expect(courseTypesApi.reactivate).toHaveBeenCalledWith('1')
      })
    })
  })

  describe('error handling', () => {
    it('displays error alert when mutation fails', async () => {
      vi.mocked(courseTypesApi.create).mockRejectedValue({
        response: { data: { message: 'Something went wrong' } },
      })

      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /add/i })).toBeInTheDocument()
      })

      // Open form
      fireEvent.click(screen.getByRole('button', { name: /add/i }))

      // Fill required fields
      const nameInput = screen.getByPlaceholderText('e.g., Piano 30 min')
      fireEvent.change(nameInput, { target: { value: 'Test' } })

      const instrumentSelect = screen.getByText('Select instrument').closest('button')
      fireEvent.click(instrumentSelect!)
      fireEvent.click(screen.getByText('Piano'))

      const priceInputs = screen.getAllByRole('spinbutton')
      fireEvent.change(priceInputs[0], { target: { value: '50' } })

      // Submit
      fireEvent.click(screen.getByRole('button', { name: 'Create' }))

      await waitFor(() => {
        expect(screen.getByText('Something went wrong')).toBeInTheDocument()
      })
    })

    it('clears error when dismiss button is clicked', async () => {
      vi.mocked(courseTypesApi.create).mockRejectedValue({
        response: { data: { message: 'Error message' } },
      })

      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /add/i })).toBeInTheDocument()
      })

      // Open form and submit to trigger error
      fireEvent.click(screen.getByRole('button', { name: /add/i }))

      const nameInput = screen.getByPlaceholderText('e.g., Piano 30 min')
      fireEvent.change(nameInput, { target: { value: 'Test' } })

      const instrumentSelect = screen.getByText('Select instrument').closest('button')
      fireEvent.click(instrumentSelect!)
      fireEvent.click(screen.getByText('Piano'))

      const priceInputs = screen.getAllByRole('spinbutton')
      fireEvent.change(priceInputs[0], { target: { value: '50' } })

      fireEvent.click(screen.getByRole('button', { name: 'Create' }))

      await waitFor(() => {
        expect(screen.getByText('Error message')).toBeInTheDocument()
      })

      // Dismiss error
      fireEvent.click(screen.getByText('×'))

      await waitFor(() => {
        expect(screen.queryByText('Error message')).not.toBeInTheDocument()
      })
    })
  })

  describe('empty state', () => {
    it('renders empty message when no course types exist', async () => {
      vi.mocked(courseTypesApi.getAll).mockResolvedValue([])

      renderWithProviders(<CourseTypesSection />)

      await waitFor(() => {
        expect(screen.getByText('No course types configured')).toBeInTheDocument()
      })
    })
  })
})
