import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
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
import { useFeedback } from "@/components/ui/feedbackContext";
import type { EmailJobListItem } from "@/types/admin";

export function EmailQueuePage() {
  const {
    jobs,
    loading,
    error,
    totalPages,
    currentPage,
    fetchJobs,
    retryJob,
    cancelJob,
  } = useEmailMarketingStore();
  const { confirm, toast } = useFeedback();
  const [status, setStatus] = useState("");
  useEffect(() => {
    fetchJobs(status).catch(() => {});
  }, [fetchJobs, status]);
  async function handleRetry(job: EmailJobListItem) {
    if (job.status === "sent") {
      toast("Email đã gửi không thể gửi lại.", "error");
      return;
    }
    if (job.status === "sending") {
      toast("Email đang gửi, chưa thể gửi lại.", "error");
      return;
    }
    if (!job.toEmail) {
      toast("Job thiếu email nhận.", "error");
      return;
    }
    const ok = await confirm({
      title: "Gửi lại email?",
      message: `Đưa job ${job.toEmail} về hàng đợi gửi lại.`,
      confirmText: "Gửi lại",
    });
    if (!ok) return;
    await retryJob(job.id);
    toast("Đã đưa email vào hàng đợi gửi lại.", "success");
  }

  async function handleCancel(job: EmailJobListItem) {
    if (job.status === "sent") {
      toast("Email đã gửi không thể hủy.", "error");
      return;
    }
    if (job.status === "sending") {
      toast("Email đang gửi, không thể hủy an toàn.", "error");
      return;
    }
    if (job.status === "cancelled") {
      toast("Email này đã bị hủy.", "error");
      return;
    }
    const ok = await confirm({
      title: "Hủy email?",
      message: `Hủy job gửi tới ${job.toEmail}.`,
      confirmText: "Hủy job",
      destructive: true,
    });
    if (!ok) return;
    await cancelJob(job.id);
    toast("Đã hủy email trong hàng đợi.", "success");
  }

  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold text-burgundy">Hàng đợi email</h1>
        <p className="text-sm text-gray-600">
          Theo dõi job gửi email và xử lý lỗi.
        </p>
      </div>
      {error && (
        <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}
      <div className="flex flex-wrap items-center gap-3">
        <select
          className="h-9 rounded-md border bg-white px-3 text-sm"
          value={status}
          onChange={(e) => setStatus(e.target.value)}
        >
          <option value="">Tất cả</option>
          <option value="queued">Queued</option>
          <option value="sending">Sending</option>
          <option value="sent">Sent</option>
          <option value="dead">Dead</option>
          <option value="failed">Failed</option>
          <option value="cancelled">Cancelled</option>
        </select>
        <span className="text-xs text-gray-500">
          Trạng thái hiện có: queued, sending, sent, dead, failed, cancelled (6).
        </span>
      </div>
      <div className="rounded-lg border bg-white">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Email</TableHead>
              <TableHead>Mẫu</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Retry</TableHead>
              <TableHead>Lịch gửi</TableHead>
              <TableHead>Lỗi</TableHead>
              <TableHead className="text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={7}>Đang tải...</TableCell>
              </TableRow>
            ) : (
              jobs.map((j) => (
                <TableRow key={j.id}>
                  <TableCell>{j.toEmail}</TableCell>
                  <TableCell className="font-mono text-xs">
                    {j.templateKey}
                  </TableCell>
                  <TableCell>
                    <Badge
                      variant={
                        j.status === "sent"
                          ? "default"
                          : j.status === "dead"
                            ? "warning"
                            : "outline"
                      }
                    >
                      {j.status}
                    </Badge>
                  </TableCell>
                  <TableCell>{j.retryCount}</TableCell>
                  <TableCell>
                    {new Date(j.scheduledAt).toLocaleString("vi-VN")}
                  </TableCell>
                  <TableCell className="max-w-xs truncate">
                    {j.errorMessage ?? "—"}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={j.status === "sent" || j.status === "sending"}
                      onClick={() => void handleRetry(j)}
                    >
                      Gửi lại
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      disabled={j.status === "sent" || j.status === "sending" || j.status === "cancelled"}
                      onClick={() => void handleCancel(j)}
                    >
                      Hủy
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
          onClick={() => fetchJobs(status, currentPage - 1)}
        >
          Trước
        </Button>
        <span className="py-2 text-sm">
          {currentPage}/{totalPages}
        </span>
        <Button
          variant="outline"
          disabled={currentPage >= totalPages}
          onClick={() => fetchJobs(status, currentPage + 1)}
        >
          Sau
        </Button>
      </div>
    </div>
  );
}
