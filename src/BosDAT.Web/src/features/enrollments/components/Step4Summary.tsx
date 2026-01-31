import { CheckCircle } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'

export function Step4Summary() {
  // Placeholder component for Step 4 - Confirmation
  // Full validation and submission logic will be implemented when
  // the course creation API endpoint is integrated

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium">Confirmation</h3>
        <p className="text-sm text-muted-foreground">
          Review your enrollment details before submitting
        </p>
      </div>

      <Alert className="border-green-200 bg-green-50">
        <CheckCircle className="h-4 w-4 text-green-600" />
        <AlertTitle className="text-green-900">
          Ready to submit
        </AlertTitle>
        <AlertDescription className="text-green-800">
          Your enrollment details have been configured. Conflict detection will be performed when you submit.
        </AlertDescription>
      </Alert>
    </div>
  )
}
