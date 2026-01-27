import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Receipt } from 'lucide-react'

export function InvoicesSection() {
  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Invoices</h2>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Receipt className="h-5 w-5" />
            Invoice History
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <Receipt className="h-12 w-12 text-muted-foreground/50 mb-4" />
            <p className="text-lg font-medium text-muted-foreground">Coming soon</p>
            <p className="text-sm text-muted-foreground mt-1">
              Invoice management will be available in a future update.
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
