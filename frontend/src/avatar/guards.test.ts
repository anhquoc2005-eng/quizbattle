import { readdirSync, readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { describe, expect, it } from 'vitest'

const SRC = join(process.cwd(), 'src')
const THIS_FILE = join(SRC, 'avatar', 'guards.test.ts')

function sourceFiles(dir: string): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((e) => {
    const full = join(dir, e.name)
    if (e.isDirectory()) return sourceFiles(full)
    return /\.(ts|tsx)$/.test(e.name) ? [full] : []
  })
}

const ALL = sourceFiles(SRC).filter((f) => f !== THIS_FILE)
const NON_TEST = ALL.filter((f) => !/\.test\.tsx?$/.test(f))
const rel = (f: string) => relative(SRC, f).replace(/\\/g, '/')

describe('DiceBear guards', () => {
  it('package.json_DicebearPackages_PinnedExactly', () => {
    const pkg = JSON.parse(readFileSync(join(process.cwd(), 'package.json'), 'utf8')) as { dependencies: Record<string, string> }

    expect(pkg.dependencies['@dicebear/core']).toBe('10.6.0')
    expect(pkg.dependencies['@dicebear/styles']).toBe('10.6.0')
    for (const [name, version] of Object.entries(pkg.dependencies)) {
      if (name.startsWith('@dicebear/')) expect(version, name).not.toMatch(/^[\^~]/)
    }
  })

  it('src_DoesNotReference_api_dicebear_com', () => {
    const offenders = ALL.filter((f) => readFileSync(f, 'utf8').includes('api.dicebear.com')).map(rel)

    expect(offenders).toEqual([])
  })

  it('src_OnlyImportsTheNotionistsStyleFile', () => {
    const offenders = ALL.filter((f) => {
      const matches = readFileSync(f, 'utf8').match(/@dicebear\/styles[^'"\s]*/g) ?? []
      return matches.some((m) => m !== '@dicebear/styles/notionists.json')
    }).map(rel)

    expect(offenders).toEqual([])
  })

  it('src_NonTestFiles_NeverStaticallyImportDicebear_onlyDynamicImport', () => {
    // import() động được phép; `import type` bị xóa lúc build nên cũng được phép. Import tĩnh (kể cả `import x from`, `import {..} from`,
    // `import 'x'`, `export ... from`) sẽ kéo core + 372 KB style vào bundle chính.
    const staticImport = /^\s*(?:import(?!\s+type\b)[^('"]*?from\s*|import\s*|export[^('"]*?from\s*)['"]@dicebear\//m
    const offenders = NON_TEST.filter((f) => staticImport.test(readFileSync(f, 'utf8'))).map(rel)

    expect(offenders).toEqual([])
  })

  it('src_OnlyAvatarRendererLoadsDicebearAtRuntime', () => {
    const loaders = NON_TEST.filter((f) => /import\(\s*['"]@dicebear\//.test(readFileSync(f, 'utf8'))).map(rel)

    expect(loaders).toEqual(['avatar/avatarRenderer.ts'])
  })
})
