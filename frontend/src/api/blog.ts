import { request, requestPaginated } from './client';
import type { BlogListParams, BlogPost, BlogPostListItem } from '../types/blog';

function qs(params: BlogListParams = {}) {
  const sp = new URLSearchParams();
  if (params.tag) sp.set('tag', params.tag);
  if (params.search) sp.set('search', params.search);
  if (params.page) sp.set('page', String(params.page));
  if (params.pageSize) sp.set('pageSize', String(params.pageSize));
  const value = sp.toString();
  return value ? `?${value}` : '';
}
export async function getBlogPosts(params: BlogListParams = {}) { return requestPaginated<BlogPostListItem[]>(`/api/v1/blog${qs(params)}`); }
export async function getBlogPost(slug: string) { return request<BlogPost>(`/api/v1/blog/${slug}`); }
export async function getRelatedPosts(slug: string, count = 3) { return request<BlogPostListItem[]>(`/api/v1/blog/${slug}/related?count=${count}`); }
export async function getBlogTags() { return request<string[]>('/api/v1/blog/tags'); }
