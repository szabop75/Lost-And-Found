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

export type Driver = {
  id: string;
  code: string; // Törzsszám
  name: string; // Név
  active: boolean;
};

async function fetchDrivers(): Promise<Driver[]> {
  const res = await api.get<Driver[]>('/api/drivers');
  return res.data;
}

export default function Drivers() {
  const qc = useQueryClient();
  const { data } = useQuery({ queryKey: ['drivers'], queryFn: fetchDrivers });

  const [creating, setCreating] = useState(false);
  const [newCode, setNewCode] = useState('');
  const [newName, setNewName] = useState('');
  const [createError, setCreateError] = useState<string | null>(null);

  const createMut = useMutation({
    mutationFn: async () => {
      setCreateError(null);
      const res = await api.post<Driver>('/api/drivers', {
        code: newCode,
        name: newName,
        active: true,
      });
      return res.data;
    },
    onSuccess: (created) => {
      setCreating(false);
      setNewCode(''); setNewName('');
      qc.setQueryData<Driver[] | undefined>(['drivers'], (old) => ([...(old ?? []), created]));
    },
    onError: (err: any) => {
      console.error('Driver create failed', err);
      setCreateError(err?.response?.data ?? 'Mentés sikertelen');
    }
  });

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editCode, setEditCode] = useState('');
  const [editName, setEditName] = useState('');

  const startEdit = (d: Driver) => {
    setEditingId(d.id);
    setEditCode(d.code);
    setEditName(d.name);
  };

  const saveEditMut = useMutation({
    mutationFn: async () => {
      if (!editingId) return;
      await api.put(`/api/drivers/${editingId}`, {
        id: editingId,
        code: editCode,
        name: editName,
        active: true,
      });
    },
    onSuccess: () => {
      const id = editingId; // capture before clearing
      setEditingId(null);
      qc.setQueryData<Driver[] | undefined>(['drivers'], (old) => {
        if (!old) return old;
        return old.map(v => v.id === id ? { ...v, code: editCode, name: editName } : v);
      });
    },
    onError: (err: any) => {
      console.error('Driver update failed', err);
      alert('Mentés sikertelen');
    }
  });

  const deleteMut = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/drivers/${id}`);
    },
    onSuccess: (_data, deletedId) => {
      qc.setQueryData<Driver[] | undefined>(['drivers'], (old) => old ? old.filter(v => v.id !== deletedId) : old);
    },
    onError: (err: any) => {
      console.error('Driver delete failed', err);
      alert('Törlés sikertelen');
    }
  });

  const ordered = (data ?? []).slice().sort((a, b) => a.name.localeCompare(b.name));

  return (
    <Stack spacing={2}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5">Járművezetők</Typography>
        <Button startIcon={<AddIcon />} variant="contained" onClick={() => setCreating(true)}>Új járművezető</Button>
      </Stack>

      {creating && (
        <Paper sx={{ p: 2 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
            <TextField label="Törzsszám" value={newCode} onChange={(e) => setNewCode(e.target.value)} sx={{ minWidth: 200 }} />
            <TextField label="Név" value={newName} onChange={(e) => setNewName(e.target.value)} fullWidth />
            {createError && (
              <Typography color="error" variant="body2">{createError}</Typography>
            )}
            <Box display="flex" gap={1} alignItems="center">
              <Button onClick={() => setCreating(false)}>Mégse</Button>
              <Button variant="contained" onClick={() => createMut.mutate()} disabled={!newCode || !newName || createMut.isPending}>
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
              <TableCell sx={{ width: 220 }}>Törzsszám</TableCell>
              <TableCell>Név</TableCell>
              <TableCell align="center" sx={{ width: 120 }}>Műveletek</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {ordered.map((v) => (
              <TableRow key={v.id} hover>
                <TableCell>
                  {editingId === v.id ? (
                    <TextField value={editCode} onChange={(e) => setEditCode(e.target.value)} fullWidth />
                  ) : (
                    <Typography variant="subtitle2">{v.code}</Typography>
                  )}
                </TableCell>
                <TableCell>
                  {editingId === v.id ? (
                    <TextField value={editName} onChange={(e) => setEditName(e.target.value)} fullWidth />
                  ) : (
                    v.name
                  )}
                </TableCell>
                <TableCell align="center">
                  {editingId === v.id ? (
                    <Box display="inline-flex" gap={1}>
                      <Button onClick={() => setEditingId(null)}>Mégse</Button>
                      <Button variant="contained" onClick={() => saveEditMut.mutate()} disabled={!editCode || !editName || saveEditMut.isPending}>
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
                <TableCell colSpan={3}>
                  <Typography align="center">Nincs járművezető.</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Stack>
  );
}
