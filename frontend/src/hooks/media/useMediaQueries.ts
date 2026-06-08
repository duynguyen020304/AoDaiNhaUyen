import { useQuery } from '@tanstack/react-query';
import { getMyImages } from '../../api/media';
import { queryKeys } from '../../lib/queryKeys';

export function useMyImagesQuery(page: number, pageSize: number, sourceType?: string) {
  return useQuery({
    queryKey: [...queryKeys.media.myImages, { page, pageSize, sourceType: sourceType ?? null }] as const,
    queryFn: () => getMyImages(page, pageSize, sourceType),
    staleTime: 5 * 60_000,
  });
}
