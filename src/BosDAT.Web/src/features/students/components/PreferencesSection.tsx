import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Settings2 } from 'lucide-react'

export function PreferencesSection() {
  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Preferences</h2>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings2 className="h-5 w-5" />
            Student Preferences
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <Settings2 className="h-12 w-12 text-muted-foreground/50 mb-4" />
            <p className="text-lg font-medium text-muted-foreground">Coming soon</p>
            <p className="text-sm text-muted-foreground mt-1">
              Student preferences will be available in a future update.
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
