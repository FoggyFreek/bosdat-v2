import { useState, useRef, useCallback } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ArrowLeft, Trash2, Paperclip, X, FileText, Image } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { LexicalEditor } from '@/components/LexicalEditor'
import { lessonNotesApi } from '../api'
import type { LessonNote, NoteAttachment } from '../types'

const ACCEPTED_TYPES = 'image/*,application/pdf'
const MAX_FILE_SIZE = 10 * 1024 * 1024 // 10 MB

function AttachmentItem({
  attachment,
  onDelete,
  isDeleting,
}: {
  attachment: NoteAttachment
  onDelete: () => void
  isDeleting: boolean
}) {
  const isImage = attachment.contentType.startsWith('image/')
  return (
    <div className="flex items-center gap-2 rounded border px-2 py-1.5 text-sm bg-muted/30">
      {isImage ? (
        <Image className="h-4 w-4 text-muted-foreground shrink-0" />
      ) : (
        <FileText className="h-4 w-4 text-muted-foreground shrink-0" />
      )}
      <a
        href={attachment.url}
        target="_blank"
        rel="noopener noreferrer"
        className="flex-1 truncate hover:underline text-xs"
      >
        {attachment.fileName}
      </a>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="h-5 w-5 shrink-0"
        onClick={onDelete}
        disabled={isDeleting}
        aria-label="Delete attachment"
      >
        <X className="h-3 w-3" />
      </Button>
    </div>
  )
}

interface NoteEditorViewProps {
  note: LessonNote | null // null = new note
  lessonId: string
  lessonDate?: string
  queryKey: string[]
  onBack: () => void
}

export function NoteEditorView({
  note,
  lessonId,
  lessonDate,
  queryKey,
  onBack,
}: NoteEditorViewProps) {
  const { t, i18n } = useTranslation()
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [content, setContent] = useState(note?.content ?? '')
  const [isDragging, setIsDragging] = useState(false)

  const formattedDate = lessonDate
    ? new Intl.DateTimeFormat(i18n.language, {
        day: 'numeric',
        month: 'long',
        year: 'numeric',
      }).format(new Date(lessonDate))
    : ''

  const invalidate = useCallback(() => {
    queryClient.invalidateQueries({ queryKey })
  }, [queryClient, queryKey])

  const saveMutation = useMutation({
    mutationFn: () => {
      if (note) {
        return lessonNotesApi.update(lessonId, note.id, content)
      }
      return lessonNotesApi.create(lessonId, content)
    },
    onSuccess: () => {
      invalidate()
      if (!note) onBack()
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => lessonNotesApi.delete(lessonId, note!.id),
    onSuccess: () => {
      invalidate()
      onBack()
    },
  })

  const uploadMutation = useMutation({
    mutationFn: (file: File) => lessonNotesApi.addAttachment(lessonId, note!.id, file),
    onSuccess: invalidate,
  })

  const deleteAttachmentMutation = useMutation({
    mutationFn: (attachmentId: string) =>
      lessonNotesApi.deleteAttachment(lessonId, note!.id, attachmentId),
    onSuccess: invalidate,
  })

  const handleFiles = (files: FileList | null) => {
    if (!files || !note) return
    for (const file of Array.from(files)) {
      if (file.size > MAX_FILE_SIZE) continue
      uploadMutation.mutate(file)
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    handleFiles(e.dataTransfer.files)
  }

  const attachments = note?.attachments ?? []

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Button type="button" variant="ghost" size="icon" onClick={onBack} aria-label={t('common.actions.back')}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          {formattedDate && (
            <span className="text-sm font-medium">
              {t('lessons.notes.lessonDate', { date: formattedDate })}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {note && (
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="text-destructive hover:text-destructive"
              onClick={() => deleteMutation.mutate()}
              disabled={deleteMutation.isPending}
              aria-label={t('lessons.notes.deleteNote')}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          )}
          <Button
            type="button"
            size="sm"
            onClick={() => saveMutation.mutate()}
            disabled={saveMutation.isPending}
          >
            {t('common.actions.save')}
          </Button>
        </div>
      </div>

      <div className="border rounded-lg overflow-hidden">
        <LexicalEditor
          value={note?.content ?? ''}
          onChange={setContent}
          placeholder={t('lessons.notes.savePlaceholder')}
          minHeight="200px"
        />
      </div>

      {note && (
        <div className="space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
              {t('lessons.notes.attachments')}
            </span>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="h-7 text-xs gap-1"
              onClick={() => fileInputRef.current?.click()}
              disabled={uploadMutation.isPending}
            >
              <Paperclip className="h-3 w-3" />
              {t('lessons.notes.addAttachment')}
            </Button>
          </div>

          {attachments.length > 0 && (
            <div className="space-y-1">
              {attachments.map(a => (
                <AttachmentItem
                  key={a.id}
                  attachment={a}
                  onDelete={() => deleteAttachmentMutation.mutate(a.id)}
                  isDeleting={deleteAttachmentMutation.isPending}
                />
              ))}
            </div>
          )}

          <div
            role="button"
            tabIndex={0}
            aria-label={t('lessons.notes.uploadHint')}
            onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') fileInputRef.current?.click() }}
            onDragOver={e => { e.preventDefault(); setIsDragging(true) }}
            onDragLeave={() => setIsDragging(false)}
            onDrop={handleDrop}
            onClick={() => fileInputRef.current?.click()}
            className={`border-2 border-dashed rounded p-4 text-center text-xs text-muted-foreground cursor-pointer transition-colors ${
              isDragging ? 'border-primary bg-primary/5' : 'hover:border-muted-foreground/50'
            }`}
          >
            {t('lessons.notes.uploadHint')}
          </div>

          <input
            ref={fileInputRef}
            type="file"
            accept={ACCEPTED_TYPES}
            multiple
            className="hidden"
            onChange={e => handleFiles(e.target.files)}
          />
        </div>
      )}
    </div>
  )
}
