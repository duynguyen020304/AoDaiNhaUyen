import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { Select } from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { useEmailMarketingStore } from "@/stores/emailMarketingStore";
import { useFeedback } from "@/components/ui/feedbackContext";

export function SubscribersPage() {
  const {
    subscribers,
    loading,
    error,
    totalPages,
    currentPage,
    fetchSubscribers,
    unsubscribe,
    importSubscribers,
  } = useEmailMarketingStore();
  const { confirm, toast } = useFeedback();
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [emails, setEmails] = useState("");
  useEffect(() => {
    fetchSubscribers(search, status).catch(() => {});
  }, [fetchSubscribers, search, status]);
  async function handleImport() {
    const rawList = emails
      .split(/[\n,;]+/)
      .map((x) => x.trim().toLowerCase())
      .filter(Boolean);
    if (!rawList.length) {
      toast("Vui lòng nhập ít nhất một email.", "error");
      return;
    }
    const list = [...new Set(rawList)];
    const invalid = list.find((email) => !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email));
    if (invalid) {
      toast(`Email không hợp lệ: ${invalid}`, "error");
      return;
    }
    if (list.length > 500) {
      toast("Mỗi lần chỉ nhập tối đa 500 email.", "error");
      return;
    }
    await importSubscribers({ emails: list, source: "admin_import" });
    setEmails("");
    toast("Đã nhập danh sách người đăng ký.", "success");
  }
  async function handleUnsubscribe(id: string) {
    const subscriber = subscribers.find((item) => item.id === id);
    if (!subscriber) {
      toast("Không tìm thấy người đăng ký.", "error");
      return;
    }
    if (subscriber.status === "unsubscribed") {
      toast("Người này đã hủy nhận email.", "error");
      return;
    }
    const ok = await confirm({
      title: "Hủy nhận email?",
      message: "Người đăng ký này sẽ chuyển sang trạng thái unsubscribed.",
      confirmText: "Hủy nhận",
      destructive: true,
    });
    if (!ok) return;
    await unsubscribe(id);
    toast("Đã hủy nhận email.", "success");
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-ink">Người đăng ký</h1>
        <p className="text-sm text-muted-foreground">
          Theo dõi consent và trạng thái nhận email.
        </p>
      </div>
      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}
      <div className="grid gap-3 md:grid-cols-[1fr_180px] mb-4">
        <Input
          placeholder="Tìm email..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Select
          className="w-full"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
        >
          <option value="">Tất cả</option>
          <option value="pending">Pending</option>
          <option value="active">Active</option>
          <option value="unsubscribed">Unsubscribed</option>
        </Select>
      </div>
      <Card className="mb-4 p-3">
        <label className="text-sm font-medium">Nhập email nhanh</label>
        <textarea
          className="mt-2 min-h-20 w-full rounded-md border p-2 text-sm"
          value={emails}
          onChange={(e) => setEmails(e.target.value)}
          placeholder="email1@example.com, email2@example.com"
        />
        <Button className="mt-2" size="sm" onClick={handleImport}>
          Nhập danh sách
        </Button>
      </Card>
      <Card className="overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Email</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Đăng ký</TableHead>
              <TableHead>Gửi gần nhất</TableHead>
              <TableHead className="text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={5}>Đang tải...</TableCell>
              </TableRow>
            ) : (
              subscribers.map((s) => (
                <TableRow key={s.id}>
                  <TableCell>{s.email}</TableCell>
                  <TableCell>
                    <Badge
                      variant={s.status === "active" ? "default" : "outline"}
                    >
                      {s.status}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    {s.subscribedAt
                      ? new Date(s.subscribedAt).toLocaleDateString("vi-VN")
                      : "—"}
                  </TableCell>
                  <TableCell>
                    {s.lastSentAt
                      ? new Date(s.lastSentAt).toLocaleDateString("vi-VN")
                      : "—"}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={s.status === "unsubscribed"}
                      onClick={() => void handleUnsubscribe(s.id)}
                    >
                      Hủy nhận
                    </Button>
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
          onClick={() => fetchSubscribers(search, status, currentPage - 1)}
        >
          Trước
        </Button>
        <span className="py-2 text-sm">
          {currentPage}/{totalPages}
        </span>
        <Button
          variant="outline"
          disabled={currentPage >= totalPages}
          onClick={() => fetchSubscribers(search, status, currentPage + 1)}
        >
          Sau
        </Button>
      </div>
    </div>
  );
}
