import { useEffect, useMemo, useState } from "react";
import type { Dispatch, SetStateAction } from "react";
import { Check, ChevronLeft, ChevronRight, FileText, Mail, Paperclip, Send, UsersRound } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { getEmailTemplates, getSubscribers } from "@/api/emailMarketing";
import { useFeedback } from "@/components/ui/feedbackContext";
import { useEmailMarketingStore } from "@/stores/emailMarketingStore";
import type { EmailTemplateListItem, MarketingContentOption, SubscriberListItem } from "@/types/admin";

type RecipientMode = "all_active" | "selected" | "manual";
type WizardStep = "content" | "attachments" | "confirm";

const steps: Array<{ id: WizardStep; title: string; description: string; icon: typeof FileText }> = [
  { id: "content", title: "Chọn người nhận", description: "Tất cả, chọn người hoặc nhập email", icon: UsersRound },
  { id: "attachments", title: "Nội dung & đính kèm", description: "Template, tiêu đề, CTA, nội dung kèm", icon: Paperclip },
  { id: "confirm", title: "Xác nhận gửi", description: "Kiểm tra rồi xếp hàng", icon: Check },
];

const attachmentTypeLabels: Record<string, string> = { promo: "Khuyến mãi", blog: "Bài viết", product: "Sản phẩm" };
const splitEmails = (value: string) => [...new Set(value.split(/[,;\s]+/).map((x) => x.trim().toLowerCase()).filter(Boolean))];
const toAttachment = (option: MarketingContentOption) => ({ id: option.id, type: option.type, title: option.title, url: option.url, description: option.subtitle, code: option.type === "promo" ? option.title : null });
const stepIndex = (step: WizardStep) => steps.findIndex((item) => item.id === step);

