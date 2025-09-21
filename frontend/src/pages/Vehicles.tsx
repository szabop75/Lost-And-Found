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

export type Vehicle = {
  id: string;
  licensePlate: string; // Rendszám
  active: boolean;
};

async function fetchVehicles(): Promise<Vehicle[]> {
  const res = await api.get<Vehicle[]>('/api/vehicles');
  return res.data;
}

export default function Vehicles() {
  const qc = useQueryClient();
  const { data } = useQuery({ queryKey: ['vehicles'], queryFn: fetchVehicles });

  const [creating, setCreating] = useState(false);
  const [newPlate, setNewPlate] = useState('');
  const [createError, setCreateError] = useState<string | null>(null);

  const createMut = useMutation({
    mutationFn: async () => {
      setCreateError(null);
      const res = await api.post<Vehicle>('/api/vehicles', {
        licensePlate: newPlate,
        active: true,
      });
      return res.data;
    },
    onSuccess: (created) => {
      setCreating(false);
      setNewPlate('');
      qc.setQueryData<Vehicle[] | undefined>(['vehicles'], (old) => ([...(old ?? []), created]));
    },
    onError: (err: any) => {
      console.error('Vehicle create failed', err);
      setCreateError(err?.response?.data ?? 'Mentés sikertelen');
    }
  });

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editPlate, setEditPlate] = useState('');

  const startEdit = (v: Vehicle) => {
    setEditingId(v.id);
    setEditPlate(v.licensePlate);
  };

  const saveEditMut = useMutation({
    mutationFn: async () => {
      if (!editingId) return;
      await api.put(`/api/vehicles/${editingId}`, {
        id: editingId,
        licensePlate: editPlate,
        active: true,
      });
    },
    onSuccess: () => {
      const id = editingId; // capture before clearing
      setEditingId(null);
      qc.setQueryData<Vehicle[] | undefined>(['vehicles'], (old) => {
        if (!old) return old;
        return old.map(v => v.id === id ? { ...v, licensePlate: editPlate } : v);
      });
    },
    onError: (err: any) => {
      console.error('Vehicle update failed', err);
      alert('Mentés sikertelen');
    }
  });

  const deleteMut = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/vehicles/${id}`);
    },
    onSuccess: (_data, deletedId) => {
      qc.setQueryData<Vehicle[] | undefined>(['vehicles'], (old) => old ? old.filter(v => v.id !== deletedId) : old);
    },
    onError: (err: any) => {
      console.error('Vehicle delete failed', err);
      alert('Törlés sikertelen');
    }
  });

  const ordered = (data ?? []).slice().sort((a, b) => a.licensePlate.localeCompare(b.licensePlate));

  return (
    <Stack spacing={2}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5">Járművek</Typography>
        <Button startIcon={<AddIcon />} variant="contained" onClick={() => setCreating(true)}>Új jármű</Button>
      </Stack>

      {creating && (
        <Paper sx={{ p: 2 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField label="Rendszám" value={newPlate} onChange={(e) => setNewPlate(e.target.value)} fullWidth />
            {createError && (
              <Typography color="error" variant="body2">{createError}</Typography>
            )}
            <Box display="flex" gap={1} alignItems="center">
              <Button onClick={() => setCreating(false)}>Mégse</Button>
              <Button variant="contained" onClick={() => createMut.mutate()} disabled={!newPlate || createMut.isPending}>
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
              <TableCell>Rendszám</TableCell>
              <TableCell align="center" sx={{ width: 120 }}>Műveletek</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {ordered.map((v) => (
              <TableRow key={v.id} hover>
                <TableCell>
                  {editingId === v.id ? (
                    <TextField value={editPlate} onChange={(e) => setEditPlate(e.target.value)} fullWidth />
                  ) : (
                    <Typography variant="subtitle2">{v.licensePlate}</Typography>
                  )}
                </TableCell>
                <TableCell align="center">
                  {editingId === v.id ? (
                    <Box display="inline-flex" gap={1}>
                      <Button onClick={() => setEditingId(null)}>Mégse</Button>
                      <Button variant="contained" onClick={() => saveEditMut.mutate()} disabled={!editPlate || saveEditMut.isPending}>
                        {saveEditMut.isPending ? 'Mentés...' : 'Mentés'}
                      </Button>
                    </Box>
                  ) : (
                    <Box display="inline-flex" gap={1}>
                      <IconButton size="small" onClick={() => startEdit(v)} disabled={deleteMut.isPending}><EditIcon /></IconButton>
                      <IconButton size="small" onClick={() => deleteMut.mutate(v.id)} disabled={deleteMut.isPending}><DeleteIcon /></IconButton>
                    </Box>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {ordered.length === 0 && (
              <TableRow>
                <TableCell colSpan={2}>
                  <Typography align="center">Nincs jármű.</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}
