import { useQuery } from '@tanstack/react-query'
import { settingsApi } from '@/features/settings/api'

export function useSchoolName() {
  const { data, isLoading } = useQuery({
    queryKey: ['settings', 'school_name'],
    queryFn: () => settingsApi.getByKey('school_name'),
  })

  return {
    schoolName: data?.value ?? '',
    isLoading,
  }
}
