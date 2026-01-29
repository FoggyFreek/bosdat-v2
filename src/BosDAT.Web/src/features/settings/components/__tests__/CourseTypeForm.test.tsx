import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@/test/utils'
import { CourseTypeForm, type CourseTypeFormProps } from '../CourseTypeForm'
import { DEFAULT_FORM_DATA, type CourseTypeFormData } from '../../hooks/useCourseTypeForm'
import type { Instrument } from '@/features/instruments/types'
import type { CourseType } from '@/features/course-types/types'

const mockInstruments: Instrument[] = [
  { id: 1, name: 'Piano', category: 'Keyboard', isActive: true },
  { id: 2, name: 'Guitar', category: 'String', isActive: true },
]

const mockCourseType: CourseType = {
  id: '1',
  name: 'Piano 30min',
  instrumentId: 1,
  instrumentName: 'Piano',
  durationMinutes: 30,
  type: 'Individual',
  maxStudents: 1,
  isActive: true,
  activeCourseCount: 2,
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
}

const defaultProps: CourseTypeFormProps = {
  formData: DEFAULT_FORM_DATA,
  instruments: mockInstruments,
  editId: null,
  editingCourseType: null,
  useCustomDuration: false,
  teacherWarning: null,
  childDiscountPercent: 10,
  isFormValid: false,
  isSubmitting: false,
  onFormDataChange: vi.fn(),
  onInstrumentChange: vi.fn(),
  onTypeChange: vi.fn(),
  onAdultPriceChange: vi.fn(),
  onChildPriceChange: vi.fn(),
  onCustomDurationToggle: vi.fn(),
  onCancel: vi.fn(),
  onSubmit: vi.fn(),
}