export function MarketingSendPage() {
  const { contentOptions, fetchContentOptions, sendCampaign, loading, error } = useEmailMarketingStore();
  const { confirm, toast } = useFeedback();
  const [templates, setTemplates] = useState<EmailTemplateListItem[]>([]);
  const [subscribers, setSubscribers] = useState<SubscriberListItem[]>([]);
  const [step, setStep] = useState<WizardStep>("content");
  const [recipientMode, setRecipientMode] = useState<RecipientMode>("all_active");
  const [selectedSubscriberIds, setSelectedSubscriberIds] = useState<Set<string>>(new Set());
  const [manualEmails, setManualEmails] = useState("");
  const [templateKey, setTemplateKey] = useState("");
  const [subject, setSubject] = useState("");
  const [preheader, setPreheader] = useState("");
  const [intro, setIntro] = useState("Tin mới từ Ao Dai Nha Uyen dành riêng cho bạn.");
  const [bodyHtml, setBodyHtml] = useState("<p>Chúng tôi gửi bạn nội dung nổi bật trong tuần này.</p>");
  const [ctaLabel, setCtaLabel] = useState("Xem ngay");
  const [ctaUrl, setCtaUrl] = useState("");
  const [scheduledAt, setScheduledAt] = useState("");
  const [selectedAttachmentIds, setSelectedAttachmentIds] = useState<Set<string>>(new Set());

  useEffect(() => {
    fetchContentOptions().catch(() => {});
    getEmailTemplates("", false, 1, 100).then((response) => {
      const active = response.data.filter((item) => item.isActive && !item.isDeleted);
      setTemplates(active);
      setTemplateKey((active.find((item) => item.key === "marketing.promo") ?? active[0])?.key ?? "");
    }).catch(() => {});
    getSubscribers("", "active", false, 1, 100).then((response) => setSubscribers(response.data)).catch(() => {});
  }, [fetchContentOptions]);

  const selectedAttachments = useMemo(() => contentOptions.filter((item) => selectedAttachmentIds.has(item.id)).map(toAttachment), [contentOptions, selectedAttachmentIds]);
  const selectedTemplate = templates.find((item) => item.key === templateKey);
  const manualEmailList = splitEmails(manualEmails);
  const recipientCount = recipientMode === "all_active" ? "Tất cả subscriber active" : recipientMode === "selected" ? `${selectedSubscriberIds.size} người nhận đã chọn` : `${manualEmailList.length} email thủ công`;

  function toggle(setter: Dispatch<SetStateAction<Set<string>>>, id: string) {
    setter((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  function validateRecipientStep() {
    if (recipientMode === "selected" && selectedSubscriberIds.size === 0) return "Vui lòng chọn ít nhất một người nhận.";
    if (recipientMode === "manual") {
      if (!manualEmailList.length) return "Vui lòng nhập email người nhận.";
      const invalid = manualEmailList.find((email) => !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email));
      if (invalid) return `Email không hợp lệ: ${invalid}`;
    }
    return null;
  }

  function validateContentStep() {
    if (!templateKey) return "Vui lòng chọn template.";
    if (!subject.trim()) return "Vui lòng nhập tiêu đề email.";
    return null;
  }

  function goNext() {
    if (step === "content") {
      const message = validateRecipientStep();
      if (message) return toast(message, "error");
      return setStep("attachments");
    }
    const message = validateContentStep();
    if (message) return toast(message, "error");
    setStep("confirm");
  }

  function goBack() {
    if (step === "confirm") setStep("attachments");
    else if (step === "attachments") setStep("content");
  }

  async function handleSend() {
    const recipientMessage = validateRecipientStep();
    const contentMessage = validateContentStep();
    const message = recipientMessage ?? contentMessage;
    if (message) {
      toast(message, "error");
      setStep(recipientMessage ? "content" : "attachments");
      return;
    }
    const ok = await confirm({ title: "Gửi email marketing?", message: `Chiến dịch sẽ được xếp hàng cho ${recipientCount}.`, confirmText: "Gửi chiến dịch" });
    if (!ok) return;
    const result = await sendCampaign({ recipientMode, subscriberIds: recipientMode === "selected" ? [...selectedSubscriberIds] : undefined, manualEmails: recipientMode === "manual" ? manualEmailList : undefined, templateKey, subject: subject.trim(), preheader: preheader.trim() || null, intro: intro.trim() || null, bodyHtml: bodyHtml.trim() || null, ctaLabel: ctaLabel.trim() || null, ctaUrl: ctaUrl.trim() || null, attachments: selectedAttachments, scheduledAt: scheduledAt ? new Date(scheduledAt).toISOString() : null });
    toast(`Đã xếp hàng ${result.queued} email. Bỏ qua ${result.skipped}.`, "success");
    window.location.assign("http://localhost:5174/admin/marketing?tab=send");
  }

  const currentStepIndex = stepIndex(step);

  return (
    <div className="space-y-5">
      <div className="rounded-2xl border bg-white p-4 shadow-sm md:p-5">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-ink">Gửi email marketing</h1>
            <p className="mt-1 max-w-2xl text-sm text-gray-600">Tạo chiến dịch theo từng bước: chọn người nhận, nhập nội dung mail và đính kèm, xác nhận trước khi gửi.</p>
          </div>
          <div className="rounded-xl border bg-muted/30 px-4 py-3 text-sm">
            <div className="font-semibold text-burgundy">{recipientCount}</div>
            <div className="text-xs text-gray-500">{selectedAttachments.length} nội dung đính kèm</div>
          </div>
        </div>
        <div className="mt-5 grid gap-3 md:grid-cols-3" aria-label="Tiến trình gửi email">
          {steps.map((item, index) => {
            const Icon = item.icon;
            const isActive = item.id === step;
            const isDone = index < currentStepIndex;
            return (
              <button
                key={item.id}
                type="button"
                className={`rounded-xl border p-3 text-left transition ${isActive ? "border-primary bg-primary/5 shadow-sm" : isDone ? "border-green-200 bg-green-50" : "bg-white hover:bg-muted/50"}`}
                onClick={() => {
                  const message = item.id === "content" ? null : validateRecipientStep() ?? (item.id === "confirm" ? validateContentStep() : null);
                  if (message) {
                    toast(message, "error");
                    return;
                  }
                  setStep(item.id);
                }}
              >
                <div className="flex items-center gap-2">
                  <span className={`flex size-8 items-center justify-center rounded-full ${isDone ? "bg-green-600 text-white" : isActive ? "bg-primary text-primary-foreground" : "bg-gray-100 text-gray-600"}`}>{isDone ? <Check className="size-4" /> : <Icon className="size-4" />}</span>
                  <div><div className="text-sm font-semibold">{item.title}</div><div className="text-xs text-gray-500">{item.description}</div></div>
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {error && <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">{error}</div>}

      {step === "content" && (
        <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_360px]">
          <Card>
            <CardHeader><CardTitle className="flex items-center gap-2 text-base"><UsersRound className="size-4" /> Chọn người nhận</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-3 md:grid-cols-3">
                {[["all_active", "Tất cả active", "Gửi tới toàn bộ người đang nhận tin"], ["selected", "Chọn người", "Chọn từng subscriber active"], ["manual", "Nhập email", "Dán danh sách email thủ công"]].map(([id, title, description]) => (
                  <button key={id} type="button" className={`rounded-xl border p-4 text-left transition ${recipientMode === id ? "border-primary bg-primary/5" : "bg-white hover:bg-muted/50"}`} onClick={() => setRecipientMode(id as RecipientMode)}>
                    <div className="text-sm font-semibold">{title}</div><div className="mt-1 text-xs text-gray-500">{description}</div>
                  </button>
                ))}
              </div>
              {recipientMode === "selected" && (
                <div className="overflow-hidden rounded-xl border bg-white">
                  <div className="flex items-center justify-between border-b bg-muted/40 px-4 py-3 text-sm"><span className="font-medium">Subscriber active</span><span className="text-gray-500">Đã chọn {selectedSubscriberIds.size}</span></div>
                  <div className="max-h-72 overflow-auto divide-y">
                    {subscribers.map((subscriber) => (
                      <label key={subscriber.id} className="flex cursor-pointer items-center justify-between gap-3 px-4 py-3 text-sm hover:bg-muted/40"><span>{subscriber.email}</span><Checkbox checked={selectedSubscriberIds.has(subscriber.id)} onChange={() => toggle(setSelectedSubscriberIds, subscriber.id)} /></label>
                    ))}
                    {!subscribers.length && <div className="p-4 text-sm text-gray-500">Chưa có subscriber active.</div>}
                  </div>
                </div>
              )}
              {recipientMode === "manual" && <div className="space-y-2"><Label>Email thủ công</Label><Textarea className="min-h-32" value={manualEmails} onChange={(event) => setManualEmails(event.target.value)} placeholder="email1@example.com, email2@example.com" /><p className="text-xs text-gray-500">Phân tách bằng dấu phẩy, chấm phẩy hoặc xuống dòng.</p></div>}
            </CardContent>
          </Card>
          <Card className="h-fit xl:sticky xl:top-4">
            <CardHeader><CardTitle className="text-base">Tóm tắt người nhận</CardTitle></CardHeader>
            <CardContent className="space-y-3 text-sm"><SummaryRow label="Chế độ" value={recipientMode === "all_active" ? "Tất cả active" : recipientMode === "selected" ? "Chọn người" : "Nhập email"} /><SummaryRow label="Số lượng" value={recipientCount} /></CardContent>
          </Card>
        </div>
      )}

      {step === "attachments" && (
        <div className="space-y-4">
          <Card>
            <CardHeader><CardTitle className="flex items-center gap-2 text-base"><Mail className="size-4" /> Nội dung mail</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2"><Label>Template</Label><Select value={templateKey} onChange={(event) => setTemplateKey(event.target.value)}><option value="">Chọn template</option>{templates.map((template) => <option key={template.id} value={template.key}>{template.name} ({template.key})</option>)}</Select></div>
                <div className="space-y-2"><Label>Lịch gửi</Label><Input type="datetime-local" value={scheduledAt} onChange={(event) => setScheduledAt(event.target.value)} /></div>
              </div>
              <div className="space-y-2"><Label>Tiêu đề</Label><Input value={subject} onChange={(event) => setSubject(event.target.value)} placeholder="VD: Ưu đãi áo dài cuối tuần" /></div>
              <div className="space-y-2"><Label>Preheader</Label><Input value={preheader} onChange={(event) => setPreheader(event.target.value)} placeholder="Dòng mô tả ngắn trong inbox" /></div>
              <div className="space-y-2"><Label>Mở đầu</Label><Textarea className="min-h-24" value={intro} onChange={(event) => setIntro(event.target.value)} /></div>
              <div className="space-y-2"><Label>Nội dung HTML</Label><Textarea className="min-h-40 font-mono text-xs" value={bodyHtml} onChange={(event) => setBodyHtml(event.target.value)} /></div>
              <div className="grid gap-4 md:grid-cols-2"><div className="space-y-2"><Label>CTA label</Label><Input value={ctaLabel} onChange={(event) => setCtaLabel(event.target.value)} /></div><div className="space-y-2"><Label>CTA URL</Label><Input value={ctaUrl} onChange={(event) => setCtaUrl(event.target.value)} placeholder="https://..." /></div></div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle className="flex items-center gap-2 text-base"><Paperclip className="size-4" /> Chọn nội dung đính kèm</CardTitle></CardHeader>
            <CardContent className="space-y-4">
              <div className="rounded-xl border bg-muted/30 p-4 text-sm text-gray-600">Chọn nội dung muốn đưa vào email: mã khuyến mãi, bài viết hoặc sản phẩm nổi bật. Có thể bỏ trống nếu chỉ gửi thông báo.</div>
              <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                {contentOptions.map((option) => {
                  const checked = selectedAttachmentIds.has(option.id);
                  return (
                    <button key={`${option.type}-${option.id}`} type="button" className={`rounded-xl border p-4 text-left transition ${checked ? "border-primary bg-primary/5 shadow-sm" : "bg-white hover:bg-muted/50"}`} onClick={() => toggle(setSelectedAttachmentIds, option.id)}>
                      <div className="flex items-start justify-between gap-3"><div><div className="font-semibold text-sm">{option.title}</div><div className="mt-2 line-clamp-3 text-xs leading-5 text-gray-500">{option.subtitle ?? option.url ?? "Không có mô tả"}</div></div><Badge variant={checked ? "default" : "outline"}>{attachmentTypeLabels[option.type] ?? option.type}</Badge></div>
                      {option.badge && <div className="mt-3 text-xs font-medium text-wine">{option.badge}</div>}
                    </button>
                  );
                })}
                {!contentOptions.length && <div className="rounded-xl border p-4 text-sm text-gray-500">Chưa có khuyến mãi, bài viết hoặc sản phẩm active để đính kèm.</div>}
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      {step === "confirm" && (
        <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_360px]">
          <Card>
            <CardHeader><CardTitle className="text-base">Xác nhận chiến dịch</CardTitle></CardHeader>
            <CardContent className="space-y-5">
              <div className="grid gap-3 md:grid-cols-2"><ReviewTile label="Người nhận" value={recipientCount} /><ReviewTile label="Template" value={selectedTemplate?.name ?? templateKey} /><ReviewTile label="Tiêu đề" value={subject} /><ReviewTile label="Lịch gửi" value={scheduledAt ? new Date(scheduledAt).toLocaleString("vi-VN") : "Gửi ngay"} /></div>
              <div className="rounded-xl border bg-white p-4">
                <div className="text-sm font-semibold text-burgundy">Preview nội dung</div>
                <div className="mt-3 space-y-2 text-sm text-gray-700"><p><strong>Preheader:</strong> {preheader || "Không có"}</p><p><strong>Mở đầu:</strong> {intro || "Không có"}</p><div className="rounded-lg bg-muted/40 p-3 font-mono text-xs text-gray-600">{bodyHtml || "Không có nội dung HTML"}</div></div>
              </div>
              <div className="rounded-xl border bg-white p-4">
                <div className="mb-3 text-sm font-semibold text-burgundy">Đính kèm đã chọn ({selectedAttachments.length})</div>
                <div className="space-y-2">{selectedAttachments.map((item) => <div key={`${item.type}-${item.id}`} className="rounded-lg border bg-muted/20 p-3 text-sm"><div className="font-medium">{item.title}</div><div className="text-xs text-gray-500">{attachmentTypeLabels[item.type] ?? item.type}</div></div>)}{!selectedAttachments.length && <div className="text-sm text-gray-500">Không có nội dung đính kèm.</div>}</div>
              </div>
            </CardContent>
          </Card>
          <Card className="h-fit xl:sticky xl:top-4">
            <CardHeader><CardTitle className="text-base">Sẵn sàng gửi</CardTitle></CardHeader>
            <CardContent className="space-y-3 text-sm"><div className="rounded-xl bg-amber-50 p-3 text-amber-800">Email sẽ được đưa vào hàng đợi. Worker nền gửi qua SMTP theo cấu hình hiện tại.</div><Button className="w-full" onClick={() => void handleSend()} disabled={loading}><Send className="size-4" />{loading ? "Đang xếp hàng..." : "Gửi chiến dịch"}</Button></CardContent>
          </Card>
        </div>
      )}

      <div className="flex items-center justify-between rounded-2xl border bg-white p-4 shadow-sm">
        <Button variant="outline" onClick={goBack} disabled={step === "content" || loading}><ChevronLeft className="size-4" />Quay lại</Button>
        {step !== "confirm" ? <Button onClick={goNext} disabled={loading}>Tiếp tục<ChevronRight className="size-4" /></Button> : <Button onClick={() => void handleSend()} disabled={loading}><Send className="size-4" />{loading ? "Đang xếp hàng..." : "Gửi chiến dịch"}</Button>}
      </div>
    </div>
  );
}

function SummaryRow({ label, value }: { label: string; value: string }) {
  return <div className="flex justify-between gap-3 border-b pb-2 last:border-b-0 last:pb-0"><span className="text-gray-500">{label}</span><strong className="text-right text-gray-900">{value}</strong></div>;
}

function ReviewTile({ label, value }: { label: string; value: string }) {
  return <div className="rounded-xl border bg-muted/20 p-4"><div className="text-xs font-medium text-gray-500">{label}</div><div className="mt-1 text-sm font-semibold text-gray-900">{value || "Chưa có"}</div></div>;
}
