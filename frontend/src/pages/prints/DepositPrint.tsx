import { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Box, CircularProgress, Typography } from '@mui/material';
import api from '../../api/client';

export default function DepositPrint() {
  const navigate = useNavigate();
  const { number } = useParams<{ number: string }>();

  useEffect(() => {
    (async () => {
      if (!number) return;
      try {
        // Lookup deposit by number to get its ID
        const depRes = await api.get<{ id: string }>(`/api/deposits/by-number/${encodeURIComponent(number)}`);
        const id = (depRes.data as any)?.id;
        if (!id) throw new Error('Deposit not found');

        // Set tab title: "<leadási szám>_yyyyMMdd" and build same filename for URL path
        const now = new Date();
        const yyyy = String(now.getFullYear());
        const mm = String(now.getMonth() + 1).padStart(2, '0');
        const dd = String(now.getDate()).padStart(2, '0');
        const fileBase = `${number}_${yyyy}${mm}${dd}`;
        document.title = fileBase;

        // Navigate to route that contains filename in URL so browsers use it when saving
        const base = (api.defaults as any).baseURL || '';
        const url = `${base.replace(/\/$/, '')}/api/deposits/${id}/print/${encodeURIComponent(fileBase)}.pdf?t=${Date.now()}`;
        window.location.href = url;
      } catch (e) {
        // On error, show a brief message then navigate back
        console.error('Failed to load deposit print PDF', e);
        setTimeout(() => navigate(-1), 1500);
      }
    })();
    return () => {};
  }, [number, navigate]);

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>Leadási jegyzőkönyv betöltése…</Typography>
      <CircularProgress />
    </Box>
  );
}
