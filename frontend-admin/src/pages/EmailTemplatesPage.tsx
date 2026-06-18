import { useEffect, useState } from "react";
import { Eye, ExternalLink, Plus, RotateCcw, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useEmailMarketingStore } from "@/stores/emailMarketingStore";
import { EmailTemplateFormModal } from "@/components/admin/EmailTemplateFormModal";
import { getEmailTemplate } from "@/api/emailMarketing";
import { buildEmailPreviewDocument, openEmailPreviewInNewTab } from "@/lib/emailPreview";
import { useFeedback } from "@/components/ui/feedbackContext";
import type { EmailTemplateDetail, EmailTemplateListItem } from "@/types/admin";

export function EmailTemplatesPage() {
  const {
    templates,
    loading,
    error,
    totalPages,
    currentPage,
    fetchTemplates,
    deleteTemplate,
    restoreTemplate,
  } = useEmailMarketingStore();
  const { confirm, toast } = useFeedback();
  const [search, setSearch] = useState("");
  const [edit, setEdit] = useState<EmailTemplateDetail | null>(null);
  const [preview, setPreview] = useState<EmailTemplateDetail | null>(null);
  const [open, setOpen] = useState(false);
  useEffect(() => {
    fetchTemplates(search).catch(() => {});
  }, [fetchTemplates, search]);
  async function openEditTemplate(template: EmailTemplateListItem) {
    setEdit(await getEmailTemplate(template.id));
    setOpen(true);
  }

  async function openPreviewTemplate(template: EmailTemplateListItem) {
    setPreview(await getEmailTemplate(template.id));
  }

  async function handleDeleteTemplate(id: string) {
    const ok = await confirm({
      title: "Xóa mẫu email?",
      message: "Mẫu email sẽ bị đánh dấu đã xóa và có thể khôi phục sau.",
      confirmText: "Xóa",
      destructive: true,
    });
    if (!ok) return;
    await deleteTemplate(id);
    toast("Đã xóa mẫu email.", "success");
  }

  async function handleRestoreTemplate(id: string) {
    await restoreTemplate(id);
    toast("Đã khôi phục mẫu email.", "success");
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Mẫu email</h1>
          <p className="text-sm text-muted-foreground">
            Tạo và chỉnh nội dung HTML cho email.
          </p>
        </div>
        <Button
          onClick={() => {
            setEdit(null);
            setOpen(true);
          }}
        >
          <Plus className="size-4 mr-2" />
          Thêm mẫu
        </Button>
      </div>
      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}
      <div className="mb-4 max-w-md">
        <Input
          placeholder="Tìm theo khóa, tên, tiêu đề..."
        value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>
      <Card className="overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Khóa</TableHead>
              <TableHead>Tên</TableHead>
              <TableHead>Tiêu đề</TableHead>
              <TableHead>Locale</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead className="text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={6}>Đang tải...</TableCell>
              </TableRow>
            ) : (
              templates.map((t) => (
                <TableRow
                  key={t.id}
                  onDoubleClick={() => void openEditTemplate(t)}
                >
                  <TableCell className="font-mono text-xs">{t.key}</TableCell>
                  <TableCell>{t.name}</TableCell>
                  <TableCell>{t.subject}</TableCell>
                  <TableCell>{t.locale}</TableCell>
                  <TableCell>
                    <Badge
                      variant={
                        t.isDeleted
                          ? "outline"
                          : t.isActive
                            ? "default"
                            : "outline"
                      }
                    >
                      {t.isDeleted
                        ? "Đã xóa"
                        : t.isActive
                          ? "Hoạt động"
                          : "Tắt"}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => void openPreviewTemplate(t)}
                    >
                      <Eye className="size-4 mr-1" />
                      Preview
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => void openEditTemplate(t)}
                    >
                      Sửa
                    </Button>
                    {t.isDeleted ? (
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => void handleRestoreTemplate(t.id)}
                      >
                        <RotateCcw className="size-4" />
                      </Button>
                    ) : (
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => void handleDeleteTemplate(t.id)}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </Card>
      <div className="flex justify-end gap-2">
        <Button
          variant="outline"
          disabled={currentPage <= 1}
          onClick={() => fetchTemplates(search, currentPage - 1)}
        >
          Trước
        </Button>
        <span className="py-2 text-sm">
          {currentPage}/{totalPages}
        </span>
        <Button
          variant="outline"
          disabled={currentPage >= totalPages}
          onClick={() => fetchTemplates(search, currentPage + 1)}
        >
          Sau
        </Button>
      </div>
      <EmailTemplateFormModal
        key={edit?.id ?? 'new'}
        open={open}
        template={edit}
        onClose={() => setOpen(false)}
      />
      {preview && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          onMouseDown={(e) => {
            if (e.target === e.currentTarget) setPreview(null);
          }}
        >
          <div className="relative h-[85vh] w-full max-w-5xl rounded-xl bg-white p-4 shadow-lg">
            <div className="mb-3 flex items-center justify-between gap-3 pr-8">
              <div>
                <h2 className="text-lg font-semibold text-ink">Preview mẫu email</h2>
                <p className="text-sm text-muted-foreground">{preview.subject}</p>
              </div>
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  const opened = openEmailPreviewInNewTab(preview.subject, preview.preheader, preview.htmlBody);
                  toast(opened ? "Đã mở preview mẫu email." : "Trình duyệt chặn popup preview.", opened ? "success" : "error");
                }}
              >
                <ExternalLink className="size-4 mr-2" />
                Mở tab mới
              </Button>
            </div>
            <button
              className="absolute right-4 top-4 text-sm text-gray-500 hover:text-gray-900"
              onClick={() => setPreview(null)}
            >
              Đóng
            </button>
            <iframe
              title="Email preview"
              className="h-[calc(85vh-92px)] w-full rounded-lg border"
              srcDoc={buildEmailPreviewDocument(preview.subject, preview.preheader, preview.htmlBody)}
            />
          </div>
        </div>
      )}
    </div>
  );
}
