export function buildEmailPreviewDocument(subject: string, preheader: string | null | undefined, renderedHtml: string) {
  if (/<!doctype|<html[\s>]/i.test(renderedHtml)) return renderedHtml
  return `<!doctype html>
<html lang="vi">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>${escapeHtml(subject)}</title>
</head>
<body>${renderedHtml}</body>
</html>`
}

export function createEmailPreviewUrl(subject: string, preheader: string | null | undefined, renderedHtml: string) {
  return URL.createObjectURL(new Blob([buildEmailPreviewDocument(subject, preheader, renderedHtml)], { type: 'text/html' }))
}

export function openEmailPreviewInNewTab(subject: string, preheader: string | null | undefined, renderedHtml: string) {
  const url = createEmailPreviewUrl(subject, preheader, renderedHtml)
  const preview = window.open(url, '_blank', 'noopener,noreferrer')
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
  return Boolean(preview)
}

function escapeHtml(value: string) {
  return value.replace(/[&<>'"]/g, (char) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[char]!)
}
