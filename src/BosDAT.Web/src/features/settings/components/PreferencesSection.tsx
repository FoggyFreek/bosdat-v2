import { SlidersHorizontal } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function PreferencesSection() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Preferences</CardTitle>
        <CardDescription>Customize your experience</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-center py-12 text-muted-foreground">
          <div className="text-center">
            <SlidersHorizontal className="h-12 w-12 mx-auto mb-4 opacity-50" />
            <p>Preference settings coming soon</p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
