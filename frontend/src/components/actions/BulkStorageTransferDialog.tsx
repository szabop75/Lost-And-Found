import { useEffect, useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Stack, TextField, FormControl, InputLabel, Select, MenuItem, Alert } from '@mui/material';
import api from '../../api/client';

export default function BulkStorageTransferDialog({ open, onClose, itemIds, onSuccess }: {
  open: boolean;
  onClose: () => void;
  itemIds: string[];
  onSuccess: () => void;
}) {
  const [locations, setLocations] = useState<Array<{ id: string; name: string }>>([]);
  const [storageLocationId, setStorageLocationId] = useState('');
  const [courier, setCourier] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (open) {
      api.get<Array<{ id: string; name: string }>>('/api/storage-locations')
        .then(res => setLocations(res.data))
        .catch(() => setLocations([]));
      setStorageLocationId('');
      setCourier('');
      setNotes('');
      setError(null);
    }
  }, [open]);

  const handleSubmit = async () => {
    setError(null);
    if (!storageLocationId) { setError('Tárolási hely kötelező.'); return; }
    if (!courier) { setError('Szállítást végző kötelező.'); return; }
    setLoading(true);
    try {
      await api.post('/api/items/bulk/transfer-storage', { itemIds, storageLocationId, courierUserIdOrName: courier, notes: notes || undefined });
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
      <DialogTitle>Csoportos tárolási hely módosítás</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={1}>
          {error && <Alert severity="error">{error}</Alert>}
          <FormControl size="small" fullWidth>
            <InputLabel shrink>Tárolási hely</InputLabel>
            <Select size="small" label="Tárolási hely" value={storageLocationId} onChange={(e) => setStorageLocationId(e.target.value)}>
              {locations.map(l => <MenuItem key={l.id} value={l.id}>{l.name}</MenuItem>)}
            </Select>
          </FormControl>
          <TextField label="Szállítást végző (felhasználó/név)" value={courier} onChange={e => setCourier(e.target.value)} required size="small" fullWidth />
          <TextField label="Megjegyzés (opcionális)" value={notes} onChange={e => setNotes(e.target.value)} size="small" fullWidth />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Mégse</Button>
        <Button onClick={handleSubmit} variant="contained" disabled={loading || itemIds.length === 0}>Mentés</Button>
      </DialogActions>
    </Dialog>
  );
}
