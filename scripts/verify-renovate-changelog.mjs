#!/usr/bin/env node
import { execFileSync, spawnSync } from 'node:child_process';
import { createRequire } from 'node:module';
import { mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const packageDir = 'pack/Alexaka1.Analyzers.StructuredLogging';
const expectedSourceUrl = 'https://github.com/alexaka1/structured-logging-analyzers';
const expectedSourceDirectory = packageDir;
const expectedChangelogFile = `${packageDir}/CHANGELOG.md`;
const expectedVersion = '0.1.0-preview.1';
const expectedRepositoryUrl = `${expectedSourceUrl}/tree/main/${expectedSourceDirectory}`;

function resolveFromRenovate(specifier) {
  const renovateRoot = process.env.RENOVATE_ROOT;
  if (!renovateRoot) {
    throw new Error('RENOVATE_ROOT must point at the installed renovate package');
  }
  const req = createRequire(path.join(renovateRoot, 'package.json'));
  if (specifier.startsWith('renovate/')) {
    return req.resolve(`./${specifier.slice('renovate/'.length)}`);
  }
  return req.resolve(specifier);
}

async function importFromRenovate(specifier) {
  return import(pathToFileURL(resolveFromRenovate(specifier)).href);
}

function packAndReadNuspec() {
  const outDir = mkdtempSync(path.join(tmpdir(), 'sla-nupkg-'));
  try {
    execFileSync(
      'dotnet',
      [
        'pack',
        path.join(repoRoot, packageDir, 'Package.csproj'),
        '-c',
        'Release',
        '-o',
        outDir,
      ],
      { stdio: 'inherit' },
    );
    const nupkg = path.join(
      outDir,
      `Alexaka1.Analyzers.StructuredLogging.${expectedVersion}.nupkg`,
    );
    const zipList = spawnSync('unzip', ['-p', nupkg, 'Alexaka1.Analyzers.StructuredLogging.nuspec'], {
      encoding: 'utf8',
    });
    if (zipList.status !== 0) {
      throw new Error(zipList.stderr || 'failed to read nuspec from nupkg');
    }
    const nuspec = zipList.stdout;
    const repositoryUrl = nuspec.match(/<repository\b[^>]*\burl="([^"]+)"/i)?.[1];
    if (!repositoryUrl) {
      throw new Error('Packed nuspec is missing repository@url');
    }
    return { nupkg, nuspec, repositoryUrl };
  } finally {
    rmSync(outDir, { recursive: true, force: true });
  }
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const packed = packAndReadNuspec();
console.log(`packed repository@url: ${packed.repositoryUrl}`);
assert(
  packed.repositoryUrl === expectedRepositoryUrl,
  `Expected repository@url ${expectedRepositoryUrl}, got ${packed.repositoryUrl}`,
);

const { addMetaData } = await importFromRenovate('renovate/dist/modules/datasource/metadata.js');
const dep = {
  releases: [{ version: expectedVersion }],
  sourceUrl: packed.repositoryUrl,
};
addMetaData(dep, 'nuget', 'Alexaka1.Analyzers.StructuredLogging');
console.log(`addMetaData sourceUrl: ${dep.sourceUrl}`);
console.log(`addMetaData sourceDirectory: ${dep.sourceDirectory}`);
assert(dep.sourceUrl === expectedSourceUrl, `sourceUrl mismatch: ${dep.sourceUrl}`);
assert(
  dep.sourceDirectory === expectedSourceDirectory,
  `sourceDirectory mismatch: ${dep.sourceDirectory}`,
);

const { compareChangelogFilePath } = await importFromRenovate(
  'renovate/dist/workers/repository/update/pr/changelog/common.js',
);
const changelogFilenameRegex = (await importFromRenovate('changelog-filename-regex')).default;

function selectChangelogFile(treePaths, sourceDirectory) {
  const allFiles = treePaths.filter((entry) => !entry.endsWith('/'));
  let files = [];
  if (sourceDirectory) {
    const prefix = sourceDirectory.endsWith('/') ? sourceDirectory : `${sourceDirectory}/`;
    files = allFiles
      .filter((entry) => entry.startsWith(sourceDirectory))
      .filter((entry) => changelogFilenameRegex.test(entry.replace(prefix, '')));
  } else {
    // Renovate only lists the repository root when sourceDirectory is unset.
    files = allFiles
      .filter((entry) => !entry.includes('/'))
      .filter((entry) => changelogFilenameRegex.test(entry));
  }
  files.sort(compareChangelogFilePath);
  return files[0] ?? null;
}

const localTree = execFileSync('git', ['ls-tree', '-r', '--name-only', 'HEAD'], {
  cwd: repoRoot,
  encoding: 'utf8',
})
  .trim()
  .split('\n');

const withoutDirectory = selectChangelogFile(localTree, '');
const withDirectory = selectChangelogFile(localTree, expectedSourceDirectory);
console.log(`changelog without sourceDirectory: ${withoutDirectory ?? '(none)'}`);
console.log(`changelog with sourceDirectory: ${withDirectory ?? '(none)'}`);
assert(
  withDirectory === expectedChangelogFile,
  `Expected ${expectedChangelogFile}, got ${withDirectory}`,
);
assert(
  withoutDirectory == null,
  `Root-only lookup should miss the nested changelog, got ${withoutDirectory}`,
);

const { init: initLogger } = await importFromRenovate('renovate/dist/logger/index.js');
initLogger();

const { init: initMemCache, set: setMemCache } = await importFromRenovate(
  'renovate/dist/util/cache/memory/index.js',
);
const releaseNotes = await importFromRenovate(
  'renovate/dist/workers/repository/update/pr/changelog/release-notes.js',
);
const { GitHubChangeLogSource } = await importFromRenovate(
  'renovate/dist/workers/repository/update/pr/changelog/github/source.js',
);

initMemCache();
const changelogMd = `${readFileSync(path.join(repoRoot, expectedChangelogFile), 'utf8')}\n#\n##`;
const apiBaseUrl = 'https://api.github.com/';
const repository = 'alexaka1/structured-logging-analyzers';
setMemCache(
  `getReleaseNotesMdFile@v2-${repository}-${expectedSourceDirectory}-${apiBaseUrl}`,
  Promise.resolve({ changelogFile: expectedChangelogFile, changelogMd }),
);

const project = {
  apiBaseUrl,
  baseUrl: 'https://github.com/',
  type: 'github',
  repository,
  sourceUrl: expectedSourceUrl,
  sourceDirectory: expectedSourceDirectory,
  packageName: 'Alexaka1.Analyzers.StructuredLogging',
  depName: 'Alexaka1.Analyzers.StructuredLogging',
};
const source = new GitHubChangeLogSource();
const notes = await releaseNotes.getReleaseNotesMd(
  project,
  { version: expectedVersion, changes: [], compare: {} },
  source,
);

console.log('parsed release notes:');
console.log(JSON.stringify(notes, null, 2));
assert(notes, 'Renovate did not parse release notes from CHANGELOG.md');
assert(
  notes.notesSourceUrl?.includes(expectedChangelogFile),
  `notesSourceUrl did not point at ${expectedChangelogFile}: ${notes.notesSourceUrl}`,
);
assert(
  typeof notes.body === 'string' && notes.body.includes('Initial preview'),
  `Release notes body did not contain the changelog section: ${notes.body}`,
);

console.log('Renovate changelog verification passed.');
