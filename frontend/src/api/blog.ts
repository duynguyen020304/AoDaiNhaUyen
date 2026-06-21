import { request, requestPaginated } from './client';
import type { BlogBlock, BlogCategory, BlogListParams, BlogPost, BlogPostListItem } from '../types/blog';

function qs(params: BlogListParams = {}) {
  const sp = new URLSearchParams();
  if (params.tag) sp.set('tag', params.tag);
  if (params.category) sp.set('category', params.category);
  if (params.search) sp.set('search', params.search);
  if (params.page) sp.set('page', String(params.page));
  if (params.pageSize) sp.set('pageSize', String(params.pageSize));
  const value = sp.toString();
  return value ? `?${value}` : '';
}
interface RawBlogBlock {
  type: string;
  data?: Record<string, unknown>;
  src?: unknown;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === 'object' && !Array.isArray(value);
}

function isAbsoluteUrl(value: unknown): value is string {
  return typeof value === 'string' && /^https?:\/\//i.test(value);
}

function normalizeBlogBlocks(blocks: RawBlogBlock[]): BlogBlock[] {
  if (!Array.isArray(blocks)) return [];
  return blocks.map((block) => {
    if (block && isRecord(block.data)) {
      const nestedData = isRecord(block.data.data) ? block.data.data : undefined;
      const normalized = {
        ...nestedData,
        ...block,
        ...block.data,
      };

      if (isAbsoluteUrl(block.src)) normalized.src = block.src;
      if (isAbsoluteUrl(block.data.src)) normalized.src = block.data.src;
      delete normalized.data;

      return normalized as unknown as BlogBlock;
    }
    return block as unknown as BlogBlock;
  });
}

export async function getBlogPosts(params: BlogListParams = {}) { return requestPaginated<BlogPostListItem[]>(`/api/v1/blog${qs(params)}`); }

export async function getBlogPost(slug: string) {
  const post = await request<BlogPost>(`/api/v1/blog/${slug}`);
  if (post && post.content) {
    post.content = normalizeBlogBlocks(post.content);
  }
  return post;
}
export async function getRelatedPosts(slug: string, count = 3) { return request<BlogPostListItem[]>(`/api/v1/blog/${slug}/related?count=${count}`); }
export async function getBlogCategories() { return request<BlogCategory[]>('/api/v1/blog/categories'); }
