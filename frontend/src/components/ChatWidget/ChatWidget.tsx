import { useEffect, useMemo, useRef, useState } from 'react';
import {
  createChatThread,
  executeChatTryOn,
  getChatThread,
  listChatThreads,
  sendChatMessage,
  type ChatMessage,
  type ChatThreadDetail,
  type ChatThreadSummary,
} from '../../api/chat';
import { resolveAssetUrl } from '../../api/client';
import styles from './ChatWidget.module.css';

interface ChatWidgetProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function ChatWidget({ isOpen, onClose }: ChatWidgetProps) {
  const [threads, setThreads] = useState<ChatThreadSummary[]>([]);
  const [activeThread, setActiveThread] = useState<ChatThreadDetail | null>(null);
  const [draft, setDraft] = useState('');
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const endRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    let ignore = false;

    async function bootstrap() {
      setLoading(true);
      try {
        const summaries = await listChatThreads();
        if (ignore) {
          return;
        }

        setThreads(summaries);
        if (summaries.length > 0) {
          const thread = await getChatThread(summaries[0].id);
          if (!ignore) {
            setActiveThread(thread);
          }
        } else {
          const thread = await createChatThread();
          if (!ignore) {
            setActiveThread(thread);
            setThreads([{ id: thread.id, title: thread.title, preview: thread.messages.at(-1)?.content ?? null, status: thread.status, updatedAt: thread.updatedAt }]);
          }
        }
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    }

    bootstrap();

    return () => {
      ignore = true;
    };
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) {
      endRef.current?.scrollIntoView({ behavior: 'smooth' });
    }
  }, [activeThread, isOpen]);

  const latestTryOnPayload = useMemo(() => {
    return [...(activeThread?.messages ?? [])]
      .reverse()
      .find((message) => message.structuredPayload?.canTryOn);
  }, [activeThread]);

  if (!isOpen) {
    return null;
  }

  const handleCreateThread = async () => {
    setLoading(true);
    try {
      const thread = await createChatThread();
      setActiveThread(thread);
      setThreads((current) => [
        { id: thread.id, title: thread.title, preview: thread.messages.at(-1)?.content ?? null, status: thread.status, updatedAt: thread.updatedAt },
        ...current,
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectThread = async (threadId: number) => {
    setLoading(true);
    try {
      setActiveThread(await getChatThread(threadId));
    } finally {
      setLoading(false);
    }
  };

  const handleSend = async () => {
    if (!activeThread || (!draft.trim() && pendingFiles.length === 0)) {
      return;
    }

    setSubmitting(true);
    try {
      const thread = await sendChatMessage(activeThread.id, draft.trim(), pendingFiles);
      setActiveThread(thread);
      setThreads((current) => updateThreadSummary(current, thread));
      setDraft('');
      setPendingFiles([]);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } finally {
      setSubmitting(false);
    }
  };

  const handleTryOn = async () => {
    if (!activeThread || !latestTryOnPayload?.structuredPayload?.selectedGarmentProductId) {
      return;
    }

    setSubmitting(true);
    try {
      const message = await executeChatTryOn(
        activeThread.id,
        latestTryOnPayload.structuredPayload.selectedGarmentProductId,
        latestTryOnPayload.structuredPayload.selectedAccessoryProductIds,
      );
      setActiveThread((current) => current
        ? { ...current, messages: [...current.messages, message], updatedAt: message.createdAt }
        : current);
      setThreads((current) => current.map((thread) => thread.id === activeThread.id
        ? { ...thread, preview: message.content, updatedAt: message.createdAt }
        : thread));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className={styles.overlay}>
      <section className={styles.panel}>
        <aside className={styles.sidebar}>
          <div className={styles.sidebarTop}>
            <div className={styles.sidebarTitle}>Stylist Chat</div>
            <button type="button" className={styles.newThreadButton} onClick={handleCreateThread}>
              Cuộc trò chuyện mới
            </button>
          </div>
          {loading && threads.length === 0 ? <div className={styles.loadingState}>Đang tải...</div> : null}
          <div className={styles.threadList}>
            {threads.map((thread) => (
              <button
                key={thread.id}
                type="button"
                className={`${styles.threadButton} ${activeThread?.id === thread.id ? styles.threadButtonActive : ''}`}
                onClick={() => handleSelectThread(thread.id)}
              >
                <span className={styles.threadTitle}>{thread.title}</span>
                <span className={styles.threadPreview}>{thread.preview ?? 'Chưa có tin nhắn'}</span>
              </button>
            ))}
          </div>
        </aside>

        <div className={styles.conversation}>
          <header className={styles.conversationHeader}>
            <div className={styles.conversationTitle}>{activeThread?.title ?? 'Stylist Chat'}</div>
            <button type="button" className={styles.closeButton} onClick={onClose}>
              ×
            </button>
          </header>

          <div className={styles.messages}>
            {activeThread?.messages.map((message) => (
              <MessageCard key={message.id} message={message} />
            ))}
            <div ref={endRef} />
          </div>

          <div className={styles.composer}>
            {pendingFiles.length > 0 ? (
              <div className={styles.pendingFiles}>
                {pendingFiles.map((file) => (
                  <span key={`${file.name}-${file.size}`} className={styles.pendingFile}>{file.name}</span>
                ))}
              </div>
            ) : null}
            <div className={styles.composerControls}>
              <textarea
                className={styles.composerInput}
                value={draft}
                onChange={(event) => setDraft(event.target.value)}
                placeholder="Ví dụ: Gợi ý cho mình một bộ áo dài đi dạy màu nhã, ngân sách dưới 3 triệu."
              />
              <input
                ref={fileInputRef}
                type="file"
                accept="image/png,image/jpeg,image/webp"
                hidden
                multiple
                onChange={(event) => setPendingFiles(Array.from(event.target.files ?? []))}
              />
              <button type="button" className={styles.attachButton} onClick={() => fileInputRef.current?.click()}>
                Thêm ảnh
              </button>
              {latestTryOnPayload?.structuredPayload?.canTryOn ? (
                <button type="button" className={styles.tryOnButton} disabled={submitting} onClick={handleTryOn}>
                  Thử ngay
                </button>
              ) : null}
              <button type="button" className={styles.sendButton} disabled={submitting} onClick={handleSend}>
                Gửi
              </button>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

function MessageCard({ message }: { message: ChatMessage }) {
  const isUser = message.role === 'user';

  return (
    <div className={`${styles.messageRow} ${isUser ? styles.messageRowUser : ''}`}>
      <div className={styles.messageBubble}>
        <div>{message.content}</div>
        {message.attachments.map((attachment) => {
          const imageUrl = resolveAssetUrl(attachment.fileUrl) ?? attachment.fileUrl;
          return attachment.mimeType.startsWith('image/')
            ? <img key={attachment.id} className={styles.attachmentImage} src={imageUrl} alt={attachment.originalFileName ?? 'Attachment'} />
            : null;
        })}
        {message.structuredPayload?.products.length ? (
          <div className={styles.productGrid}>
            {message.structuredPayload.products.map((product) => (
              <div key={product.productId} className={styles.productCard}>
                {product.primaryImageUrl ? (
                  <img
                    className={styles.productImage}
                    src={resolveAssetUrl(product.primaryImageUrl) ?? product.primaryImageUrl}
                    alt={product.name}
                  />
                ) : null}
                <div className={styles.productName}>{product.name}</div>
                <div className={styles.productMeta}>
                  {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(product.salePrice ?? product.price)}
                </div>
                <div className={styles.productRationale}>{product.rationale}</div>
              </div>
            ))}
          </div>
        ) : null}
      </div>
    </div>
  );
}

function updateThreadSummary(current: ChatThreadSummary[], thread: ChatThreadDetail): ChatThreadSummary[] {
  const next = [
    {
      id: thread.id,
      title: thread.title,
      preview: thread.messages.at(-1)?.content ?? null,
      status: thread.status,
      updatedAt: thread.updatedAt,
    },
    ...current.filter((item) => item.id !== thread.id),
  ];

  return next;
}
