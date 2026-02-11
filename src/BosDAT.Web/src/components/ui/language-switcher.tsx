import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const NLFlag = () => (
  <svg viewBox="0 0 9 6" className="h-5 w-5 rounded-sm">
    <rect fill="#21468B" y="4" width="9" height="2" />
    <rect fill="#FFF" y="2" width="9" height="2" />
    <rect fill="#AE1C28" width="9" height="2" />
  </svg>
)

const ENFlag = () => (
  <svg viewBox="0 0 60 30" className="h-5 w-5 rounded-sm">
    <rect fill="#012169" width="60" height="30" />
    <path d="M0,0 L60,30 M60,0 L0,30" stroke="#FFF" strokeWidth="6" />
    <path d="M0,0 L60,30 M60,0 L0,30" stroke="#C8102E" strokeWidth="4" />
    <path d="M30,0 L30,30 M0,15 L60,15" stroke="#FFF" strokeWidth="10" />
    <path d="M30,0 L30,30 M0,15 L60,15" stroke="#C8102E" strokeWidth="6" />
  </svg>
)

const languages = [
  { code: 'nl', flag: NLFlag, name: 'Nederlands' },
  { code: 'en', flag: ENFlag, name: 'English' },
]

export function LanguageSwitcher() {
  const { i18n } = useTranslation()

  return (
    <div className="flex items-center gap-1">
      {languages.map((lang) => {
        const isActive = i18n.language.startsWith(lang.code)
        const FlagComponent = lang.flag
        return (
          <Button
            key={lang.code}
            variant="ghost"
            size="icon"
            title={lang.name}
            onClick={() => i18n.changeLanguage(lang.code)}
            className={cn(
              'h-8 w-8 rounded-full transition-all p-1.5',
              isActive
                ? 'bg-primary/10 ring-1 ring-gray-400'
                : 'hover:bg-gray-100 opacity-60 hover:opacity-100'
            )}
          >
            <FlagComponent />
          </Button>
        )
      })}
    </div>
  )
}
