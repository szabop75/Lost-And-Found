import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Box,
  Button,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import api from '../api/client';

export type StorageLocation = {
  id: string;
  name: string;
  address?: string | null;
  notes?: string | null;
  active: boolean;
};

async function fetchLocations(): Promise<StorageLocation[]> {
  const res = await api.get<StorageLocation[]>('/api/storage-locations');
  return res.data;
}

export default function StorageLocations() {
  const qc = useQueryClient();
  const { data } = useQuery({ queryKey: ['storage-locations'], queryFn: fetchLocations });

  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState('');
  const [newAddress, setNewAddress] = useState('');
  const [newNotes, setNewNotes] = useState('');
  const [createError, setCreateError] = useState<string | null>(null);

  const createMut = useMutation({
    mutationFn: async () => {
      setCreateError(null);
      const res = await api.post<StorageLocation>('/api/storage-locations', {
        name: newName,
        address: newAddress || null,
        notes: newNotes || null,
        active: true,
      });
      return res.data;
    },
    onSuccess: async (created) => {
      setCreating(false);
      setNewName(''); setNewAddress(''); setNewNotes('');
      // Optimista frissítés a listában
      qc.setQueryData<StorageLocation[] | undefined>(['storage-locations'], (old) => {
        const list = old ?? [];
        return [...list, created as StorageLocation];
      });
      // opcionális: háttérben újratöltés
      // await qc.invalidateQueries({ queryKey: ['storage-locations'] });
    },
    onError: (err: any) => {
      console.error('Storage location create failed', err);
      setCreateError(err?.response?.data ?? 'Mentés sikertelen');
    }
  });

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editAddress, setEditAddress] = useState('');
  const [editNotes, setEditNotes] = useState('');

  const startEdit = (loc: StorageLocation) => {
    setEditingId(loc.id);
    setEditName(loc.name);
    setEditAddress(loc.address ?? '');
    setEditNotes(loc.notes ?? '');
  };

  const saveEditMut = useMutation({
    mutationFn: async () => {
      if (!editingId) return;
      await api.put(`/api/storage-locations/${editingId}`, {
        id: editingId,
        name: editName,
        address: editAddress || null,
        notes: editNotes || null,
        active: true,
      });
    },
    onSuccess: async () => {
      setEditingId(null);
      // Cache frissítés lokálisan
      qc.setQueryData<StorageLocation[] | undefined>(['storage-locations'], (old) => {
        if (!old) return old;
        return old.map(l => l.id === editingId ? { ...l, name: editName, address: editAddress || null, notes: editNotes || null, active: true } : l);
      });
    },
    onError: (err: any) => {
      console.error('Storage location update failed', err);
      alert('Mentés sikertelen');
    }
  });

  const deleteMut = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/storage-locations/${id}`);
    },
    onSuccess: async (_data, deletedId) => {
      // Törlés a cache-ből a ténylegesen törölt azonosítóval
      qc.setQueryData<StorageLocation[] | undefined>(['storage-locations'], (old) => {
        if (!old) return old;
        return old.filter(l => l.id !== deletedId);
      });
    },
    onError: (err: any) => {
      console.error('Storage location delete failed', err);
      alert('Törlés sikertelen');
    }
  });

  return (
    <Stack spacing={2}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5">Tárolási helyek</Typography>
        <Button startIcon={<AddIcon />} variant="contained" onClick={() => setCreating(true)}>Új hely</Button>
      </Stack>

      {creating && (
        <Paper sx={{ p: 2 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField label="Név" value={newName} onChange={(e) => setNewName(e.target.value)} fullWidth />
            <TextField label="Cím" value={newAddress} onChange={(e) => setNewAddress(e.target.value)} fullWidth />
            <TextField label="Megjegyzés" value={newNotes} onChange={(e) => setNewNotes(e.target.value)} fullWidth />
            {createError && (
              <Typography color="error" variant="body2">{createError}</Typography>
            )}
            <Box display="flex" gap={1} alignItems="center">
              <Button onClick={() => setCreating(false)}>Mégse</Button>
              <Button variant="contained" onClick={() => createMut.mutate()} disabled={!newName || createMut.isPending}>
                {createMut.isPending ? 'Mentés...' : 'Mentés'}
              </Button>
            </Box>
          </Stack>
        </Paper>
      )}

      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Név</TableCell>
              <TableCell>Cím</TableCell>
              <TableCell>Megjegyzés</TableCell>
              <TableCell align="center">Műveletek</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(data ?? []).map((loc) => (
              <TableRow key={loc.id} hover>
                <TableCell>
                  {editingId === loc.id ? (
                    <TextField value={editName} onChange={(e) => setEditName(e.target.value)} fullWidth />
                  ) : (
                    <Typography variant="subtitle2">{loc.name}</Typography>
                  )}
                </TableCell>
                <TableCell>
                  {editingId === loc.id ? (
                    <TextField value={editAddress} onChange={(e) => setEditAddress(e.target.value)} fullWidth />
                  ) : (
                    loc.address ?? ''
                  )}
                </TableCell>
                <TableCell>
                  {editingId === loc.id ? (
                    <TextField value={editNotes} onChange={(e) => setEditNotes(e.target.value)} fullWidth />
                  ) : (
                    loc.notes ?? ''
                  )}
                </TableCell>
                <TableCell align="center">
                  {editingId === loc.id ? (
                    <Box display="inline-flex" gap={1}>
                      <Button onClick={() => setEditingId(null)}>Mégse</Button>
                      <Button variant="contained" onClick={() => saveEditMut.mutate()} disabled={!editName || saveEditMut.isPending}>
                        {saveEditMut.isPending ? 'Mentés...' : 'Mentés'}
                      </Button>
                    </Box>
                  ) : (
                    <Box display="inline-flex" gap={1}>
                      <IconButton size="small" onClick={() => startEdit(loc)} disabled={deleteMut.isPending}><EditIcon /></IconButton>
                      <IconButton size="small" onClick={() => deleteMut.mutate(loc.id)} disabled={deleteMut.isPending}><DeleteIcon /></IconButton>
                    </Box>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {(data ?? []).length === 0 && (
              <TableRow>
                <TableCell colSpan={4}>
                  <Typography align="center">Nincs tárolási hely.</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}
