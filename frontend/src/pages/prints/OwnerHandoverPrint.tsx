import { useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Box, Divider, Stack, Typography } from '@mui/material';
import api from '../../api/client';
import { branding } from '../../config/branding';

type FoundItem = {
  id: string;
  category: string;
  otherCategoryText?: string | null;
  details: string;
  foundLocation?: string | null;
  foundAt?: string | null;
  ownerClaims?: Array<{
    ownerName: string;
    ownerAddress: string;
    ownerIdNumber?: string | null;
    releasedAt: string;
    releasedByUserId: string;
  }>;
};

async function fetchItem(id: string): Promise<FoundItem> {
  const res = await api.get<FoundItem>(`/api/items/${id}`);
  return res.data;
}

export default function OwnerHandoverPrint() {
  const { id } = useParams<{ id: string }>();
  const { data } = useQuery({ queryKey: ['item', id], queryFn: () => fetchItem(id!), enabled: !!id });

  const lastClaim = useMemo(() => {
    const claims = data?.ownerClaims ?? [];
    if (claims.length === 0) return null;
    return claims[claims.length - 1];
  }, [data]);

  return (
    <Box sx={{ p: 3, maxWidth: 900, mx: 'auto', fontSize: 14 }}>
      {/* Header with Logo */}
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 2 }}>
        <Stack direction="row" alignItems="center" spacing={2}>
          {/* Logo placeholder */}
          <Box sx={{ width: 80, height: 80, border: '1px dashed #999', display: 'flex', alignItems: 'center', justifyContent: 'center', overflow: 'hidden', bgcolor: '#fafafa' }}>
            {/* If logo exists in public/branding/logo.png it will load. If not, keep the dashed box. */}
            <img src={branding.logoUrl} alt="logo" style={{ maxWidth: '100%', maxHeight: '100%', display: 'block' }} onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none'; }} />
          </Box>
          <Box>
            <Typography variant="h6">{branding.organizationName}</Typography>
            <Typography variant="subtitle2">Átadás-átvételi jegyzőkönyv</Typography>
          </Box>
        </Stack>
        <Box>
          <Typography variant="body2">Dátum: {lastClaim ? new Date(lastClaim.releasedAt).toLocaleString('hu-HU') : (data?.foundAt ? new Date(data.foundAt).toLocaleDateString('hu-HU') : '')}</Typography>
          <Typography variant="body2">Ügyiratszám: {data?.id}</Typography>
        </Box>
      </Stack>

      <Divider sx={{ mb: 2 }} />

      {/* Item section */}
      <Box sx={{ mb: 2 }}>
        <Typography variant="subtitle1" gutterBottom>Tárgy adatai</Typography>
        <Typography variant="body2">Kategória: {data?.category}{data?.category === 'Egyéb' && data?.otherCategoryText ? ` - ${data.otherCategoryText}` : ''}</Typography>
        <Typography variant="body2">Megtalálás helye: {data?.foundLocation ?? '-'}</Typography>
        <Typography variant="body2">Megtalálás ideje: {data?.foundAt ? new Date(data.foundAt).toLocaleString('hu-HU') : '-'}</Typography>
        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>Leírás: {data?.details}</Typography>
      </Box>

      {/* Owner section */}
      <Box sx={{ mb: 2 }}>
        <Typography variant="subtitle1" gutterBottom>Tulajdonos adatai</Typography>
        <Typography variant="body2">Név: {lastClaim?.ownerName ?? '________________________'}</Typography>
        <Typography variant="body2">Lakcím: {lastClaim?.ownerAddress ?? '________________________'}</Typography>
        <Typography variant="body2">Igazolványszám: {lastClaim?.ownerIdNumber ?? '________________________'}</Typography>
      </Box>

      <Divider sx={{ my: 3 }} />

      {/* Signatures */}
      <Stack direction="row" justifyContent="space-between" sx={{ mt: 6 }}>
        <Box sx={{ textAlign: 'center', width: '45%' }}>
          <Box sx={{ borderTop: '1px solid #000', pt: 1 }}>Átadó</Box>
        </Box>
        <Box sx={{ textAlign: 'center', width: '45%' }}>
          <Box sx={{ borderTop: '1px solid #000', pt: 1 }}>Tulajdonos</Box>
        </Box>
      </Stack>

      {/* Auto print when opened */}
      <script dangerouslySetInnerHTML={{ __html: 'window.addEventListener("load", () => setTimeout(() => window.print(), 300));' }} />
    </Box>
  );
}
