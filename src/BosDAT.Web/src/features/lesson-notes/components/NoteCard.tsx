import { useTranslation } from 'react-i18next'
import { Paperclip } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { LessonNote } from '../types'

function extractTextPreview(content: string, maxLength = 80): string {
  if (!content) return ''
  try {
    const parsed = JSON.parse(content)
    const texts: string[] = []

    const extractFromNode = (node: { text?: string; children?: unknown[] }) => {
      if (node.text) texts.push(node.text)
      if (node.children) {
        for (const child of node.children) {
          extractFromNode(child as { text?: string; children?: unknown[] })
        }
      }
    }

    if (parsed.root) extractFromNode(parsed.root)
    const fullText = texts.join(' ')
    return fullText.length > maxLength ? fullText.slice(0, maxLength) + '…' : fullText
  } catch {
    return ''
  }
}

interface NoteCardProps {
  note: LessonNote
  isSelected: boolean
  onClick: () => void
}

export function NoteCard({ note, isSelected, onClick }: NoteCardProps) {
  const { t, i18n } = useTranslation()

  const formattedDate = new Intl.DateTimeFormat(i18n.language, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  }).format(new Date(note.lessonDate))

  const preview = extractTextPreview(note.content)

  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'w-full text-left rounded-lg border p-3 space-y-1.5 transition-colors hover:border-amber-400 hover:bg-amber-50/50',
        isSelected && 'border-amber-500 bg-amber-50'
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <span className="text-xs font-medium text-muted-foreground">
          {t('lessons.notes.lessonDate', { date: formattedDate })}
        </span>
        {note.attachments.length > 0 && (
          <span className="flex items-center gap-0.5 text-xs text-muted-foreground shrink-0">
            <Paperclip className="h-3 w-3" />
            {note.attachments.length}
          </span>
        )}
      </div>
      <p className="text-sm line-clamp-3">
        {preview || (
          <span className="text-muted-foreground italic">{t('lessons.notes.noContent')}</span>
        )}
      </p>
    </button>
  )
}
