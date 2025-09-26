import { useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Stack, TextField, Alert } from '@mui/material';
import api from '../../api/client';

export default function BulkDisposalDialog({ open, onClose, itemIds, onSuccess }: {
  open: boolean;
  onClose: () => void;
  itemIds: string[];
  onSuccess: () => void;
}) {
  const [actor, setActor] = useState('');
  const [notes, setNotes] = useState('');
  const [disposedAt, setDisposedAt] = useState<string>(new Date().toISOString().slice(0,16));
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    setError(null);
    if (!actor || !disposedAt) { setError('Selejtezést végző és időpont kötelező.'); return; }
    setLoading(true);
    try {
      await api.post('/api/items/bulk/dispose', {
        itemIds,
        disposedAt: new Date(disposedAt),
        actorUserIdOrName: actor,
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
      <DialogTitle>Csoportos selejtezés</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={1}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label="Selejtezést végző" value={actor} onChange={e => setActor(e.target.value)} required size="small" fullWidth />
          <TextField label="Megjegyzés (opcionális)" value={notes} onChange={e => setNotes(e.target.value)} size="small" fullWidth />
          <TextField label="Selejtezés időpontja" type="datetime-local" value={disposedAt} onChange={e => setDisposedAt(e.target.value)} required size="small" fullWidth InputLabelProps={{ shrink: true }} />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Mégse</Button>
        <Button onClick={handleSubmit} variant="contained" disabled={loading || itemIds.length === 0}>Mentés</Button>
      </DialogActions>
    </Dialog>
  );
}
