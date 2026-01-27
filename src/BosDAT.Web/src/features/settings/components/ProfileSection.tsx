import { User } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function ProfileSection() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Profile</CardTitle>
        <CardDescription>Manage your account profile</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-center py-12 text-muted-foreground">
          <div className="text-center">
            <User className="h-12 w-12 mx-auto mb-4 opacity-50" />
            <p>Profile settings coming soon</p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
