import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { courseTasksApi } from '../api'

interface CourseTasksSectionProps {
  courseId: string
}

export function CourseTasksSection({ courseId }: CourseTasksSectionProps) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [newTitle, setNewTitle] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  const { data: tasks, isLoading } = useQuery({
    queryKey: ['course-tasks', courseId],
    queryFn: () => courseTasksApi.getByCourse(courseId),
  })

  const createMutation = useMutation({
    mutationFn: (title: string) => courseTasksApi.create(courseId, { title }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['course-tasks', courseId] })
      setNewTitle('')
      inputRef.current?.focus()
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (taskId: string) => courseTasksApi.delete(courseId, taskId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['course-tasks', courseId] })
    },
  })

  const handleAdd = () => {
    const title = newTitle.trim()
    if (!title) return
    createMutation.mutate(title)
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') handleAdd()
  }

  const list = tasks ?? []

  return (
    <section className="space-y-3">
      <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
        {t('lessons.tasks.title')}
      </h3>

      {isLoading && (
        <div className="space-y-2">
          {[1, 2].map(i => (
            <div key={i} className="h-8 bg-muted animate-pulse rounded" />
          ))}
        </div>
      )}

      {!isLoading && list.length === 0 && (
        <p className="text-sm text-muted-foreground">{t('lessons.tasks.empty')}</p>
      )}

      {!isLoading && list.length > 0 && (
        <ul className="space-y-1">
          {list.map(task => (
            <li
              key={task.id}
              className="flex items-center gap-2 group rounded px-2 py-1 hover:bg-muted/50"
            >
              <button
                type="button"
                aria-label={t('lessons.tasks.markDone')}
                onClick={() => deleteMutation.mutate(task.id)}
                disabled={deleteMutation.isPending}
                className="h-4 w-4 rounded-full border-2 border-muted-foreground hover:border-primary hover:bg-primary/20 transition-colors flex-shrink-0"
              />
              <span className="flex-1 text-sm">{task.title}</span>
              <Button
                variant="ghost"
                size="icon"
                className="h-6 w-6 opacity-0 group-hover:opacity-100 transition-opacity"
                onClick={() => deleteMutation.mutate(task.id)}
                disabled={deleteMutation.isPending}
                aria-label={t('common.actions.delete')}
              >
                <Trash2 className="h-3 w-3" />
              </Button>
            </li>
          ))}
        </ul>
      )}

      <div className="flex gap-2">
        <Input
          ref={inputRef}
          value={newTitle}
          onChange={e => setNewTitle(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={t('lessons.tasks.placeholder')}
          className="h-8 text-sm"
          disabled={createMutation.isPending}
        />
        <Button
          size="sm"
          variant="outline"
          onClick={handleAdd}
          disabled={!newTitle.trim() || createMutation.isPending}
          aria-label={t('common.actions.add')}
        >
          <Plus className="h-4 w-4" />
        </Button>
      </div>
    </section>
  )
}
