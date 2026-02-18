import { defineConfig, mergeConfig } from 'vitest/config'
import viteConfig from './vite.config'

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      css: true,
      coverage: {
        provider: 'v8',
        reporter: ['text', 'lcov'],
        reportsDirectory: './coverage',
        exclude: [
          // Lexical editor wraps a complex DOM-based rich text library
          // that cannot run in jsdom — mocked in all consuming component tests
          'src/components/LexicalEditor.tsx',
        ],
      },
    },
  })
)
