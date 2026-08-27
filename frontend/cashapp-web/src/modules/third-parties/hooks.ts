import { useQuery } from '@tanstack/react-query';
import { thirdPartiesApi } from '@/api/thirdPartiesApi';

export function useThirdParties() {
  return useQuery({
    queryKey: ['third-parties'],
    queryFn: () => thirdPartiesApi.list(),
    staleTime: 60_000
  });
}
