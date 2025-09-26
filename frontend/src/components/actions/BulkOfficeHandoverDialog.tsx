import { useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Stack, TextField, Alert } from '@mui/material';
import api from '../../api/client';

export default function BulkOfficeHandoverDialog({ open, onClose, itemIds, onSuccess }: {
  open: boolean;
  onClose: () => void;
  itemIds: string[];
  onSuccess: () => void;
}) {
  const [courier, setCourier] = useState('');
  const [notes, setNotes] = useState('');
  const [handoverAt, setHandoverAt] = useState<string>(new Date().toISOString().slice(0,16));
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    setError(null);
    if (!courier || !handoverAt) { setError('Átadó és időpont kötelező.'); return; }
    setLoading(true);
    try {
      await api.post('/api/items/bulk/handover-office', {
        itemIds,
        courierUserIdOrName: courier,
        handoverAt: new Date(handoverAt),
        notes: notes || undefined
      });
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
      <DialogTitle>Csoportos átadás Okmányirodába</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={1}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label="Átadó (felhasználó/név)" value={courier} onChange={e => setCourier(e.target.value)} required size="small" fullWidth />
          <TextField label="Megjegyzés (opcionális)" value={notes} onChange={e => setNotes(e.target.value)} size="small" fullWidth />
          <TextField label="Átadás időpontja" type="datetime-local" value={handoverAt} onChange={e => setHandoverAt(e.target.value)} required size="small" fullWidth InputLabelProps={{ shrink: true }} />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Mégse</Button>
        <Button onClick={handleSubmit} variant="contained" disabled={loading || itemIds.length === 0}>Mentés</Button>
      </DialogActions>
    </Dialog>
  );
}
