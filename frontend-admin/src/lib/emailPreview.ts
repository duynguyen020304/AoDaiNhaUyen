export function buildEmailPreviewDocument(subject: string, preheader: string | null | undefined, htmlBody: string) {
  return `<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>${escapeHtml(subject)}</title>
  <style>body{margin:0;background:#f4f4f5;font-family:Arial,sans-serif}.frame{max-width:720px;margin:32px auto;background:white;border-radius:12px;box-shadow:0 10px 30px rgba(0,0,0,.08);overflow:hidden}.head{padding:20px 24px;border-bottom:1px solid #eee}.subject{margin:0;font-size:18px;color:#721311}.pre{margin:6px 0 0;color:#667085;font-size:13px}.body{padding:24px}</style>
</head>
<body>
  <main class="frame">
    <header class="head"><h1 class="subject">${escapeHtml(subject)}</h1>${preheader ? `<p class="pre">${escapeHtml(preheader)}</p>` : ''}</header>
    <section class="body">${htmlBody}</section>
  </main>
</body>
</html>`
}

export function createEmailPreviewUrl(subject: string, preheader: string | null | undefined, htmlBody: string) {
  return URL.createObjectURL(new Blob([buildEmailPreviewDocument(subject, preheader, htmlBody)], { type: 'text/html' }))
}

export function openEmailPreviewInNewTab(subject: string, preheader: string | null | undefined, htmlBody: string) {
  const url = createEmailPreviewUrl(subject, preheader, htmlBody)
  const preview = window.open(url, '_blank', 'noopener,noreferrer')
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
  return Boolean(preview)
}

function escapeHtml(value: string) {
  return value.replace(/[&<>'"]/g, (char) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[char]!)
}
