import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { ModalOverlay } from './ModalOverlay'
import { useEmailMarketingStore } from '@/stores/emailMarketingStore'
import type { EmailTemplateDetail } from '@/types/admin'

const starterHtml = '<h2>{{heading}}</h2><p>{{body}}</p><p><a href="{{ctaUrl}}">{{ctaText}}</a></p>'

export function EmailTemplateFormModal({ open, template, onClose }: { open: boolean; template: EmailTemplateDetail | null; onClose: () => void }) {
  const saveTemplate = useEmailMarketingStore((s) => s.saveTemplate)
  const [key, setKey] = useState(template?.key ?? 'marketing.promo')
  const [name, setName] = useState(template?.name ?? '')
  const [subject, setSubject] = useState(template?.subject ?? '{{subject}}')
  const [preheader, setPreheader] = useState(template?.preheader ?? '')
  const [locale, setLocale] = useState(template?.locale ?? 'vi-VN')
  const [htmlBody, setHtmlBody] = useState(template?.htmlBody ?? starterHtml)
  const [textBody, setTextBody] = useState(template?.textBody ?? '')
  const [isActive, setIsActive] = useState(template?.isActive ?? true)
  const [saving, setSaving] = useState(false)

  if (!open) return null

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setSaving(true)
    try {
      const payload = { key, name, subject, preheader: preheader || undefined, htmlBody, textBody: textBody || undefined, locale, isActive }
      await saveTemplate(payload, template?.id)
      onClose()
    } finally {
      setSaving(false)
    }
  }

  return <ModalOverlay open={open} onClose={onClose} className="max-w-3xl p-6">
    <h2 className="mb-4 text-lg font-semibold text-burgundy">{template ? 'Sửa mẫu email' : 'Thêm mẫu email'}</h2>
    <form onSubmit={handleSubmit} className="space-y-4 max-h-[75vh] overflow-y-auto pr-1">
      <div className="grid gap-4 md:grid-cols-2"><div><Label htmlFor="email-template-key">Khóa</Label><Input id="email-template-key" value={key} onChange={(e) => setKey(e.target.value)} disabled={!!template} required /></div><div><Label htmlFor="email-template-name">Tên</Label><Input id="email-template-name" value={name} onChange={(e) => setName(e.target.value)} required /></div></div>
      <div><Label htmlFor="email-template-subject">Tiêu đề</Label><Input id="email-template-subject" value={subject} onChange={(e) => setSubject(e.target.value)} required /><p className="mt-1 text-xs text-gray-500">Có thể dùng token như {'{{subject}}'}, {'{{name}}'}, {'{{ctaText}}'}.</p></div>
      <div className="grid gap-4 md:grid-cols-2"><div><Label htmlFor="email-template-preheader">Preheader</Label><Input id="email-template-preheader" value={preheader} onChange={(e) => setPreheader(e.target.value)} /></div><div><Label htmlFor="email-template-locale">Locale</Label><Input id="email-template-locale" value={locale} onChange={(e) => setLocale(e.target.value)} required /></div></div>
      <div><Label htmlFor="email-template-html">HTML body</Label><Textarea id="email-template-html" className="min-h-56 font-mono text-xs" value={htmlBody} onChange={(e) => setHtmlBody(e.target.value)} required /></div>
      <div><Label htmlFor="email-template-text">Text body</Label><Textarea id="email-template-text" className="min-h-24" value={textBody} onChange={(e) => setTextBody(e.target.value)} /></div>
      <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} /> Hoạt động</label>
      <div className="rounded bg-amber-50 p-3 text-xs text-amber-800">Checklist: tiêu đề ngắn, một CTA chính, có bản text, email marketing cần link hủy đăng ký.</div>
      <div className="flex justify-end gap-2"><Button type="button" variant="outline" onClick={onClose}>Hủy</Button><Button type="submit" disabled={saving}>{saving ? 'Đang lưu...' : 'Lưu'}</Button></div>
    </form>
  </ModalOverlay>
}
