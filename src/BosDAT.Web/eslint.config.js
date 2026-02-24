import js from "@eslint/js";
import tseslint from "typescript-eslint";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import globals from "globals";

export default tseslint.config(
  // ─── 1. Global ignores (replaces ignorePatterns) ─────────────────────────
  {
    ignores: ["dist/**", "src/test/**"],
  },

  // ─── 2. Base recommended rules ───────────────────────────────────────────
  js.configs.recommended,

  // ─── 3. TypeScript + React rules (scoped to TS/TSX) ──────────────────────
  {
    files: ["**/*.ts", "**/*.tsx"],
    extends: [
      ...tseslint.configs.recommended,
      reactHooks.configs.flat["recommended-latest"],
    ],
    languageOptions: {
      ecmaVersion: "latest",
      sourceType: "module",
      globals: {
        ...globals.browser,
        ...globals.es2020,
      },
      parserOptions: {
        ecmaFeatures: { jsx: true },
      },
    },
    plugins: {
      "react-refresh": reactRefresh,
    },
    rules: {
      // React Refresh
      "react-refresh/only-export-components": [
        "warn",
        {
          allowConstantExport: true,
          allowExportNames: ["useFormDirty", "useSettingsDirty", "useAuth"],
        },
      ],

      // TypeScript
      "@typescript-eslint/no-unused-vars": [
        "error",
        {
          argsIgnorePattern: "^_",
          varsIgnorePattern: "^_",
          caughtErrorsIgnorePattern: "^_",
        },
      ],
      "@typescript-eslint/no-explicit-any": "warn",

      // General
      "no-console": ["warn", { allow: ["warn", "error"] }],
    },
  },

  // ─── 4. Node.js scripts (CJS) ────────────────────────────────────────────
  {
    files: ["scripts/**/*.cjs"],
    languageOptions: {
      globals: {
        ...globals.node,
      },
    },
  },

  // ─── 5. shadcn/ui overrides ───────────────────────────────────────────────
  //    These components intentionally export variants alongside components,
  //    so react-refresh's rule would produce false positives here.
  {
    files: ["src/components/ui/**/*.tsx"],
    rules: {
      "react-refresh/only-export-components": "off",
    },
  },
);