import { useRef, useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Search, Users, GraduationCap, Loader2 } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { useGlobalSearch } from '@/hooks/useGlobalSearch'
import type { SearchResult } from '@/hooks/useGlobalSearch'
import { cn } from '@/lib/utils'

export function GlobalSearch() {
  const { t } = useTranslation()
  const containerRef = useRef<HTMLDivElement>(null)
  const {
    term,
    setTerm,
    debouncedTerm,
    isOpen,
    close,
    results,
    isLoading,
    activeIndex,
    setActiveIndex,
    onSelect,
  } = useGlobalSearch()

  const shouldSearch = debouncedTerm.length >= 2

  const studentResults = useMemo(
    () => results.filter((r) => r.type === 'student'),
    [results]
  )
  const teacherResults = useMemo(
    () => results.filter((r) => r.type === 'teacher'),
    [results]
  )

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        close()
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [close])

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      close()
      return
    }

    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setActiveIndex(Math.min(activeIndex + 1, results.length - 1))
      return
    }

    if (e.key === 'ArrowUp') {
      e.preventDefault()
      setActiveIndex(Math.max(activeIndex - 1, -1))
      return
    }

    if (e.key === 'Enter' && activeIndex >= 0 && activeIndex < results.length) {
      e.preventDefault()
      onSelect(results[activeIndex])
    }
  }

  const showDropdown = isOpen && term.length > 0

  return (
    <div ref={containerRef} className="flex-1 max-w-md mx-auto relative">
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder={t('search.placeholder')}
          className="pl-9"
          value={term}
          onChange={(e) => setTerm(e.target.value)}
          onKeyDown={handleKeyDown}
          role="combobox"
          aria-expanded={showDropdown}
          aria-controls="global-search-results"
          aria-activedescendant={activeIndex >= 0 ? `search-result-${activeIndex}` : undefined}
        />
      </div>

      {showDropdown && (
        <ul
          id="global-search-results"
          className="absolute top-full left-0 right-0 mt-1 bg-white border rounded-md shadow-lg z-50 max-h-[400px] overflow-y-auto list-none p-0 m-0"
        >
          {term.length < 2 && (
            <li className="px-4 py-3 text-sm text-muted-foreground">
              {t('search.minCharacters')}
            </li>
          )}

          {isLoading && term.length >= 2 && (
            <li className="flex items-center gap-2 px-4 py-3 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              {t('common.states.loading')}
            </li>
          )}

          {!isLoading && shouldSearch && results.length === 0 && (
            <li className="px-4 py-3 text-sm text-muted-foreground">
              {t('search.noResults')}
            </li>
          )}

          {!isLoading && results.length > 0 && (
            <>
              {studentResults.length > 0 && (
                <ResultGroup
                  label={t('common.entities.students')}
                  icon={<Users className="h-4 w-4" />}
                  results={studentResults}
                  startIndex={0}
                  activeIndex={activeIndex}
                  onSelect={onSelect}
                  setActiveIndex={setActiveIndex}
                />
              )}
              {teacherResults.length > 0 && (
                <ResultGroup
                  label={t('common.entities.teachers')}
                  icon={<GraduationCap className="h-4 w-4" />}
                  results={teacherResults}
                  startIndex={studentResults.length}
                  activeIndex={activeIndex}
                  onSelect={onSelect}
                  setActiveIndex={setActiveIndex}
                />
              )}
            </>
          )}
        </ul>
      )}
    </div>
  )
}

interface ResultGroupProps {
  label: string
  icon: React.ReactNode
  results: SearchResult[]
  startIndex: number
  activeIndex: number
  onSelect: (result: SearchResult) => void
  setActiveIndex: (index: number) => void
}

function ResultGroup({
  label,
  icon,
  results,
  startIndex,
  activeIndex,
  onSelect,
  setActiveIndex,
}: Readonly<ResultGroupProps>) {
  return (
    <li>
      <div className="flex items-center gap-2 px-4 py-2 text-xs font-semibold text-muted-foreground uppercase tracking-wider border-b bg-gray-50">
        {icon}
        {label}
      </div>
      <ul className="list-none p-0 m-0">
        {results.map((result, i) => {
          const globalIndex = startIndex + i
          return (
            <li key={result.id}>
              <button
                id={`search-result-${globalIndex}`}
                aria-selected={activeIndex === globalIndex}
                className={cn(
                  'w-full text-left px-4 py-2 flex flex-col hover:bg-accent transition-colors',
                  activeIndex === globalIndex && 'bg-accent'
                )}
                onClick={() => onSelect(result)}
                onMouseEnter={() => setActiveIndex(globalIndex)}
                type="button"
              >
                <span className="text-sm font-medium">{result.name}</span>
                <span className="text-xs text-muted-foreground">{result.subtitle}</span>
              </button>
            </li>
          )
        })}
      </ul>
    </li>
  )
}