describe('CourseTypeForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders "New Course Type" title when creating', () => {
      render(<CourseTypeForm {...defaultProps} />)
      expect(screen.getByText('New Course Type')).toBeInTheDocument()
    })

    it('renders "Edit Course Type" title when editing', () => {
      render(<CourseTypeForm {...defaultProps} editId="1" />)
      expect(screen.getByText('Edit Course Type')).toBeInTheDocument()
    })

    it('renders all form fields', () => {
      render(<CourseTypeForm {...defaultProps} />)

      expect(screen.getByText('Name *')).toBeInTheDocument()
      expect(screen.getByText('Instrument *')).toBeInTheDocument()
      expect(screen.getByText('Duration (min)')).toBeInTheDocument()
      expect(screen.getByText('Type')).toBeInTheDocument()
      expect(screen.getByText('Price Adult *')).toBeInTheDocument()
      expect(screen.getByText('Price Child')).toBeInTheDocument()
    })

    it('renders instrument options', () => {
      render(<CourseTypeForm {...defaultProps} />)

      // Find the instrument select by placeholder text
      const instrumentSelect = screen.getByText('Select instrument').closest('button')
      fireEvent.click(instrumentSelect!)

      expect(screen.getByText('Piano')).toBeInTheDocument()
      expect(screen.getByText('Guitar')).toBeInTheDocument()
    })

    it('renders type select with default value', () => {
      render(<CourseTypeForm {...defaultProps} />)

      // The type select should show the default value "Individual"
      // The selects are: Instrument (0), Duration (1), Type (2)
      const typeSelect = screen.getAllByRole('combobox')[2]
      expect(typeSelect).toBeInTheDocument()
      expect(typeSelect).toHaveTextContent('Individual')
    })

    it('renders Max Students field for non-Individual types', () => {
      const formData: CourseTypeFormData = { ...DEFAULT_FORM_DATA, type: 'Group' }
      render(<CourseTypeForm {...defaultProps} formData={formData} />)

      expect(screen.getByText('Max Students')).toBeInTheDocument()
    })

    it('does not render Max Students field for Individual type', () => {
      render(<CourseTypeForm {...defaultProps} />)
      expect(screen.queryByText('Max Students')).not.toBeInTheDocument()
    })

    it('renders child discount info text', () => {
      render(<CourseTypeForm {...defaultProps} childDiscountPercent={15} />)
      expect(screen.getByText('Default: 15% discount from adult price')).toBeInTheDocument()
    })
  })

  describe('teacher warning', () => {
    it('renders teacher warning when provided', () => {
      render(
        <CourseTypeForm
          {...defaultProps}
          teacherWarning="No active teachers teach this instrument"
        />
      )

      expect(
        screen.getByText('No active teachers teach this instrument')
      ).toBeInTheDocument()
    })

    it('does not render teacher warning when null', () => {
      render(<CourseTypeForm {...defaultProps} teacherWarning={null} />)
      expect(
        screen.queryByText('No active teachers teach this instrument')
      ).not.toBeInTheDocument()
    })
  })

  describe('pricing version warning', () => {
    it('renders pricing version warning when editing and cannot edit directly', () => {
      const courseTypeWithLockedPricing: CourseType = {
        ...mockCourseType,
        canEditPricingDirectly: false,
      }

      render(
        <CourseTypeForm
          {...defaultProps}
          editId="1"
          editingCourseType={courseTypeWithLockedPricing}
        />
      )

      expect(
        screen.getByText(/Pricing has been used in invoices/)
      ).toBeInTheDocument()
    })

    it('does not render pricing warning when can edit directly', () => {
      render(
        <CourseTypeForm
          {...defaultProps}
          editId="1"
          editingCourseType={mockCourseType}
        />
      )

      expect(
        screen.queryByText(/Pricing has been used in invoices/)
      ).not.toBeInTheDocument()
    })
  })

  describe('custom duration toggle', () => {
    it('shows preset select when useCustomDuration is false', () => {
      render(<CourseTypeForm {...defaultProps} useCustomDuration={false} />)

      // Should show "Custom" button to switch to custom mode
      expect(screen.getByRole('button', { name: 'Custom' })).toBeInTheDocument()
    })

    it('shows custom input when useCustomDuration is true', () => {
      render(<CourseTypeForm {...defaultProps} useCustomDuration={true} />)

      // Should show "Preset" button to switch back
      expect(screen.getByRole('button', { name: 'Preset' })).toBeInTheDocument()
      expect(screen.getByPlaceholderText('Custom')).toBeInTheDocument()
    })

    it('calls onCustomDurationToggle when toggle button clicked', () => {
      const onCustomDurationToggle = vi.fn()
      render(
        <CourseTypeForm
          {...defaultProps}
          useCustomDuration={false}
          onCustomDurationToggle={onCustomDurationToggle}
        />
      )

      fireEvent.click(screen.getByRole('button', { name: 'Custom' }))
      expect(onCustomDurationToggle).toHaveBeenCalled()
    })
  })

  describe('form interactions', () => {
    it('calls onFormDataChange when name changes', () => {
      const onFormDataChange = vi.fn()
      render(<CourseTypeForm {...defaultProps} onFormDataChange={onFormDataChange} />)

      const nameInput = screen.getByPlaceholderText('e.g., Piano 30 min')
      fireEvent.change(nameInput, { target: { value: 'Test Name' } })

      expect(onFormDataChange).toHaveBeenCalledWith({ name: 'Test Name' })
    })

    it('calls onAdultPriceChange when adult price changes', () => {
      const onAdultPriceChange = vi.fn()
      render(<CourseTypeForm {...defaultProps} onAdultPriceChange={onAdultPriceChange} />)

      const priceInputs = screen.getAllByRole('spinbutton')
      const adultPriceInput = priceInputs[0]
      fireEvent.change(adultPriceInput, { target: { value: '50' } })

      expect(onAdultPriceChange).toHaveBeenCalledWith('50')
    })

    it('calls onChildPriceChange when child price changes', () => {
      const onChildPriceChange = vi.fn()
      render(<CourseTypeForm {...defaultProps} onChildPriceChange={onChildPriceChange} />)

      const priceInputs = screen.getAllByRole('spinbutton')
      const childPriceInput = priceInputs[1]
      fireEvent.change(childPriceInput, { target: { value: '45' } })

      expect(onChildPriceChange).toHaveBeenCalledWith('45')
    })

    it('calls onCancel when cancel button clicked', () => {
      const onCancel = vi.fn()
      render(<CourseTypeForm {...defaultProps} onCancel={onCancel} />)

      fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
      expect(onCancel).toHaveBeenCalled()
    })

    it('calls onSubmit when create button clicked', () => {
      const onSubmit = vi.fn()
      render(<CourseTypeForm {...defaultProps} isFormValid={true} onSubmit={onSubmit} />)

      fireEvent.click(screen.getByRole('button', { name: 'Create' }))
      expect(onSubmit).toHaveBeenCalled()
    })

    it('calls onSubmit when save button clicked in edit mode', () => {
      const onSubmit = vi.fn()
      render(
        <CourseTypeForm
          {...defaultProps}
          editId="1"
          isFormValid={true}
          onSubmit={onSubmit}
        />
      )

      fireEvent.click(screen.getByRole('button', { name: 'Save' }))
      expect(onSubmit).toHaveBeenCalled()
    })
  })

  describe('button states', () => {
    it('disables submit button when form is invalid', () => {
      render(<CourseTypeForm {...defaultProps} isFormValid={false} />)
      expect(screen.getByRole('button', { name: 'Create' })).toBeDisabled()
    })

    it('disables submit button when submitting', () => {
      render(<CourseTypeForm {...defaultProps} isFormValid={true} isSubmitting={true} />)
      expect(screen.getByRole('button', { name: 'Create' })).toBeDisabled()
    })

    it('enables submit button when form is valid and not submitting', () => {
      render(<CourseTypeForm {...defaultProps} isFormValid={true} isSubmitting={false} />)
      expect(screen.getByRole('button', { name: 'Create' })).not.toBeDisabled()
    })
  })

  describe('child price validation styling', () => {
    it('applies red border when child price exceeds adult price', () => {
      const formData: CourseTypeFormData = {
        ...DEFAULT_FORM_DATA,
        priceAdult: '50',
        priceChild: '60',
      }
      render(<CourseTypeForm {...defaultProps} formData={formData} />)

      const priceInputs = screen.getAllByRole('spinbutton')
      const childPriceInput = priceInputs[1]
      expect(childPriceInput).toHaveClass('border-red-500')
    })

    it('does not apply red border when child price is valid', () => {
      const formData: CourseTypeFormData = {
        ...DEFAULT_FORM_DATA,
        priceAdult: '50',
        priceChild: '40',
      }
      render(<CourseTypeForm {...defaultProps} formData={formData} />)

      const priceInputs = screen.getAllByRole('spinbutton')
      const childPriceInput = priceInputs[1]
      expect(childPriceInput).not.toHaveClass('border-red-500')
    })
  })

  describe('custom duration input', () => {
    it('renders custom duration input when useCustomDuration is true', () => {
      const formData: CourseTypeFormData = {
        ...DEFAULT_FORM_DATA,
        customDuration: '25',
      }
      render(
        <CourseTypeForm
          {...defaultProps}
          formData={formData}
          useCustomDuration={true}
        />
      )

      const customInput = screen.getByPlaceholderText('Custom')
      expect(customInput).toHaveValue(25)
    })

    it('calls onFormDataChange when custom duration changes', () => {
      const onFormDataChange = vi.fn()
      const formData: CourseTypeFormData = {
        ...DEFAULT_FORM_DATA,
        customDuration: '25',
      }
      render(
        <CourseTypeForm
          {...defaultProps}
          formData={formData}
          useCustomDuration={true}
          onFormDataChange={onFormDataChange}
        />
      )

      const customInput = screen.getByPlaceholderText('Custom')
      fireEvent.change(customInput, { target: { value: '35' } })

      expect(onFormDataChange).toHaveBeenCalledWith({ customDuration: '35' })
    })
  })

  describe('max students field for Group type', () => {
    it('renders and handles max students input for Group type', () => {
      const onFormDataChange = vi.fn()
      const formData: CourseTypeFormData = {
        ...DEFAULT_FORM_DATA,
        type: 'Group',
        maxStudents: '6',
      }
      render(
        <CourseTypeForm
          {...defaultProps}
          formData={formData}
          onFormDataChange={onFormDataChange}
        />
      )

      const maxStudentsInput = screen.getByDisplayValue('6')
      expect(maxStudentsInput).toBeInTheDocument()

      fireEvent.change(maxStudentsInput, { target: { value: '8' } })
      expect(onFormDataChange).toHaveBeenCalledWith({ maxStudents: '8' })
    })
  })

  describe('duration select', () => {
    it('calls onFormDataChange when duration preset changes', () => {
      const onFormDataChange = vi.fn()
      render(
        <CourseTypeForm
          {...defaultProps}
          onFormDataChange={onFormDataChange}
          useCustomDuration={false}
        />
      )

      // The duration select is the second combobox (after instrument)
      const durationSelect = screen.getAllByRole('combobox')[1]
      fireEvent.click(durationSelect)

      // Select 45 min option
      fireEvent.click(screen.getByText('45 min'))

      expect(onFormDataChange).toHaveBeenCalledWith({ durationMinutes: '45' })
    })
  })
})
