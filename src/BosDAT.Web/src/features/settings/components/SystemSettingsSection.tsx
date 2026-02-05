import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Pencil, X, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { settingsApi } from '@/features/settings/api'
import { useFormDirty } from '@/context/FormDirtyContext'
import type { SystemSetting } from '@/features/settings/types'

export function SystemSettingsSection() {
  const queryClient = useQueryClient()
  const [editKey, setEditKey] = useState<string | null>(null)
  const [editValue, setEditValue] = useState('')
  const { setIsDirty } = useFormDirty()

  const { data: settings = [], isLoading } = useQuery<SystemSetting[]>({
    queryKey: ['settings'],
    queryFn: () => settingsApi.getAll(),
  })

  const updateMutation = useMutation({
    mutationFn: ({ key, value }: { key: string; value: string }) => settingsApi.update(key, value),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] })
      setEditKey(null)
      setEditValue('')
      setIsDirty(false)
    },
  })

  const startEdit = (key: string, value: string) => {
    setEditKey(key)
    setEditValue(value)
    setIsDirty(true)
  }

  const cancelEdit = () => {
    setEditKey(null)
    setEditValue('')
    setIsDirty(false)
  }

  const formatSettingName = (key: string) => {
    return key.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase())
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>System Settings</CardTitle>
        <CardDescription>Configure application-wide settings</CardDescription>
      </CardHeader>
      <CardContent>
        {isLoading && (
          <div className="flex items-center justify-center py-8">
            <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
          </div>
        )}

        {!isLoading && settings.length === 0 && (
          <p className="text-muted-foreground">No settings configured</p>
        )}

        {!isLoading && settings.length > 0 && (
          <div className="divide-y">
            {settings.map((setting) => (
              <div key={setting.key} className="flex items-center justify-between py-3">
                {editKey === setting.key ? (
                  <div className="flex gap-2 flex-1 items-center">
                    <div className="flex-1">
                      <p className="font-medium text-sm">{formatSettingName(setting.key)}</p>
                      <Input
                        value={editValue}
                        onChange={(e) => setEditValue(e.target.value)}
                        className="mt-1"
                      />
                    </div>
                    <Button
                      size="icon"
                      onClick={() => updateMutation.mutate({ key: setting.key, value: editValue })}
                      disabled={updateMutation.isPending}
                    >
                      <Check className="h-4 w-4" />
                    </Button>
                    <Button size="icon" variant="ghost" onClick={cancelEdit}>
                      <X className="h-4 w-4" />
                    </Button>
                  </div>
                ) : (
                  <>
                    <div>
                      <p className="font-medium">{formatSettingName(setting.key)}</p>
                      <p className="text-sm text-muted-foreground">{setting.description}</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-mono bg-muted px-2 py-1 rounded">
                        {setting.value}
                      </span>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => startEdit(setting.key, setting.value)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </div>
                  </>
                )}
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
