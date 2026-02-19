#!/usr/bin/env node

/**
 * Translation Consistency Checker
 *
 * Validates that all translation keys exist in both nl.json and en.json.
 * This ensures no missing translations when switching languages.
 *
 * Usage:
 *   node scripts/check-i18n-consistency.cjs
 *   npm run check:i18n
 *
 * Exit codes:
 *   0 - All translations are consistent
 *   1 - Missing translations found
 */

const fs = require('node:fs');
const path = require('node:path');

// Paths to translation files
const nlPath = path.join(__dirname, '../src/i18n/locales/nl.json');
const enPath = path.join(__dirname, '../src/i18n/locales/en.json');

/**
 * Recursively extracts all keys from a nested object
 * @param {object} obj - The object to extract keys from
 * @param {string} prefix - The current key prefix
 * @returns {string[]} Array of dot-notation keys
 */
function getAllKeys(obj, prefix = '') {
  const keys = [];
  for (const key in obj) {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    if (typeof obj[key] === 'object' && obj[key] !== null && !Array.isArray(obj[key])) {
      keys.push(...getAllKeys(obj[key], fullKey));
    } else {
      keys.push(fullKey);
    }
  }
  return keys;
}

/**
 * Main execution
 */
try {
  // Read translation files
  const nl = JSON.parse(fs.readFileSync(nlPath, 'utf-8'));
  const en = JSON.parse(fs.readFileSync(enPath, 'utf-8'));

  // Extract all keys
  const nlKeys = new Set(getAllKeys(nl));
  const enKeys = new Set(getAllKeys(en));

  // Find missing keys
  const missingInEn = [...nlKeys].filter(k => !enKeys.has(k));
  const missingInNl = [...enKeys].filter(k => !nlKeys.has(k));

  let hasErrors = false;

  // Report missing keys in English
  if (missingInEn.length > 0) {
    hasErrors = true;
    console.error('\n❌ Keys missing in en.json:\n');
    missingInEn.forEach(k => console.error(`  - ${k}`));
    console.error('');
  }

  // Report missing keys in Dutch
  if (missingInNl.length > 0) {
    hasErrors = true;
    console.error('\n❌ Keys missing in nl.json:\n');
    missingInNl.forEach(k => console.error(`  - ${k}`));
    console.error('');
  }

  // Exit with appropriate code
  if (hasErrors) {
    console.error('Fix missing translations by adding them to the appropriate locale file.\n');
    process.exit(1);
  } else {
    console.log('✅ All translation keys are consistent between nl.json and en.json!\n');
    console.log(`   Total keys: ${nlKeys.size}`);
    process.exit(0);
  }

} catch (error) {
  console.error('\n❌ Error checking translations:\n');
  console.error(error.message);
  process.exit(1);
}
