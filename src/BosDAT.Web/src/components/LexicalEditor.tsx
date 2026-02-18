import { useEffect, useCallback } from 'react'
import { LexicalComposer } from '@lexical/react/LexicalComposer'
import { RichTextPlugin } from '@lexical/react/LexicalRichTextPlugin'
import { ContentEditable } from '@lexical/react/LexicalContentEditable'
import { HistoryPlugin } from '@lexical/react/LexicalHistoryPlugin'
import { OnChangePlugin } from '@lexical/react/LexicalOnChangePlugin'
import { LexicalErrorBoundary } from '@lexical/react/LexicalErrorBoundary'
import { useLexicalComposerContext } from '@lexical/react/LexicalComposerContext'
import { ListPlugin } from '@lexical/react/LexicalListPlugin'
import { EditorState, FORMAT_TEXT_COMMAND } from 'lexical'
import { HeadingNode, QuoteNode } from '@lexical/rich-text'
import { ListNode, ListItemNode } from '@lexical/list'
import { LinkNode } from '@lexical/link'
import { Bold, Italic, Underline, Strikethrough } from 'lucide-react'
import { Button } from '@/components/ui/button'

const theme = {
  text: {
    bold: 'font-bold',
    italic: 'italic',
    underline: 'underline',
    strikethrough: 'line-through',
  },
  list: {
    ul: 'list-disc list-inside',
    ol: 'list-decimal list-inside',
  },
}

function ToolbarPlugin() {
  const [editor] = useLexicalComposerContext()

  const formatText = useCallback(
    (format: 'bold' | 'italic' | 'underline' | 'strikethrough') => {
      editor.dispatchCommand(FORMAT_TEXT_COMMAND, format)
    },
    [editor]
  )

  return (
    <div className="flex gap-1 border-b p-1">
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="h-7 w-7"
        onClick={() => formatText('bold')}
        aria-label="Bold"
      >
        <Bold className="h-3.5 w-3.5" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="h-7 w-7"
        onClick={() => formatText('italic')}
        aria-label="Italic"
      >
        <Italic className="h-3.5 w-3.5" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="h-7 w-7"
        onClick={() => formatText('underline')}
        aria-label="Underline"
      >
        <Underline className="h-3.5 w-3.5" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="h-7 w-7"
        onClick={() => formatText('strikethrough')}
        aria-label="Strikethrough"
      >
        <Strikethrough className="h-3.5 w-3.5" />
      </Button>
    </div>
  )
}

interface LoadInitialStatePlugin {
  value: string
}

function LoadInitialStatePlugin({ value }: LoadInitialStatePlugin) {
  const [editor] = useLexicalComposerContext()

  useEffect(() => {
    if (!value) return
    try {
      const parsed = JSON.parse(value)
      const editorState = editor.parseEditorState(parsed)
      editor.setEditorState(editorState)
    } catch {
      // Invalid JSON — leave as-is
    }
    // Only run on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return null
}

interface LexicalEditorProps {
  value?: string
  onChange?: (json: string) => void
  placeholder?: string
  readOnly?: boolean
  minHeight?: string
}

export function LexicalEditor({
  value = '',
  onChange,
  placeholder = '',
  readOnly = false,
  minHeight = '120px',
}: LexicalEditorProps) {
  const initialConfig = {
    namespace: 'BosDAT-Note',
    theme,
    nodes: [HeadingNode, QuoteNode, ListNode, ListItemNode, LinkNode],
    editable: !readOnly,
    onError: (error: Error) => {
      console.error('Lexical error:', error)
    },
  }

  const handleChange = useCallback(
    (state: EditorState) => {
      if (!onChange) return
      onChange(JSON.stringify(state.toJSON()))
    },
    [onChange]
  )

  return (
    <LexicalComposer initialConfig={initialConfig}>
      {!readOnly && <ToolbarPlugin />}
      <div className="relative" style={{ minHeight }}>
        <RichTextPlugin
          contentEditable={
            <ContentEditable
              className="outline-none px-3 py-2 text-sm min-h-[inherit] focus:ring-0"
              style={{ minHeight }}
              aria-placeholder={placeholder}
              placeholder={
                <div className="absolute top-2 left-3 text-muted-foreground text-sm pointer-events-none select-none">
                  {placeholder}
                </div>
              }
            />
          }
          ErrorBoundary={LexicalErrorBoundary}
        />
        <HistoryPlugin />
        <ListPlugin />
        {value && <LoadInitialStatePlugin value={value} />}
        {onChange && <OnChangePlugin onChange={handleChange} />}
      </div>
    </LexicalComposer>
  )
}
