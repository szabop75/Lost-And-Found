import { useState } from 'react';
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

export default function OfficeHandoverDialog({ open, onClose, item }: Props) {
  const [courierUser, setCourierUser] = useState('');
  const [handoverAt, setHandoverAt] = useState<string>('');
  const [notes, setNotes] = useState('');
  const qc = useQueryClient();

  let popupRef: Window | null = null;
  const mutateSave = useMutation({
    mutationFn: async () => {
      if (!item) return;
      await api.post(`/api/items/${item.id}/handover-office`, {
        courierUserIdOrName: courierUser,
        handoverAt,
        notes,
      });
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['items'] });
      onClose();
      if (item) {
        try {
          const base = (import.meta as any).env?.VITE_API_BASE_URL ?? '';
          const res = await api.get(`${base}/api/items/${item.id}/print/office-handover`, { responseType: 'blob' });
          const blob = new Blob([res.data], { type: 'application/pdf' });
          const pdfUrl = URL.createObjectURL(blob);
          if (popupRef && !popupRef.closed) {
            popupRef.location.href = pdfUrl;
          } else {
            window.open(pdfUrl, '_blank');
          }
        } catch {
          try { if (popupRef && !popupRef.closed) popupRef.close(); } catch {}
        }
      }
    },
    onError: () => {
      try { if (popupRef && !popupRef.closed) popupRef.close(); } catch {}
    }
  });

  const onSave = async () => {
    popupRef = window.open('about:blank', '_blank');
    mutateSave.mutate();
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Átadás Okmányirodába</DialogTitle>
      <DialogContent>
        <Box mt={1} display="flex" flexDirection="column" gap={2}>
          <TextField
            label="Átvevő felhasználó"
            value={courierUser}
            onChange={(e) => setCourierUser(e.target.value)}
            fullWidth
          />
          <TextField
            label="Átadás időpontja"
            type="datetime-local"
            value={handoverAt}
            onChange={(e) => setHandoverAt(e.target.value)}
            InputLabelProps={{ shrink: true }}
            fullWidth
          />
          <TextField
            label="Megjegyzés"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            multiline
            minRows={2}
            fullWidth
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Mégse</Button>
        <Button variant="contained" onClick={onSave} disabled={!courierUser || !handoverAt || mutateSave.isPending}>Mentés és nyomtatás</Button>
      </DialogActions>
    </Dialog>
  );
}
