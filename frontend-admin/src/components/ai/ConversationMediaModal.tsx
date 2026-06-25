import { useEffect, useMemo, useState } from 'react'
import { Check, FileText, Image, Loader2, Paperclip, Trash2, Video, X } from 'lucide-react'
import { uploadSocialMedia } from '@/api/social'

export interface ConversationMediaItem {
  id: string
  fileName: string
  contentType: string
  fileSize: number
  publicUrl: string
  objectUrl?: string
  status: 'uploaded' | 'local' | 'uploading' | 'error'
  createdAt: string
}

const STORAGE_KEY = 'admin-ai-conversation-media'
const IMAGE_VIDEO_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif', 'video/mp4', 'video/quicktime', 'video/webm']
const DOCUMENT_TYPES = [
  'application/pdf',
  'application/vnd.ms-excel',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'text/csv',
]
export const CONVERSATION_MEDIA_ACCEPT = [...IMAGE_VIDEO_TYPES, ...DOCUMENT_TYPES].join(',')

function formatBytes(bytes: number) {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB']
  const index = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1)
  return `${(bytes / 1024 ** index).toFixed(index === 0 ? 0 : 1)} ${units[index]}`
}

export function loadConversationMedia(): ConversationMediaItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : []
  } catch {
    return []
  }
}

export function persistConversationMedia(items: ConversationMediaItem[]) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(items.map(({ objectUrl: _objectUrl, ...item }) => item)))
}

function isImageVideo(file: File) {
  return IMAGE_VIDEO_TYPES.includes(file.type)
}

function isDocument(file: File) {
  return DOCUMENT_TYPES.includes(file.type)
}

function mediaIcon(contentType: string) {
  if (contentType.startsWith('image/')) return <Image className="size-4" />
  if (contentType.startsWith('video/')) return <Video className="size-4" />
  return <FileText className="size-4" />
}

function validateConversationMedia(file: File): string | null {
  if (!isImageVideo(file) && !isDocument(file)) {
    return `File ${file.name} không hỗ trợ. Dùng ảnh/video, PDF, Excel hoặc CSV.`
  }

  const maxBytes = file.type.startsWith('video/') ? 200 * 1024 * 1024 : 20 * 1024 * 1024
  if (file.size > maxBytes) {
    return file.type.startsWith('video/') ? `Video ${file.name} vượt quá 200MB.` : `File ${file.name} vượt quá 20MB.`
  }

  return null
}

export async function uploadConversationMediaFiles(
  files: FileList | File[],
  onItem?: (item: ConversationMediaItem) => void,
): Promise<{ items: ConversationMediaItem[]; errors: string[] }> {
  const selectedFiles = Array.from(files)
  const uploadedItems: ConversationMediaItem[] = []
  const errors: string[] = []

  for (const file of selectedFiles) {
    const validationError = validateConversationMedia(file)
    if (validationError) {
      errors.push(validationError)
      continue
    }

    const id = crypto.randomUUID()
    const base: ConversationMediaItem = {
      id,
      fileName: file.name,
      contentType: file.type || 'application/octet-stream',
      fileSize: file.size,
      publicUrl: '',
      objectUrl: URL.createObjectURL(file),
      status: 'uploading',
      createdAt: new Date().toISOString(),
    }
    onItem?.(base)

    try {
      const upload = await uploadSocialMedia(file)
      const uploaded: ConversationMediaItem = {
        ...base,
        publicUrl: upload.publicUrl,
        objectUrl: upload.publicUrl,
        status: 'uploaded',
      }
      uploadedItems.push(uploaded)
      onItem?.(uploaded)
    } catch (err) {
      errors.push(err instanceof Error ? err.message : `Không upload được ${file.name}.`)
      onItem?.({ ...base, status: 'error' })
    }
  }

  if (uploadedItems.length > 0) {
    const existing = loadConversationMedia()
    const next = [...uploadedItems, ...existing.filter((item) => !uploadedItems.some((uploaded) => uploaded.id === item.id))]
    persistConversationMedia(next)
  }

  return { items: uploadedItems, errors }
}

interface Props {
  open: boolean
  selectedUrls: string[]
  onClose: () => void
  onApply: (items: ConversationMediaItem[]) => void
  onDelete?: (id: string) => void
}

