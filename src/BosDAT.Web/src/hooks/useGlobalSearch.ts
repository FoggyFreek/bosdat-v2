import { useState, useMemo, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { studentsApi } from '@/features/students/api'
import { teachersApi } from '@/features/teachers/api'
import { useDebounce } from './useDebounce'
import type { StudentList } from '@/features/students/types'
import type { TeacherList } from '@/features/teachers/types'

export interface SearchResult {
  id: string
  type: 'student' | 'teacher'
  name: string
  subtitle: string
}

const MAX_RESULTS_PER_GROUP = 5

export function useGlobalSearch() {
  const navigate = useNavigate()
  const [term, setTerm] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(-1)

  const debouncedTerm = useDebounce(term, 300)
  const shouldSearch = debouncedTerm.length >= 2

  const { data: students = [], isLoading: studentsLoading } = useQuery<StudentList[]>({
    queryKey: ['students', 'search', debouncedTerm],
    queryFn: () => studentsApi.getAll({ search: debouncedTerm }),
    enabled: shouldSearch,
    staleTime: 30000,
  })

  const { data: allTeachers = [], isLoading: teachersLoading } = useQuery<TeacherList[]>({
    queryKey: ['teachers', 'active'],
    queryFn: () => teachersApi.getAll({ activeOnly: true }),
    staleTime: 30000,
  })

  const filteredTeachers = useMemo(() => {
    if (!shouldSearch) return []
    const lowerTerm = debouncedTerm.toLowerCase()
    return allTeachers
      .filter(
        (t) =>
          t.fullName.toLowerCase().includes(lowerTerm) ||
          t.email.toLowerCase().includes(lowerTerm)
      )
      .slice(0, MAX_RESULTS_PER_GROUP)
  }, [allTeachers, debouncedTerm, shouldSearch])

  const results: SearchResult[] = useMemo(() => {
    if (!shouldSearch) return []

    const studentResults: SearchResult[] = students
      .slice(0, MAX_RESULTS_PER_GROUP)
      .map((s) => ({
        id: s.id,
        type: 'student' as const,
        name: s.fullName,
        subtitle: s.email,
      }))

    const teacherResults: SearchResult[] = filteredTeachers.map((t) => ({
      id: t.id,
      type: 'teacher' as const,
      name: t.fullName,
      subtitle: t.email,
    }))

    return [...studentResults, ...teacherResults]
  }, [students, filteredTeachers, shouldSearch])

  const isLoading = shouldSearch && (studentsLoading || teachersLoading)

  const close = useCallback(() => {
    setIsOpen(false)
    setActiveIndex(-1)
  }, [])

  const onSelect = useCallback(
    (result: SearchResult) => {
      const path = result.type === 'student' ? `/students/${result.id}` : `/teachers/${result.id}`
      navigate(path)
      setTerm('')
      setIsOpen(false)
      setActiveIndex(-1)
    },
    [navigate]
  )

  const handleSetTerm = useCallback((value: string) => {
    setTerm(value)
    setIsOpen(value.length > 0)
    setActiveIndex(-1)
  }, [])

  return {
    term,
    setTerm: handleSetTerm,
    debouncedTerm,
    isOpen,
    close,
    results,
    isLoading,
    activeIndex,
    setActiveIndex,
    onSelect,
  }
}
