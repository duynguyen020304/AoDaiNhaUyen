/* eslint-disable react-refresh/only-export-components */
import React from "react";
import { render } from "@react-email/render";
import {
  Body,
  Button,
  Container,
  Head,
  Heading,
  Html,
  Img,
  Preview,
  Section,
  Text,
} from "@react-email/components";

export type EmailTemplateType =
  | "marketing.promo"
  | "marketing.newsletter"
  | "subscriber.welcome"
  | "order.confirmation";
export type EmailTemplateConfig = {
  heading: string;
  intro: string;
  body: string;
  ctaText: string;
  ctaUrl: string;
  footerNote?: string;
  imageUrl?: string;
  logoUrl?: string;
  orderCode?: string;
};
export type TemplateField = {
  key: keyof EmailTemplateConfig;
  label: string;
  multiline?: boolean;
  required?: boolean;
  placeholder?: string;
};
type Def = {
  type: EmailTemplateType;
  label: string;
  description: string;
  defaultKey: string;
  defaultName: string;
  defaultSubject: string;
  defaultPreheader: string;
  defaultConfig: EmailTemplateConfig;
  fields: TemplateField[];
};

const fields: TemplateField[] = [
  { key: "heading", label: "Tiêu đề chính", required: true },
  { key: "intro", label: "Mở đầu", multiline: true, required: true },
  { key: "body", label: "Nội dung", multiline: true, required: true },
  { key: "ctaText", label: "CTA text", required: true },
  {
    key: "ctaUrl",
    label: "CTA URL",
    required: true,
    placeholder: "https://...",
  },
  { key: "footerNote", label: "Ghi chú cuối email", multiline: true },
  { key: "imageUrl", label: "Ảnh minh họa", placeholder: "https://..." },
  { key: "logoUrl", label: "Logo URL", placeholder: "https://..." },
];

export const emailTemplateRegistry: Record<EmailTemplateType, Def> = {
  "marketing.promo": {
    type: "marketing.promo",
    label: "Email khuyến mãi",
    description: "Ưu đãi chính, CTA rõ.",
    defaultKey: "marketing.promo",
    defaultName: "Khuyến mãi",
    defaultSubject: "Ưu đãi áo dài dành riêng cho bạn",
    defaultPreheader: "Khám phá ưu đãi mới nhất từ Áo Dài Nhã Uyên",
    defaultConfig: {
      heading: "Ưu đãi áo dài cuối tuần",
      intro: "Một lựa chọn tinh tế cho những khoảnh khắc đặc biệt.",
      body: "Nhận ưu đãi cho các thiết kế áo dài mới, chất liệu mềm mại và phom dáng tôn nét Việt.",
      ctaText: "Xem ưu đãi",
      ctaUrl: "https://aodainhauyen.io.vn/products",
      footerNote: "Ưu đãi có thể kết thúc sớm khi hết số lượng.",
    },
    fields,
  },
  "marketing.newsletter": {
    type: "marketing.newsletter",
    label: "Newsletter",
    description: "Bản tin thương hiệu.",
    defaultKey: "marketing.newsletter",
    defaultName: "Newsletter",
    defaultSubject: "Bản tin Áo Dài Nhã Uyên",
    defaultPreheader: "Cảm hứng mặc đẹp và câu chuyện áo dài mới nhất",
    defaultConfig: {
      heading: "Cảm hứng áo dài trong tuần",
      intro: "Những gợi ý phối áo dài, câu chuyện chất liệu và thiết kế mới.",
      body: "Nhã Uyên chọn lọc các thiết kế trang nhã cho sự kiện gia đình, lễ hội và khoảnh khắc thường ngày.",
      ctaText: "Đọc thêm",
      ctaUrl: "https://aodainhauyen.io.vn/blog",
    },
    fields,
  },
  "subscriber.welcome": {
    type: "subscriber.welcome",
    label: "Chào mừng subscriber",
    description: "Email sau đăng ký.",
    defaultKey: "subscriber.welcome",
    defaultName: "Chào mừng đăng ký nhận tin",
    defaultSubject: "Chào mừng bạn đến với Áo Dài Nhã Uyên",
    defaultPreheader: "Cảm ơn bạn đã gia nhập cộng đồng yêu áo dài",
    defaultConfig: {
      heading: "Chào mừng bạn đến với Áo Dài Nhã Uyên",
      intro: "Cảm ơn bạn đã đăng ký nhận tin.",
      body: "Bạn sẽ nhận cảm hứng mặc đẹp, mẹo chăm sóc áo dài và ưu đãi riêng.",
      ctaText: "Khám phá bộ sưu tập",
      ctaUrl: "https://aodainhauyen.io.vn/products",
    },
    fields,
  },
  "order.confirmation": {
    type: "order.confirmation",
    label: "Xác nhận đơn hàng",
    description: "Email transactional.",
    defaultKey: "order.confirmation",
    defaultName: "Xác nhận đơn hàng",
    defaultSubject: "Nhã Uyên đã nhận đơn hàng của bạn",
    defaultPreheader: "Thông tin đơn hàng và bước xử lý tiếp theo",
    defaultConfig: {
      heading: "Xác nhận đơn hàng",
      intro: "Cảm ơn bạn đã tin chọn Áo Dài Nhã Uyên.",
      body: "Chúng tôi đã nhận được đơn hàng và sẽ liên hệ khi đơn được xử lý.",
      ctaText: "Xem đơn hàng",
      ctaUrl: "https://aodainhauyen.io.vn/account/orders",
      orderCode: "ADNU-2026-0001",
    },
    fields: [...fields, { key: "orderCode", label: "Mã đơn mẫu" }],
  },
};

