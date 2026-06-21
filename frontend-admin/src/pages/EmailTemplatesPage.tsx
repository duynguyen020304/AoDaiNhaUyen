import { useEffect, useRef, useState } from "react";
import { Eye, ExternalLink } from "lucide-react";
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
import { getEmailTemplate } from "@/api/emailMarketing";
import { buildEmailPreviewDocument, openEmailPreviewInNewTab } from "@/lib/emailPreview";
import { renderEmailTemplateHtml, normalizeTemplateConfig, resolveEmailTemplateType } from "@/lib/reactEmailTemplates";
import { useFeedback } from "@/components/ui/feedbackContext";
import type { EmailTemplateDetail, EmailTemplateListItem } from "@/types/admin";

async function renderPreviewTemplate(template: EmailTemplateDetail) {
  const templateType = resolveEmailTemplateType(template.key, template.templateType);
  if (!templateType) {
    throw new Error("Template này là legacy/system transactional, không thuộc bộ React Email marketing do dev định nghĩa.");
  }
  return renderEmailTemplateHtml({
    templateType,
    subject: template.subject,
    preheader: template.preheader,
    config: normalizeTemplateConfig(templateType, template.configJson),
  });
}

export function EmailTemplatesPage() {
  const {
    templates,
    loading,
    error,
    totalPages,
    currentPage,
    fetchTemplates,
  } = useEmailMarketingStore();
  const { toast } = useFeedback();
  const prevError = useRef(error);
  useEffect(() => {
    if (error && error !== prevError.current) {
      toast(error, "error");
    }
    prevError.current = error;
  }, [error, toast]);
  const [search, setSearch] = useState("");
  const [preview, setPreview] = useState<EmailTemplateDetail | null>(null);
  const [previewHtml, setPreviewHtml] = useState("");
  useEffect(() => {
    fetchTemplates(search).catch(() => {});
  }, [fetchTemplates, search]);
  async function openPreviewTemplate(template: EmailTemplateListItem) {
    setPreviewHtml("");
    const detail = await getEmailTemplate(template.id);
    setPreview(detail);
    try {
      setPreviewHtml(await renderPreviewTemplate(detail));
    } catch (error) {
      toast(error instanceof Error ? error.message : "Không render được preview React Email.", "error");
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-ink">Mẫu email</h1>
          <p className="text-sm text-muted-foreground">
Danh sách template React Email do dev lập trình sẵn. Admin chỉ xem và preview.
          </p>
        </div>
      </div>
      {/* error displayed via toast */}
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
              <TableHead>Loại</TableHead>
              <TableHead>Locale</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead className="text-right">Preview</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={7}>Đang tải...</TableCell>
              </TableRow>
            ) : (
              templates.map((t) => {
                const templateType = resolveEmailTemplateType(t.key, t.templateType);
                return (
                  <TableRow key={t.id}>
                    <TableCell className="font-mono text-xs">{t.key}</TableCell>
                    <TableCell>{t.name}</TableCell>
                    <TableCell>{t.subject}</TableCell>
                    <TableCell><Badge variant="outline">{templateType ?? "legacy"}</Badge></TableCell>
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
                        disabled={!templateType}
                        title={templateType ? "Preview" : "Legacy template không có React Email preview"}
                        onClick={() => void openPreviewTemplate(t)}
                      >
                        <Eye className="size-4 mr-1" />
                        Preview
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })
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
      {preview && (
        <div
          role="dialog"
          aria-modal="true"
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
          onMouseDown={(e) => {
            if (e.target === e.currentTarget) { setPreview(null); setPreviewHtml(''); }
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
                disabled={!previewHtml}
                onClick={() => {
                  const opened = openEmailPreviewInNewTab(preview.subject, preview.preheader, previewHtml);
                  toast(opened ? "Đã mở preview mẫu email." : "Trình duyệt chặn popup preview.", opened ? "success" : "error");
                }}
              >
                <ExternalLink className="size-4 mr-2" />
                Mở tab mới
              </Button>
            </div>
            <button
              className="absolute right-4 top-4 text-sm text-gray-500 hover:text-gray-900"
              onClick={() => { setPreview(null); setPreviewHtml(''); }}
            >
              Đóng
            </button>
            <iframe
                title="Email preview"
                className="h-[calc(85vh-92px)] w-full rounded-lg border"
                srcDoc={previewHtml ? buildEmailPreviewDocument(preview.subject, preview.preheader, previewHtml) : '<p style="font-family:Arial;padding:24px">Đang render preview...</p>'}
              />
          </div>
        </div>
      )}
    </div>
  );
}