export function ConversationMediaModal({ open, selectedUrls, onClose, onApply, onDelete }: Props) {
  const [items, setItems] = useState<ConversationMediaItem[]>([])
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  useEffect(() => {
    if (!open) return
    const loaded = loadConversationMedia()
    setItems(loaded)
    setSelectedIds(new Set(loaded.filter((item) => item.publicUrl && selectedUrls.includes(item.publicUrl)).map((item) => item.id)))
  }, [open, selectedUrls])

  useEffect(() => {
    if (open) persistConversationMedia(items)
  }, [items, open])

  const selectedItems = useMemo(
    () => items.filter((item) => selectedIds.has(item.id) && item.status !== 'uploading' && item.status !== 'error'),
    [items, selectedIds],
  )

  function toggle(id: string) {
    setSelectedIds((current) => {
      const next = new Set(current)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  function remove(id: string) {
    setSelectedIds((current) => {
      const next = new Set(current)
      next.delete(id)
      return next
    })
    setItems((current) => current.filter((item) => item.id !== id))
    onDelete?.(id)
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" role="dialog" aria-modal="true" aria-label="Kho media cuộc trò chuyện">
      <div className="flex max-h-[88vh] w-full max-w-4xl flex-col overflow-hidden rounded-3xl bg-white shadow-2xl">
        <div className="flex items-start justify-between gap-4 border-b border-gray-200 px-5 py-4">
          <div>
            <h3 className="text-base font-semibold text-gray-900">Media cuộc trò chuyện</h3>
            <p className="mt-1 text-xs text-gray-500">
              Upload nằm cạnh nút gửi trong ô chat. Modal này dùng để xem media đã upload lên S3, chọn đính kèm lại hoặc xoá. PDF/Excel có URL public; để LLM đọc nội dung thật cần thêm bước trích xuất server-side/OCR hoặc copy nội dung vào chat.
            </p>
          </div>
          <button type="button" onClick={onClose} className="rounded-full p-2 text-gray-500 hover:bg-gray-100" aria-label="Đóng media modal">
            <X className="size-5" />
          </button>
        </div>

        <div className="flex flex-wrap items-center gap-3 border-b border-gray-100 px-5 py-3">
          <span className="text-xs text-gray-500">Chọn: {selectedItems.length} tệp</span>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto p-5">
          {items.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-gray-200 p-10 text-center text-sm text-gray-500">
              Chưa có media. Dùng nút kẹp giấy cạnh nút gửi để upload ảnh/video lên S3 hoặc đính kèm PDF/Excel.
            </div>
          ) : (
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {items.map((item) => {
                const selected = selectedIds.has(item.id)
                const selectable = item.status === 'uploaded' || item.status === 'local'
                return (
                  <div key={item.id} className={`rounded-2xl border p-3 ${selected ? 'border-wine bg-wine/5' : 'border-gray-200 bg-white'}`}>
                    <div className="mb-3 flex h-32 items-center justify-center overflow-hidden rounded-xl bg-gray-50 text-gray-500">
                      {item.contentType.startsWith('image/') && (item.objectUrl || item.publicUrl) ? (
                        <img src={item.objectUrl || item.publicUrl} alt={item.fileName} className="h-full w-full object-cover" />
                      ) : item.contentType.startsWith('video/') && (item.objectUrl || item.publicUrl) ? (
                        <video src={item.objectUrl || item.publicUrl} className="h-full w-full object-cover" controls />
                      ) : (
                        <div className="flex flex-col items-center gap-2 text-xs">
                          {mediaIcon(item.contentType)}
                          <span>Tài liệu</span>
                        </div>
                      )}
                    </div>
                    <div className="flex items-start gap-2">
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium text-gray-800" title={item.fileName}>{item.fileName}</p>
                        <p className="text-xs text-gray-500">{formatBytes(item.fileSize)} · {item.contentType || 'unknown'}</p>
                        {item.status === 'uploading' && <p className="mt-1 inline-flex items-center gap-1 text-xs text-amber-600"><Loader2 className="size-3 animate-spin" /> Đang upload S3...</p>}
                        {item.status === 'local' && <p className="mt-1 text-xs text-blue-600">Tài liệu đã lưu cục bộ, chưa có URL public.</p>}
                        {item.status === 'error' && <p className="mt-1 text-xs text-red-600">Upload lỗi.</p>}
                      </div>
                      <div className="flex shrink-0 gap-1">
                        <button type="button" onClick={() => toggle(item.id)} disabled={!selectable} className="rounded-lg border border-gray-200 p-2 text-gray-600 hover:bg-gray-50 disabled:opacity-40" aria-label="Chọn media">
                          {selected ? <Check className="size-4 text-wine" /> : <Paperclip className="size-4" />}
                        </button>
                        <button type="button" onClick={() => remove(item.id)} className="rounded-lg border border-gray-200 p-2 text-red-600 hover:bg-red-50" aria-label="Xóa media">
                          <Trash2 className="size-4" />
                        </button>
                      </div>
                    </div>
                    {item.publicUrl && <p className="mt-2 truncate text-[11px] text-gray-400" title={item.publicUrl}>{item.publicUrl}</p>}
                  </div>
                )
              })}
            </div>
          )}
        </div>

        <div className="flex items-center justify-end gap-2 border-t border-gray-200 px-5 py-4">
          <button type="button" onClick={onClose} className="rounded-xl border border-gray-200 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">Hủy</button>
          <button type="button" onClick={() => onApply(selectedItems)} className="rounded-xl bg-wine px-4 py-2 text-sm font-medium text-white hover:bg-wine/90">
            Đính kèm {selectedItems.length > 0 ? `(${selectedItems.length})` : ''}
          </button>
        </div>
      </div>
    </div>
  )
}
