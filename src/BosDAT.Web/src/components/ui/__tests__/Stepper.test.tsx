import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@/test/utils'
import userEvent from '@testing-library/user-event'
import { Stepper } from '../stepper'

const mockSteps = [
  { title: 'Lesson Details', description: 'Configure course type and schedule' },
  { title: 'Student Selection', description: 'Select students to enroll' },
  { title: 'Pricing', description: 'Review pricing details' },
  { title: 'Confirmation', description: 'Review and confirm enrollment' },
]

describe('Stepper', () => {
  it('renders all step indicators', () => {
    render(
      <Stepper
        steps={mockSteps}
        currentStep={0}
        onStepChange={() => {}}
      />
    )

    expect(screen.getByText('Lesson Details')).toBeInTheDocument()
    expect(screen.getByText('Student Selection')).toBeInTheDocument()
    expect(screen.getByText('Pricing')).toBeInTheDocument()
    expect(screen.getByText('Confirmation')).toBeInTheDocument()
  })

  it('highlights current step', () => {
    render(
      <Stepper
        steps={mockSteps}
        currentStep={1}
        onStepChange={() => {}}
      />
    )

    const stepIndicators = screen.getAllByRole('button')
    // Step 2 (index 1) should have active styling
    expect(stepIndicators[1]).toHaveAttribute('aria-current', 'step')
  })

  it('shows checkmarks for completed steps', () => {
    render(
      <Stepper
        steps={mockSteps}
        currentStep={2}
        onStepChange={() => {}}
      />
    )

    // Steps 0 and 1 should be completed (before current step 2)
    const completedIcons = screen.getAllByTestId('check-icon')
    expect(completedIcons).toHaveLength(2)
  })

  it('allows navigation to completed steps only', async () => {
    const user = userEvent.setup()
    const onStepChange = vi.fn()

    render(
      <Stepper
        steps={mockSteps}
        currentStep={2}
        onStepChange={onStepChange}
      />
    )

    const stepButtons = screen.getAllByRole('button')

    // Click on completed step (step 0)
    await user.click(stepButtons[0])
    expect(onStepChange).toHaveBeenCalledWith(0)

    // Click on completed step (step 1)
    await user.click(stepButtons[1])
    expect(onStepChange).toHaveBeenCalledWith(1)

    // Click on current step (step 2)
    await user.click(stepButtons[2])
    expect(onStepChange).toHaveBeenCalledWith(2)
  })

  it('does not navigate to future steps', async () => {
    const user = userEvent.setup()
    const onStepChange = vi.fn()

    render(
      <Stepper
        steps={mockSteps}
        currentStep={1}
        onStepChange={onStepChange}
      />
    )

    const stepButtons = screen.getAllByRole('button')

    // Click on future step (step 2 while on step 1)
    await user.click(stepButtons[2])
    expect(onStepChange).not.toHaveBeenCalledWith(2)

    // Click on future step (step 3 while on step 1)
    await user.click(stepButtons[3])
    expect(onStepChange).not.toHaveBeenCalledWith(3)
  })

  it('shows step numbers for pending steps', () => {
    render(
      <Stepper
        steps={mockSteps}
        currentStep={0}
        onStepChange={() => {}}
      />
    )

    // Pending steps should show their number
    expect(screen.getByText('2')).toBeInTheDocument()
    expect(screen.getByText('3')).toBeInTheDocument()
    expect(screen.getByText('4')).toBeInTheDocument()
  })

  it('has proper ARIA attributes for accessibility', () => {
    render(
      <Stepper
        steps={mockSteps}
        currentStep={1}
        onStepChange={() => {}}
      />
    )

    const navigation = screen.getByRole('navigation')
    expect(navigation).toHaveAttribute('aria-label', 'Progress')

    const stepList = screen.getByRole('list')
    expect(stepList).toBeInTheDocument()

    const stepButtons = screen.getAllByRole('button')
    stepButtons.forEach((button, index) => {
      if (index === 1) {
        expect(button).toHaveAttribute('aria-current', 'step')
      } else {
        expect(button).not.toHaveAttribute('aria-current', 'step')
      }
    })
  })

  it('disables future step buttons', () => {
    render(
      <Stepper
        steps={mockSteps}
        currentStep={1}
        onStepChange={() => {}}
      />
    )

    const stepButtons = screen.getAllByRole('button')

    // Future steps should be disabled
    expect(stepButtons[2]).toBeDisabled()
    expect(stepButtons[3]).toBeDisabled()

    // Completed and current steps should be enabled
    expect(stepButtons[0]).not.toBeDisabled()
    expect(stepButtons[1]).not.toBeDisabled()
  })
})
