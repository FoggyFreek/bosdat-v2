import { useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useQuery, useMutation } from '@tanstack/react-query'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'
import { accountApi } from '@/features/users/api'

export function SetPasswordPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''

  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [errors, setErrors] = useState<Record<string, string>>({})

  const { data: tokenData, isLoading } = useQuery({
    queryKey: ['validateToken', token],
    queryFn: () => accountApi.validateToken(token),
    enabled: !!token,
    retry: false,
  })

  const setPasswordMutation = useMutation({
    mutationFn: () => accountApi.setPassword({ token, password }),
    onSuccess: () => {
      navigate('/login?accountCreated=true', { replace: true })
    },
  })

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!password) {
      newErrors.password = t('common.validation.required')
    } else if (password.length < 8) {
      newErrors.password = t('setPassword.passwordTooShort')
    }

    if (password !== confirmPassword) {
      newErrors.confirmPassword = t('setPassword.passwordMismatch')
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (validate()) {
      setPasswordMutation.mutate()
    }
  }

  if (!token) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>{t('setPassword.invalidToken')}</CardTitle>
            <CardDescription>{t('setPassword.contactAdmin')}</CardDescription>
          </CardHeader>
        </Card>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
      </div>
    )
  }

  if (!tokenData?.isValid) {
    return (
      <div className="min-h-screen flex items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>{t('setPassword.invalidToken')}</CardTitle>
            <CardDescription>{t('setPassword.contactAdmin')}</CardDescription>
          </CardHeader>
        </Card>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle>{t('setPassword.title')}</CardTitle>
          <CardDescription>{t('setPassword.subtitle')}</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {tokenData.email && (
              <div className="space-y-1.5">
                <Label>{t('users.fields.email')}</Label>
                <Input value={tokenData.email} readOnly />
              </div>
            )}

            <div className="space-y-1.5">
              <Label htmlFor="password">{t('setPassword.password')}</Label>
              <Input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="new-password"
              />
              {errors.password && (
                <p className="text-xs text-destructive">{errors.password}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="confirmPassword">{t('setPassword.confirmPassword')}</Label>
              <Input
                id="confirmPassword"
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
              />
              {errors.confirmPassword && (
                <p className="text-xs text-destructive">{errors.confirmPassword}</p>
              )}
            </div>

            {setPasswordMutation.isError && (
              <p className="text-sm text-destructive">{t('common.errors.unexpected')}</p>
            )}

            <Button
              type="submit"
              className="w-full"
              disabled={setPasswordMutation.isPending}
            >
              {setPasswordMutation.isPending
                ? t('common.actions.saving')
                : t('setPassword.submit')}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
