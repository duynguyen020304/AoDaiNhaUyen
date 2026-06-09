import { useEffect } from "react";
import { Link } from "react-router-dom";
import { Mail, Send, UsersRound, FileText } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useEmailMarketingStore } from "@/stores/emailMarketingStore";

export function MarketingDashboardPage() {
  const { stats, fetchStats, loading, error } = useEmailMarketingStore();
  useEffect(() => {
    fetchStats().catch(() => {});
  }, [fetchStats]);
  const cards = [
    ["Tổng đăng ký", stats?.totalSubscribers ?? 0, UsersRound],
    ["Đang nhận tin", stats?.activeSubscribers ?? 0, Mail],
    ["Email đã gửi hôm nay", stats?.sentJobsToday ?? 0, Send],
    ["Mẫu email", stats?.templateCount ?? 0, FileText],
  ] as const;
  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-bold text-burgundy">Marketing email</h1>
        <p className="text-sm text-gray-600">
          Quản lý mẫu email, người đăng ký và hàng đợi gửi.
        </p>
      </div>
      {error && (
        <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}
      <div className="grid gap-4 md:grid-cols-4">
        {cards.map(([label, value, Icon]) => (
          <Card key={label}>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="text-sm font-medium">{label}</CardTitle>
              <Icon className="size-4 text-wine" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {loading ? "..." : value}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        <Link
          className="inline-flex h-9 items-center justify-center rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground"
          to="/admin/email-templates"
        >
          Mẫu email
        </Link>
        <Link
          className="inline-flex h-9 items-center justify-center rounded-lg border border-input bg-white px-4 py-2 text-sm font-medium"
          to="/admin/subscribers"
        >
          Người đăng ký
        </Link>
        <Link
          className="inline-flex h-9 items-center justify-center rounded-lg border border-input bg-white px-4 py-2 text-sm font-medium"
          to="/admin/email-queue"
        >
          Hàng đợi email
        </Link>
      </div>
    </div>
  );
}