const templateKeyMap: Record<string, EmailTemplateType> = {
  "marketing.promo": "marketing.promo",
  "marketing.newsletter": "marketing.newsletter",
  "marketing.welcome": "subscriber.welcome",
  "subscriber.welcome": "subscriber.welcome",
  "order.confirmation": "order.confirmation",
};

export function resolveEmailTemplateType(
  key: string,
  templateType?: string | null
): EmailTemplateType | null {
  if (templateType && isEmailTemplateType(templateType)) return templateType;
  return templateKeyMap[key] ?? null;
}
export function isEmailTemplateType(value: string): value is EmailTemplateType {
  return value in emailTemplateRegistry;
}
export function normalizeTemplateConfig(
  type: string,
  json?: string | null
): EmailTemplateConfig {
  const safe = isEmailTemplateType(type) ? type : "marketing.promo";
  try {
    return {
      ...emailTemplateRegistry[safe].defaultConfig,
      ...(json ? JSON.parse(json) : {}),
    };
  } catch {
    return { ...emailTemplateRegistry[safe].defaultConfig };
  }
}
export async function renderEmailTemplateHtml(args: {
  templateType: EmailTemplateType;
  subject: string;
  preheader?: string | null;
  config: EmailTemplateConfig;
}) {
  return render(<TemplateEmail {...args} />, { pretty: true });
}

function TemplateEmail({
  templateType,
  subject,
  preheader,
  config,
}: {
  templateType: EmailTemplateType;
  subject: string;
  preheader?: string | null;
  config: EmailTemplateConfig;
}) {
  return (
    <Html lang="vi">
      <Head />
      <Preview>{preheader || subject}</Preview>
      <Body style={s.body}>
        <Container style={s.container}>
          <Section style={s.header}>
            {config.logoUrl ? (
              <Img
                src={config.logoUrl}
                alt="Áo Dài Nhã Uyên"
                width="160"
                style={s.logo}
              />
            ) : (
              <Text style={s.brand}>Áo Dài Nhã Uyên</Text>
            )}
          </Section>
          {config.imageUrl ? (
            <Img
              src={config.imageUrl}
              alt="Email visual"
              width="640"
              style={s.heroImage}
            />
          ) : null}
          <Section style={s.content}>
            <Heading as="h1" style={s.heading}>
              {config.heading}
            </Heading>
            {templateType === "order.confirmation" && config.orderCode ? (
              <Text style={s.orderCode}>{config.orderCode}</Text>
            ) : null}
            <Text style={s.text}>{config.intro}</Text>
            <Text style={s.text}>{config.body}</Text>
            <Button href={config.ctaUrl} style={s.button}>
              {config.ctaText}
            </Button>
            {config.footerNote ? (
              <Text style={s.note}>{config.footerNote}</Text>
            ) : null}
          </Section>
          <Section style={s.footer}>
            <Text style={s.footerText}>
              © Áo Dài Nhã Uyên. Email được gửi theo đăng ký/đơn hàng của bạn.
            </Text>
          </Section>
        </Container>
      </Body>
    </Html>
  );
}

const s = {
  body: {
    margin: 0,
    backgroundColor: "#f6f0ec",
    fontFamily: "Arial, Helvetica, sans-serif",
    color: "#2f1f1a",
  },
  container: {
    maxWidth: "640px",
    margin: "32px auto",
    backgroundColor: "#fff",
    border: "1px solid #ead7d7",
    borderRadius: "22px",
    overflow: "hidden",
  },
  header: { padding: "28px 32px 18px", backgroundColor: "#fffaf7" },
  brand: { margin: 0, color: "#7f1d1d", fontSize: "18px", fontWeight: "700" },
  logo: { display: "block", maxWidth: "160px" },
  heroImage: { display: "block", width: "100%", height: "auto" },
  content: { padding: "8px 32px 32px" },
  heading: {
    margin: "0 0 16px",
    color: "#7f1d1d",
    fontSize: "28px",
    lineHeight: "1.25",
  },
  orderCode: {
    margin: "0 0 16px",
    color: "#3f2a1f",
    fontSize: "16px",
    fontWeight: "700",
  },
  text: {
    margin: "0 0 16px",
    color: "#4b342a",
    fontSize: "16px",
    lineHeight: "1.7",
  },
  button: {
    display: "inline-block",
    marginTop: "12px",
    backgroundColor: "#7f1d1d",
    color: "#fff",
    padding: "13px 22px",
    borderRadius: "999px",
    textDecoration: "none",
    fontWeight: "700",
  },
  note: {
    margin: "20px 0 0",
    color: "#8a6f58",
    fontSize: "13px",
    lineHeight: "1.6",
  },
  footer: { padding: "22px 32px", backgroundColor: "#2f1f1a" },
  footerText: {
    margin: 0,
    color: "#f8eee8",
    fontSize: "12px",
    lineHeight: "1.6",
  },
} satisfies Record<string, React.CSSProperties>;
