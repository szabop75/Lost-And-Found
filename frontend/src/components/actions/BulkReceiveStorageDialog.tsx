import { useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Stack, TextField, Alert } from '@mui/material';
import api from '../../api/client';

export default function BulkReceiveStorageDialog({ open, onClose, itemIds, onSuccess }: {
  open: boolean;
  onClose: () => void;
  itemIds: string[];
  onSuccess: () => void;
}) {
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    setError(null);
    setLoading(true);
    try {
      await api.post('/api/items/bulk/receive-storage', { itemIds, notes: notes || undefined });
      onSuccess();
      onClose();
    } catch (e: any) {
      const data = e?.response?.data;
      if (data?.errors?.length) setError(`Hiba: ${data.errors.length} tétel nem feldolgozható.`);
      else setError('Hiba történt a művelet közben.');
    } finally { setLoading(false); }
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Csoportos átvétel tárolási helyen</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={1}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label="Megjegyzés (opcionális)" value={notes} onChange={e => setNotes(e.target.value)} size="small" fullWidth />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Mégse</Button>
        <Button onClick={handleSubmit} variant="contained" disabled={loading || itemIds.length === 0}>Átvétel rögzítése</Button>
      </DialogActions>
    </Dialog>
  );
}
