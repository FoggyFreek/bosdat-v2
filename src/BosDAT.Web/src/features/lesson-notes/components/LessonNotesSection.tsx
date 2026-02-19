import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Grid2X2, List, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { NoteCard } from './NoteCard'
import { NoteEditorView } from './NoteEditorView'
import { lessonNotesApi } from '../api'
import type { LessonNote } from '../types'

type ViewMode = 'grid' | 'list'

interface LessonNotesSectionProps {
  lessonId: string
  lessonDate?: string
}

export function LessonNotesSection({ lessonId, lessonDate }: Readonly<LessonNotesSectionProps>) {
  const { t } = useTranslation()
  const queryKey = ['lesson-notes', lessonId]

  const [viewMode, setViewMode] = useState<ViewMode>('grid')
  const [selectedNote, setSelectedNote] = useState<LessonNote | 'new' | null>(null)

  const { data: notes, isLoading } = useQuery({
    queryKey,
    queryFn: () => lessonNotesApi.getByCourse(lessonId),
  })

  const list = notes ?? []

  const handleNoteClick = (note: LessonNote) => {
    setSelectedNote(note)
  }

  const handleAddNote = () => {
    setSelectedNote('new')
  }

  const handleBack = () => {
    setSelectedNote(null)
  }

  // Editor view
  if (selectedNote !== null) {
    const note = selectedNote === 'new' ? null : selectedNote
    const date = note?.lessonDate ?? lessonDate
    return (
      <section className="space-y-3">
        <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
          {t('lessons.notes.title')}
        </h3>
        <NoteEditorView
          note={note}
          lessonId={lessonId}
          lessonDate={date}
          queryKey={queryKey}
          onBack={handleBack}
        />
      </section>
    )
  }

  // Grid / list view
  return (
    <section className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
          {t('lessons.notes.title')}
        </h3>
        <div className="flex items-center gap-1">
          <Button
            type="button"
            variant={viewMode === 'grid' ? 'secondary' : 'ghost'}
            size="icon"
            className="h-7 w-7"
            onClick={() => setViewMode('grid')}
            aria-label={t('lessons.notes.viewGrid')}
          >
            <Grid2X2 className="h-3.5 w-3.5" />
          </Button>
          <Button
            type="button"
            variant={viewMode === 'list' ? 'secondary' : 'ghost'}
            size="icon"
            className="h-7 w-7"
            onClick={() => setViewMode('list')}
            aria-label={t('lessons.notes.viewList')}
          >
            <List className="h-3.5 w-3.5" />
          </Button>
        </div>
      </div>

      {isLoading && (
        <div
          className={
            viewMode === 'grid'
              ? 'grid grid-cols-2 gap-2'
              : 'space-y-2'
          }
        >
          {[1, 2].map(i => (
            <div key={i} className="h-24 bg-muted animate-pulse rounded-lg" />
          ))}
        </div>
      )}

      {!isLoading && (
        <div
          className={
            viewMode === 'grid'
              ? 'grid grid-cols-2 gap-2'
              : 'space-y-2'
          }
        >
          {list.map(note => (
            <NoteCard
              key={note.id}
              note={note}
              isSelected={false}
              onClick={() => handleNoteClick(note)}
            />
          ))}

          <button
            type="button"
            onClick={handleAddNote}
            className="flex items-center justify-center gap-2 rounded-lg border-2 border-dashed p-3 text-sm text-muted-foreground hover:border-muted-foreground/50 hover:text-foreground transition-colors min-h-[80px]"
          >
            <Plus className="h-4 w-4" />
            {t('lessons.notes.addNote')}
          </button>
        </div>
      )}

      {!isLoading && list.length === 0 && (
        <p className="text-sm text-muted-foreground">{t('lessons.notes.empty')}</p>
      )}
    </section>
  )
}
