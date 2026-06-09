import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [emails, setEmails] = useState("");
  useEffect(() => {
    fetchSubscribers(search, status).catch(() => {});
  }, [fetchSubscribers, search, status]);
  async function handleImport() {
    const list = emails
      .split(/[\n,;]+/)
      .map((x) => x.trim())
      .filter(Boolean);
    if (!list.length) return;
    await importSubscribers({ emails: list, source: "admin_import" });
    setEmails("");
  }
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold text-burgundy">Người đăng ký</h1>
        <p className="text-sm text-gray-600">
          Theo dõi consent và trạng thái nhận email.
        </p>
      </div>
      {error && (
        <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}
      <div className="grid gap-3 md:grid-cols-[1fr_180px]">
        <Input
          placeholder="Tìm email..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select
          className="h-9 rounded-md border bg-white px-3 text-sm"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
        >
          <option value="">Tất cả</option>
          <option value="pending">Pending</option>
          <option value="active">Active</option>
          <option value="unsubscribed">Unsubscribed</option>
        </select>
      </div>
      <div className="rounded-lg border bg-white p-3">
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
      </div>
      <div className="rounded-lg border bg-white">
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
                      onClick={() => unsubscribe(s.id)}
                    >
                      Hủy nhận
                    </Button>
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
