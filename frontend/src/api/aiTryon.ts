import { request } from './client';

export interface AiTryOnResponse {
  resultImageUrl: string;
  mimeType: string;
}

interface SubmitAiTryOnParams {
  personImage: File;
  garmentImage: File;
  garmentId: string;
  accessoryImages?: Array<{
    id: string;
    file: File;
  }>;
}

export function submitAiTryOn({
  personImage,
  garmentImage,
  garmentId,
  accessoryImages = [],
}: SubmitAiTryOnParams): Promise<AiTryOnResponse> {
  const formData = new FormData();
  formData.append('personImage', personImage);
  formData.append('garmentImage', garmentImage);
  formData.append('garmentId', garmentId);

  accessoryImages.forEach((accessory) => {
    formData.append('accessoryImages', accessory.file);
    formData.append('accessoryIds', accessory.id);
  });

  return request<AiTryOnResponse>('/api/v1/ai-tryon', {
    method: 'POST',
    body: formData,
  });
}
