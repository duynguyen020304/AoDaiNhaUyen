import { execFile, spawn } from 'node:child_process';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { promisify } from 'node:util';
import path from 'node:path';

const API_BASE_URL = (process.env.VITE_API_BASE_URL || process.env.PUBLIC_BACKEND_DOMAIN || 'http://localhost:5043').replace(/\/$/, '');
const PREVIEW_PORT = Number(process.env.PRERENDER_PORT || 4173);
const SITE = `http://127.0.0.1:${PREVIEW_PORT}`;

const execFileAsync = promisify(execFile);

async function stopServer(server) {
  if (!server.pid) return;
  if (process.platform === 'win32') {
    try { await execFileAsync('taskkill', ['/PID', String(server.pid), '/T', '/F']); } catch {}
    return;
  }
  server.kill('SIGTERM');
}

async function waitFor(url, timeoutMs = 30000) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    try {
      const response = await fetch(url);
      if (response.ok) return;
    } catch {}
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
  throw new Error(`Timed out waiting for ${url}`);
}

async function getPublishedSlugs() {
  try {
    const response = await fetch(`${API_BASE_URL}/api/v1/blog?pageSize=50`);
    if (!response.ok) return [];
    const payload = await response.json();
    return (payload.data ?? []).map((post) => post.slug).filter(Boolean);
  } catch {
    return [];
  }
}

async function snapshot(page, route, outputFile) {
  await page.goto(`${SITE}${route}`, { waitUntil: 'networkidle0', timeout: 30000 });
  const html = await page.content();
  await mkdir(path.dirname(outputFile), { recursive: true });
  await writeFile(outputFile, html, 'utf8');
  console.log(`pre-rendered ${route} -> ${outputFile}`);
}

async function main() {
  const { default: puppeteer } = await import('puppeteer');
  await readFile('dist/index.html', 'utf8');
  const server = spawn('bun', ['x', 'vite', 'preview', '--host', '127.0.0.1', '--port', String(PREVIEW_PORT), '--strictPort'], { stdio: 'inherit' });
  try {
    await waitFor(SITE);
    const browser = await puppeteer.launch({ headless: 'new' });
    try {
      const page = await browser.newPage();
      await snapshot(page, '/blog/', 'dist/blog/index.html');
      for (const slug of await getPublishedSlugs()) {
        await snapshot(page, `/blog/${slug}/`, `dist/blog/${slug}/index.html`);
      }
    } finally {
      await browser.close();
    }
  } finally {
    await stopServer(server);
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
