import { useQuery } from '@tanstack/react-query';
import { getBlogCategories, getBlogPost, getBlogPosts, getBlogTags, getRelatedPosts } from '../../api/blog';
import { queryKeys } from '../../lib/queryKeys';
import type { BlogListParams } from '../../types/blog';

export function useBlogList(params: BlogListParams = {}) {
  return useQuery({ queryKey: queryKeys.blog.list(params), queryFn: () => getBlogPosts(params), staleTime: 60_000, gcTime: 10 * 60_000 });
}
export function useBlogDetail(slug: string | undefined) {
  return useQuery({ queryKey: queryKeys.blog.detail(slug ?? ''), queryFn: () => getBlogPost(slug ?? ''), enabled: Boolean(slug), staleTime: 5 * 60_000, gcTime: 30 * 60_000 });
}
export function useRelatedPosts(slug: string | undefined) {
  return useQuery({ queryKey: queryKeys.blog.related(slug ?? ''), queryFn: () => getRelatedPosts(slug ?? ''), enabled: Boolean(slug), staleTime: 5 * 60_000, gcTime: 30 * 60_000 });
}
export function useBlogCategories() {
  return useQuery({ queryKey: queryKeys.blog.categories, queryFn: getBlogCategories, staleTime: 30 * 60_000, gcTime: 2 * 60 * 60_000 });
}
export function useBlogTags() {
  return useQuery({ queryKey: queryKeys.blog.tags, queryFn: getBlogTags, staleTime: 10 * 60_000, gcTime: 60 * 60_000 });
}
