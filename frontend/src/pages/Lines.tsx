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

export type BusLine = {
  id: string;
  name: string;      // Vonal/irány
  sortOrder: number; // Sorrend
  active: boolean;
};

async function fetchLines(): Promise<BusLine[]> {
  const res = await api.get<BusLine[]>('/api/lines');
  return res.data;
}

export default function Lines() {
  const qc = useQueryClient();
  const { data } = useQuery({ queryKey: ['lines'], queryFn: fetchLines });

  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState('');
  const [newSort, setNewSort] = useState<number | ''>('');
  const [createError, setCreateError] = useState<string | null>(null);

  const createMut = useMutation({
    mutationFn: async () => {
      setCreateError(null);
      const res = await api.post<BusLine>('/api/lines', {
        name: newName,
        sortOrder: typeof newSort === 'number' ? newSort : 0,
        active: true,
      });
      return res.data;
    },
    onSuccess: (created) => {
      setCreating(false);
      setNewName(''); setNewSort('');
      qc.setQueryData<BusLine[] | undefined>(['lines'], (old) => ([...(old ?? []), created]));
    },
    onError: (err: any) => {
      console.error('Line create failed', err);
      setCreateError(err?.response?.data ?? 'Mentés sikertelen');
    }
  });

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editSort, setEditSort] = useState<number | ''>('');

  const startEdit = (line: BusLine) => {
    setEditingId(line.id);
    setEditName(line.name);
    setEditSort(line.sortOrder);
  };

  const saveEditMut = useMutation({
    mutationFn: async () => {
      if (!editingId) return;
      await api.put(`/api/lines/${editingId}`, {
        id: editingId,
        name: editName,
        sortOrder: typeof editSort === 'number' ? editSort : 0,
        active: true,
      });
    },
    onSuccess: () => {
      const id = editingId; // capture before clearing
      setEditingId(null);
      qc.setQueryData<BusLine[] | undefined>(['lines'], (old) => {
        if (!old) return old;
        return old.map(l => l.id === id ? { ...l, name: editName, sortOrder: typeof editSort === 'number' ? editSort : 0 } : l);
      });
    },
    onError: (err: any) => {
      console.error('Line update failed', err);
      alert('Mentés sikertelen');
    }
  });

  const deleteMut = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/lines/${id}`);
    },
    onSuccess: (_data, deletedId) => {
      qc.setQueryData<BusLine[] | undefined>(['lines'], (old) => old ? old.filter(l => l.id !== deletedId) : old);
    },
    onError: (err: any) => {
      console.error('Line delete failed', err);
      alert('Törlés sikertelen');
    }
  });

  const ordered = (data ?? []).slice().sort((a, b) => (a.sortOrder - b.sortOrder) || a.name.localeCompare(b.name));

  return (
    <Stack spacing={2}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5">Vonalak</Typography>
        <Button startIcon={<AddIcon />} variant="contained" onClick={() => setCreating(true)}>Új vonal</Button>
      </Stack>

      {creating && (
        <Paper sx={{ p: 2 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField label="Vonal / irány" value={newName} onChange={(e) => setNewName(e.target.value)} fullWidth />
            <TextField type="number" label="Sorrend" value={newSort} onChange={(e) => setNewSort(e.target.value === '' ? '' : Number(e.target.value))} sx={{ maxWidth: 160 }} />
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
              <TableCell>Vonal / irány</TableCell>
              <TableCell sx={{ width: 140 }}>Sorrend</TableCell>
              <TableCell align="center" sx={{ width: 120 }}>Műveletek</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {ordered.map((line) => (
              <TableRow key={line.id} hover>
                <TableCell>
                  {editingId === line.id ? (
                    <TextField value={editName} onChange={(e) => setEditName(e.target.value)} fullWidth />
                  ) : (
                    <Typography variant="subtitle2">{line.name}</Typography>
                  )}
                </TableCell>
                <TableCell>
                  {editingId === line.id ? (
                    <TextField type="number" value={editSort} onChange={(e) => setEditSort(e.target.value === '' ? '' : Number(e.target.value))} sx={{ maxWidth: 140 }} />
                  ) : (
                    line.sortOrder
                  )}
                </TableCell>
                <TableCell align="center">
                  {editingId === line.id ? (
                    <Box display="inline-flex" gap={1}>
                      <Button onClick={() => setEditingId(null)}>Mégse</Button>
                      <Button variant="contained" onClick={() => saveEditMut.mutate()} disabled={!editName || saveEditMut.isPending}>
                        {saveEditMut.isPending ? 'Mentés...' : 'Mentés'}
                      </Button>
                    </Box>
                  ) : (
                    <Box display="inline-flex" gap={1}>
                      <IconButton size="small" onClick={() => startEdit(line)} disabled={deleteMut.isPending}><EditIcon /></IconButton>
                      <IconButton size="small" onClick={() => deleteMut.mutate(line.id)} disabled={deleteMut.isPending}><DeleteIcon /></IconButton>
                    </Box>
                  )}
                </TableCell>
              </TableRow>
            ))}
            {ordered.length === 0 && (
              <TableRow>
                <TableCell colSpan={3}>
                  <Typography align="center">Nincs vonal.</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}
