import { useEffect, useState } from "react";
import { Plus, RotateCcw, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
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
  const [search, setSearch] = useState("");
  const [edit, setEdit] = useState<EmailTemplateDetail | null>(null);
  const [open, setOpen] = useState(false);
  useEffect(() => {
    fetchTemplates(search).catch(() => {});
  }, [fetchTemplates, search]);
  async function openEditTemplate(template: EmailTemplateListItem) {
    setEdit(await getEmailTemplate(template.id));
    setOpen(true);
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-burgundy">Mẫu email</h1>
          <p className="text-sm text-gray-600">
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
        <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}
      <Input
        placeholder="Tìm theo khóa, tên, tiêu đề..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
      />
      <div className="rounded-lg border bg-white">
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
                      onClick={() => void openEditTemplate(t)}
                    >
                      Sửa
                    </Button>
                    {t.isDeleted ? (
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => restoreTemplate(t.id)}
                      >
                        <RotateCcw className="size-4" />
                      </Button>
                    ) : (
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => deleteTemplate(t.id)}
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
      </div>
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
    </div>
  );
}
