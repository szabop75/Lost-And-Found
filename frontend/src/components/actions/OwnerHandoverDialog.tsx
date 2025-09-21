import { useRef, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../../api/client';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TextField,
} from '@mui/material';

export type ItemLite = {
  id: string;
};

type Props = {
  open: boolean;
  onClose: () => void;
  item: ItemLite | null;
};

export default function OwnerHandoverDialog({ open, onClose, item }: Props) {
  const [ownerName, setOwnerName] = useState('');
  const [ownerAddress, setOwnerAddress] = useState('');
  const [ownerIdNumber, setOwnerIdNumber] = useState('');
  const [handoverAt, setHandoverAt] = useState<string>('');
  const qc = useQueryClient();

  const popupRef = useRef<Window | null>(null);
  const mutateSave = useMutation({
    mutationFn: async () => {
      if (!item) return;
      await api.post(`/api/items/${item.id}/handover-owner`, {
        ownerName,
        ownerAddress,
        ownerIdNumber,
        handoverAt,
      });
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['items'] });
      onClose();
      if (item) {
        try {
          const base = (import.meta as any).env?.VITE_API_BASE_URL ?? '';
          const token = localStorage.getItem('accessToken');
          const resp = await fetch(`${base}/api/items/${item.id}/print/owner-handover`, {
            method: 'GET',
            headers: token ? { Authorization: `Bearer ${token}` } : undefined,
          });
          if (!resp.ok) throw new Error(`PDF fetch failed: ${resp.status}`);
          const blob = await resp.blob();
          const pdfUrl = URL.createObjectURL(blob);
          if (popupRef.current && !popupRef.current.closed) {
            popupRef.current.location.href = pdfUrl;
          } else {
            window.open(pdfUrl, '_blank');
          }
        } catch (err) {
          try {
            if (popupRef.current && !popupRef.current.closed) {
              popupRef.current.document.write('<html><body><p style="font-family:sans-serif;color:#c00">A PDF letöltése sikertelen. Kérlek próbáld meg újra.</p></body></html>');
              popupRef.current.document.close();
            }
          } catch {}
        }
      }
    },
    onError: () => {
      // Close pre-opened popup if request failed
      try {
        if (popupRef.current && !popupRef.current.closed) {
          popupRef.current.document.write('<html><body><p style="font-family:sans-serif;color:#c00">Mentés sikertelen. Kérlek ellenőrizd a mezőket és próbáld újra.</p></body></html>');
          popupRef.current.document.close();
        }
      } catch {}
    }
  });

  const onSave = async () => {
    // Open popup synchronously to avoid popup-blocker
    popupRef.current = window.open('about:blank', '_blank');
    try {
      await mutateSave.mutateAsync();
    } catch {
      // error handled in onError
    }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Átadás tulajdonosnak</DialogTitle>
      <DialogContent>
        <Box mt={1} display="flex" flexDirection="column" gap={2}>
          <TextField label="Tulajdonos neve" value={ownerName} onChange={(e) => setOwnerName(e.target.value)} fullWidth />
          <TextField label="Lakcím" value={ownerAddress} onChange={(e) => setOwnerAddress(e.target.value)} fullWidth />
          <TextField label="Igazolványszám" value={ownerIdNumber} onChange={(e) => setOwnerIdNumber(e.target.value)} fullWidth />
          <TextField
            label="Átadás időpontja"
            type="datetime-local"
            value={handoverAt}
            onChange={(e) => setHandoverAt(e.target.value)}
            InputLabelProps={{ shrink: true }}
            fullWidth
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Mégse</Button>
        <Button variant="contained" onClick={onSave} disabled={!ownerName || !ownerAddress || !ownerIdNumber || !handoverAt || mutateSave.isPending}>
          Mentés és nyomtatás
        </Button>
      </DialogActions>
    </Dialog>
  );
}
