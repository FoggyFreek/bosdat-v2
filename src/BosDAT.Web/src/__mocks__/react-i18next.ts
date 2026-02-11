import { vi } from 'vitest'

export const useTranslation = () => ({
  t: (key: string) => key,
  i18n: {
    language: 'nl',
    changeLanguage: vi.fn(),
  },
})

export const initReactI18next = {
  type: '3rdParty',
  init: vi.fn(),
}

export const Trans = ({ children }: { children: React.ReactNode }) => children
