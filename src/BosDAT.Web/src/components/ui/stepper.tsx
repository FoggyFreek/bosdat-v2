import { memo } from 'react'
import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'

export interface StepConfig {
  readonly title: string
  readonly description?: string
}

interface StepperProps {
  readonly steps: readonly StepConfig[]
  readonly currentStep: number
  readonly onStepChange: (step: number) => void
}

export const Stepper = memo(function Stepper({ steps, currentStep, onStepChange }: StepperProps) {
  const handleStepClick = (stepIndex: number) => {
    if (stepIndex <= currentStep) {
      onStepChange(stepIndex)
    }
  }

  return (
    <nav aria-label="Progress" className="w-full">
      <ol className="flex items-center justify-between">
        {steps.map((step, index) => {
          const isCompleted = index < currentStep
          const isCurrent = index === currentStep
          const isPending = index > currentStep

          return (
            <li key={step.title} className="relative flex-1">
              <div className="flex flex-col min-w-[128px] items-center">
                <button
                  type="button"
                  onClick={() => handleStepClick(index)}
                  disabled={isPending}
                  aria-current={isCurrent ? 'step' : undefined}
                  className={cn(
                    'flex h-10 w-10 items-center justify-center rounded-full border-2 text-sm font-semibold transition-colors',
                    isCompleted && 'border-primary bg-primary text-primary-foreground',
                    isCurrent && 'border-primary bg-background text-primary',
                    isPending && 'border-muted bg-background text-muted-foreground cursor-not-allowed'
                  )}
                >
                  {isCompleted ? (
                    <Check className="h-5 w-5" data-testid="check-icon" />
                  ) : (
                    <span>{index + 1}</span>
                  )}
                </button>
                <div className="mt-2 text-center">
                  <span
                    className={cn(
                      'text-sm font-medium',
                      (isCompleted || isCurrent) && 'text-foreground',
                      isPending && 'text-muted-foreground'
                    )}
                  >
                    {step.title}
                  </span>
                  {step.description && (
                    <p
                      className={cn(
                        'text-xs mt-0.5 min-h-[40px] max-w-[120px]',
                        (isCompleted || isCurrent) && 'text-muted-foreground',
                        isPending && 'text-muted-foreground/60'
                      )}
                    >
                      {step.description}
                    </p>
                  )}
                </div>
              </div>
              {index < steps.length - 1 && (
                <div
                  className={cn(
                    'absolute left-[calc(50%+20px)] right-[calc(-50%+20px)] top-5 h-0.5 -translate-y-1/2',
                    index < currentStep ? 'bg-primary' : 'bg-muted'
                  )}
                />
              )}
            </li>
          )
        })}
      </ol>
    </nav>
  )
})
