import { useEffect, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../../api/client';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
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

export default function StorageTransferDialog({ open, onClose, item }: Props) {
  const [locations, setLocations] = useState<Array<{ id: string; name: string }>>([]);
  const [locationId, setLocationId] = useState<string>('');
  const [notes, setNotes] = useState<string>('');
  const qc = useQueryClient();

  const loadLocations = async () => {
    const res = await api.get<Array<{ id: string; name: string }>>('/api/storage-locations');
    setLocations(res.data);
  };

  useEffect(() => {
    if (open) {
      loadLocations().catch(() => setLocations([]));
      setLocationId('');
      setNotes('');
    }
  }, [open]);

  const mutateSave = useMutation({
    mutationFn: async () => {
      if (!item) return;
      await api.post(`/api/items/${item.id}/storage-location`, {
        storageLocationId: locationId,
        notes,
      });
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['items'] });
      onClose();
    },
  });

  const onSave = async () => {
    mutateSave.mutate();
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Tárolási hely módosítása</DialogTitle>
      <DialogContent>
        <Box mt={1} display="flex" flexDirection="column" gap={2}>
          <FormControl size="small" fullWidth>
            <InputLabel shrink>Tárolási hely</InputLabel>
            <Select
              size="small"
              label="Tárolási hely"
              value={locationId}
              onChange={(e) => setLocationId(e.target.value)}
            >
              {locations.map((l) => (
                <MenuItem key={l.id} value={l.id}>{l.name}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            label="Megjegyzés"
            multiline
            minRows={2}
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            fullWidth
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Mégse</Button>
        <Button variant="contained" onClick={onSave} disabled={!locationId || mutateSave.isPending}>Mentés</Button>
      </DialogActions>
    </Dialog>
  );
}
