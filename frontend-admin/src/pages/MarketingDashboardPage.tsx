import { useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Mail, UsersRound, FileText } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useEmailMarketingStore } from "@/stores/emailMarketingStore";
import { EmailTemplatesPage } from "@/pages/EmailTemplatesPage";
import { MarketingSendPage } from "@/pages/MarketingSendPage";
import { SubscribersPage } from "@/pages/SubscribersPage";

type MarketingTab = "send" | "templates" | "subscribers";

const tabs: Array<{ id: MarketingTab; label: string; description: string }> = [
  {
    id: "send",
    label: "Gửi chiến dịch",
    description: "Chọn người nhận, template và nội dung đính kèm.",
  },
  {
    id: "templates",
    label: "Mẫu email",
    description: "Tạo và chỉnh nội dung HTML cho email.",
  },
  {
    id: "subscribers",
    label: "Người đăng ký",
    description: "Theo dõi consent và trạng thái nhận email.",
  },
];

function normalizeTab(value: string | null): MarketingTab {
  return tabs.some((tab) => tab.id === value) ? (value as MarketingTab) : "send";
}

export function MarketingDashboardPage() {
  const { stats, fetchStats, loading, error } = useEmailMarketingStore();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const activeTab = normalizeTab(searchParams.get("tab"));
  useEffect(() => {
    fetchStats().catch(() => {});
  }, [fetchStats]);
  const cards = [
    ["Tổng đăng ký", stats?.totalSubscribers ?? 0, UsersRound],
    ["Đang nhận tin", stats?.activeSubscribers ?? 0, Mail],
    ["Mẫu email", stats?.templateCount ?? 0, FileText],
  ] as const;
  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-bold tracking-tight text-ink">Marketing email</h1>
        <p className="text-sm text-muted-foreground">
          Quản lý mẫu email và người đăng ký.
        </p>
      </div>
      {error && (
        <div className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {error}
        </div>
      )}
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {cards.map(([label, value, Icon]) => (
          <Card key={label} className="min-h-28">
            <CardHeader className="flex flex-row items-start justify-between gap-3 p-5 pb-2">
              <CardTitle className="text-sm font-semibold leading-5">{label}</CardTitle>
              <span className="rounded-lg bg-burgundy/5 p-2 text-wine">
                <Icon className="size-4" />
              </span>
            </CardHeader>
            <CardContent className="p-5 pt-2">
              <div className="text-3xl font-bold leading-none">
                {loading ? "..." : value}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
      <div className="rounded-xl border bg-white p-2 shadow-sm">
        <div
          className="grid gap-2 md:grid-cols-3"
          role="tablist"
          aria-label="Chức năng marketing email"
        >
          {tabs.map((tab) => {
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                type="button"
                role="tab"
                aria-selected={isActive}
                className={`rounded-lg px-4 py-3 text-left transition ${
                  isActive
                    ? "bg-primary text-primary-foreground shadow-sm"
                    : "text-gray-700 hover:bg-gray-50"
                }`}
                onClick={() => navigate(`/admin/marketing?tab=${tab.id}`)}
              >
                <span className="block text-sm font-semibold">{tab.label}</span>
                <span
                  className={`mt-1 block text-xs ${
                    isActive ? "text-primary-foreground/80" : "text-gray-500"
                  }`}
                >
                  {tab.description}
                </span>
              </button>
            );
          })}
        </div>
      </div>

      <section role="tabpanel" className="min-w-0">
        {activeTab === "send" && <MarketingSendPage />}
        {activeTab === "templates" && <EmailTemplatesPage />}
        {activeTab === "subscribers" && <SubscribersPage />}
      </section>
    </div>
  );
}
