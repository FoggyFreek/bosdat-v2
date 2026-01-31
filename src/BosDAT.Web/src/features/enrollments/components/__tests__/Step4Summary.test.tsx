import { describe, it, expect } from 'vitest'
import { render, screen } from '@/test/utils'
import { Step4Summary } from '../Step4Summary'
import { EnrollmentFormProvider } from '../../context/EnrollmentFormContext'

const renderWithProvider = (ui: React.ReactElement) => {
  return render(<EnrollmentFormProvider>{ui}</EnrollmentFormProvider>)
}

describe('Step4Summary', () => {
  it('renders confirmation step title', () => {
    renderWithProvider(<Step4Summary />)

    expect(screen.getByText(/confirmation/i)).toBeInTheDocument()
    expect(
      screen.getByText(/review your enrollment details before submitting/i)
    ).toBeInTheDocument()
  })

  it('shows ready to submit message', () => {
    renderWithProvider(<Step4Summary />)

    expect(screen.getByText(/ready to submit/i)).toBeInTheDocument()
    expect(
      screen.getByText(/conflict detection will be performed when you submit/i)
    ).toBeInTheDocument()
  })
})
