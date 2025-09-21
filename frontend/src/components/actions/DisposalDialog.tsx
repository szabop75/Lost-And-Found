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
  Typography,
} from '@mui/material';

export type ItemLite = {
  id: string;
};

type Props = {
  open: boolean;
  onClose: () => void;
  item: ItemLite | null;
  retentionDays: number; // megőrzési határidő napokban
};

export default function DisposalDialog({ open, onClose, item, retentionDays }: Props) {
  const [actor, setActor] = useState('');
  const [disposedAt, setDisposedAt] = useState<string>('');
  const [notes, setNotes] = useState('');
  const qc = useQueryClient();

  let popupRef: Window | null = null;
  const mutateSave = useMutation({
    mutationFn: async () => {
      if (!item) return;
      await api.post(`/api/items/${item.id}/dispose`, {
        actorUserIdOrName: actor,
        disposedAt,
        notes,
      });
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['items'] });
      onClose();
      if (item) {
        const base = (import.meta as any).env?.VITE_API_BASE_URL ?? '';
        const url = `${base}/api/items/${item.id}/print/disposal`;
        if (popupRef && !popupRef.closed) {
          popupRef.location.href = url;
        } else {
          window.open(url, '_blank');
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
      <DialogTitle>Selejtezés</DialogTitle>
      <DialogContent>
        <Box mt={1} display="flex" flexDirection="column" gap={2}>
          <Typography variant="body2" color="text.secondary">
            Megőrzési határidő: {retentionDays} nap
          </Typography>
          <TextField label="Selejtezést végző" value={actor} onChange={(e) => setActor(e.target.value)} fullWidth />
          <TextField
            label="Selejtezés időpontja"
            type="datetime-local"
            value={disposedAt}
            onChange={(e) => setDisposedAt(e.target.value)}
            InputLabelProps={{ shrink: true }}
            fullWidth
          />
          <TextField label="Megjegyzés" value={notes} onChange={(e) => setNotes(e.target.value)} multiline minRows={2} fullWidth />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Mégse</Button>
        <Button variant="contained" onClick={onSave} disabled={!actor || !disposedAt || mutateSave.isPending}>Mentés és nyomtatás</Button>
      </DialogActions>
    </Dialog>
  );
}
