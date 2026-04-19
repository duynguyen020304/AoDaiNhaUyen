import { useEffect, useMemo, useRef, useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
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

const easeOutQuart = [0.22, 1, 0.36, 1] as const;

export default function ChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [showThreads, setShowThreads] = useState(false);
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

  const handleCreateThread = async () => {
    setLoading(true);
    try {
      const thread = await createChatThread();
      setActiveThread(thread);
      setThreads((current) => [
        { id: thread.id, title: thread.title, preview: thread.messages.at(-1)?.content ?? null, status: thread.status, updatedAt: thread.updatedAt },
        ...current,
      ]);
      setShowThreads(false);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectThread = async (threadId: number) => {
    setLoading(true);
    try {
      setActiveThread(await getChatThread(threadId));
      setShowThreads(false);
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
    <div className={styles.widgetRoot}>
      <AnimatePresence>
        {isOpen && (
          <motion.div
            className={styles.widgetWindow}
            initial={{ opacity: 0, scale: 0.85, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.85, y: 20 }}
            transition={{ duration: 0.32, ease: easeOutQuart }}
          >
            <div className={styles.conversation}>
              <header className={styles.conversationHeader}>
                <div className={styles.headerLeft}>
                  <button type="button" className={styles.menuButton} onClick={() => setShowThreads(true)}>
                    ☰
                  </button>
                  <div className={styles.conversationTitle}>{activeThread?.title ?? 'Stylist Chat'}</div>
                </div>
                <button type="button" className={styles.closeButton} onClick={() => setIsOpen(false)}>
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
                <div className={styles.composerCard}>
                  <textarea
                    className={styles.composerInput}
                    value={draft}
                    onChange={(event) => setDraft(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' && !event.shiftKey) {
                        event.preventDefault();
                        handleSend();
                      }
                    }}
                    placeholder="Gợi ý áo dài đi dạy màu nhã..."
                  />
                  <div className={styles.composerActions}>
                    <button type="button" className={styles.attachButton} onClick={() => fileInputRef.current?.click()}>
                      📎
                    </button>
                    {latestTryOnPayload?.structuredPayload?.canTryOn ? (
                      <button type="button" className={styles.tryOnButton} disabled={submitting} onClick={handleTryOn}>
                        Thử ngay
                      </button>
                    ) : null}
                    <button
                      type="button"
                      className={styles.sendButton}
                      disabled={submitting}
                      onClick={handleSend}
                      aria-label="Gửi tin nhắn"
                    >
                      <svg viewBox="0 0 24 24" aria-hidden="true" className={styles.sendIcon}>
                        <path d="M4.5 11.25 18.75 4.5l-3.375 14.25-4.5-5.25-6.375-2.25Z" />
                        <path d="m10.875 13.5 7.875-9" />
                      </svg>
                    </button>
                  </div>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/png,image/jpeg,image/webp"
                    hidden
                    multiple
                    onChange={(event) => setPendingFiles(Array.from(event.target.files ?? []))}
                  />
                </div>
              </div>

              <AnimatePresence>
                {showThreads && (
                  <motion.div
                    className={styles.threadDrawer}
                    initial={{ x: '-100%' }}
                    animate={{ x: 0 }}
                    exit={{ x: '-100%' }}
                    transition={{ duration: 0.28, ease: easeOutQuart }}
                  >
                    <div className={styles.drawerHeader}>
                      <div className={styles.drawerTitle}>Cuộc trò chuyện</div>
                      <button type="button" className={styles.newThreadButton} onClick={handleCreateThread}>
                        + Mới
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
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      <AnimatePresence>
        {!isOpen && (
          <motion.button
            type="button"
            className={styles.chatBubble}
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.8 }}
            transition={{ duration: 0.2 }}
            whileHover={{ scale: 1.04 }}
            whileTap={{ scale: 0.96 }}
            onClick={() => setIsOpen(true)}
          >
            <span className={styles.chatBubbleIcon}>✦</span>
            <span className={styles.chatBubbleLabel}>Tư vấn AI</span>
          </motion.button>
        )}
      </AnimatePresence>
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
                <div className={styles.productInfo}>
                  <div className={styles.productName}>{product.name}</div>
                  <div className={styles.productMeta}>
                    {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(product.salePrice ?? product.price)}
                  </div>
                  <div className={styles.productRationale}>{product.rationale}</div>
                </div>
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
