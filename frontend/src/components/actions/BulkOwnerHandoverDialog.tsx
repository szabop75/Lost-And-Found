import { useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Stack, TextField, Alert } from '@mui/material';
import api from '../../api/client';

export default function BulkOwnerHandoverDialog({ open, onClose, itemIds, onSuccess }: {
  open: boolean;
  onClose: () => void;
  itemIds: string[];
  onSuccess: () => void;
}) {
  const [ownerName, setOwnerName] = useState('');
  const [ownerAddress, setOwnerAddress] = useState('');
  const [ownerIdNumber, setOwnerIdNumber] = useState('');
  const [handoverAt, setHandoverAt] = useState<string>(new Date().toISOString().slice(0,16));
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    setError(null);
    if (!ownerName || !ownerAddress || !handoverAt) { setError('Név, cím és időpont kötelező.'); return; }
    setLoading(true);
    try {
      await api.post('/api/items/bulk/handover-owner', {
        itemIds,
        ownerName,
        ownerAddress,
        ownerIdNumber: ownerIdNumber || undefined,
        handoverAt: new Date(handoverAt)
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
      <DialogTitle>Csoportos átadás tulajdonosnak</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={1}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label="Tulajdonos neve" value={ownerName} onChange={e => setOwnerName(e.target.value)} required size="small" fullWidth />
          <TextField label="Tulajdonos címe" value={ownerAddress} onChange={e => setOwnerAddress(e.target.value)} required size="small" fullWidth />
          <TextField label="Igazolványszám (opcionális)" value={ownerIdNumber} onChange={e => setOwnerIdNumber(e.target.value)} size="small" fullWidth />
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
