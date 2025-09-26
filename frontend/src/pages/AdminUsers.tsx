import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../api/client';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';

export type UserDto = {
  id: string;
  email: string;
  fullName?: string | null;
  phoneNumber?: string | null;
  role: string;
};

async function fetchUsers(): Promise<UserDto[]> {
  const res = await api.get<UserDto[]>('/api/admin/users');
  return res.data;
}

export default function AdminUsers() {
  const qc = useQueryClient();
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-users'], queryFn: fetchUsers });
  // dynamic roles
  const rolesQuery = useQuery<{ name: string }[]>({ queryKey: ['admin-roles'], queryFn: async () => (await api.get<{ name: string }[]>('/api/admin/roles')).data });

  // Create dialog state
  const [openCreate, setOpenCreate] = useState(false);
  const [cEmail, setCEmail] = useState('');
  const [cPassword, setCPassword] = useState('');
  const [cFullName, setCFullName] = useState('');
  const [cPhone, setCPhone] = useState('');
  const [cRole, setCRole] = useState<string>('');
  const [createError, setCreateError] = useState<string | null>(null);

  const createMut = useMutation({
    mutationFn: async () => {
      setCreateError(null);
      // if no role selected but roles exist, default to the first
      const roleToUse = cRole || (rolesQuery.data && rolesQuery.data.length > 0 ? rolesQuery.data[0].name : '');
      await api.post('/api/admin/users', {
        email: cEmail,
        password: cPassword,
        fullName: cFullName || null,
        phoneNumber: cPhone || null,
        role: roleToUse,
      });
    },
    onSuccess: async () => {
      setOpenCreate(false);
      setCEmail(''); setCPassword(''); setCFullName(''); setCPhone(''); setCRole('');
      await qc.invalidateQueries({ queryKey: ['admin-users'] });
    },
    onError: (err: any) => setCreateError(err?.response?.data ?? 'Létrehozás sikertelen'),
  });

  // Edit dialog state
  const [openEdit, setOpenEdit] = useState(false);
  const [eId, setEId] = useState<string | null>(null);
  const [eFullName, setEFullName] = useState('');
  const [ePhone, setEPhone] = useState('');
  const [eRole, setERole] = useState<string>('');
  const [ePassword, setEPassword] = useState('');

  const startEdit = (u: UserDto) => {
    setEId(u.id);
    setEFullName(u.fullName || '');
    setEPhone(u.phoneNumber || '');
    setERole(u.role || '');
    setEPassword('');
    setOpenEdit(true);
  };

  const saveEditMut = useMutation({
    mutationFn: async () => {
      if (!eId) return;
      const roleToUse = eRole || (rolesQuery.data && rolesQuery.data.length > 0 ? rolesQuery.data[0].name : '');
      await api.put(`/api/admin/users/${eId}`, {
        fullName: eFullName || null,
        phoneNumber: ePhone || null,
        role: roleToUse,
        password: ePassword || null,
      });
    },
    onSuccess: async () => {
      setOpenEdit(false);
      setEId(null);
      await qc.invalidateQueries({ queryKey: ['admin-users'] });
    },
    onError: () => alert('Mentés sikertelen'),
  });

  const deleteMut = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/api/admin/users/${id}`);
    },
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ['admin-users'] });
    },
    onError: () => alert('Törlés sikertelen'),
  });

  if (isLoading) return <Typography>Betöltés...</Typography>;
  if (isError) return <Typography color="error">Hiba történt a felhasználók betöltése közben.</Typography>;

  const users = data ?? [];

  return (
    <Stack spacing={2}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5">Felhasználók</Typography>
        <Button variant="contained" onClick={() => setOpenCreate(true)}>Új felhasználó</Button>
      </Stack>

      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Email</TableCell>
              <TableCell>Név</TableCell>
              <TableCell>Telefon</TableCell>
              <TableCell>Szerepkör</TableCell>
              <TableCell align="center">Műveletek</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {users.map(u => (
              <TableRow key={u.id}>
                <TableCell>{u.email}</TableCell>
                <TableCell>{u.fullName ?? ''}</TableCell>
                <TableCell>{u.phoneNumber ?? ''}</TableCell>
                <TableCell>{u.role}</TableCell>
                <TableCell align="center">
                  <IconButton size="small" onClick={() => startEdit(u)}><EditIcon /></IconButton>
                  <IconButton size="small" color="error" onClick={() => deleteMut.mutate(u.id)}><DeleteIcon /></IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Create dialog */}
      <Dialog open={openCreate} onClose={() => setOpenCreate(false)} fullWidth maxWidth="sm">
        <DialogTitle>Új felhasználó</DialogTitle>
        <DialogContent>
          <Box mt={1} display="flex" flexDirection="column" gap={2}>
            <TextField label="Email" type="email" value={cEmail} onChange={(e) => setCEmail(e.target.value)} fullWidth />
            <TextField label="Jelszó" type="password" value={cPassword} onChange={(e) => setCPassword(e.target.value)} fullWidth />
            <TextField label="Név" value={cFullName} onChange={(e) => setCFullName(e.target.value)} fullWidth />
            <TextField label="Telefon" value={cPhone} onChange={(e) => setCPhone(e.target.value)} fullWidth />
            <TextField select label="Szerepkör" value={cRole} onChange={(e) => setCRole(e.target.value as string)}>
              {(rolesQuery.data ?? []).map(r => (
                <MenuItem key={r.name} value={r.name}>{r.name}</MenuItem>
              ))}
            </TextField>
            {createError && <Typography color="error" variant="body2">{createError}</Typography>}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenCreate(false)}>Mégse</Button>
          <Button variant="contained" onClick={() => createMut.mutate()} disabled={!cEmail || !cPassword || (rolesQuery.data && rolesQuery.data.length === 0)}>Mentés</Button>
        </DialogActions>
      </Dialog>

      {/* Edit dialog */}
      <Dialog open={openEdit} onClose={() => setOpenEdit(false)} fullWidth maxWidth="sm">
        <DialogTitle>Felhasználó szerkesztése</DialogTitle>
        <DialogContent>
          <Box mt={1} display="flex" flexDirection="column" gap={2}>
            <TextField label="Név" value={eFullName} onChange={(e) => setEFullName(e.target.value)} fullWidth />
            <TextField label="Telefon" value={ePhone} onChange={(e) => setEPhone(e.target.value)} fullWidth />
            <TextField select label="Szerepkör" value={eRole} onChange={(e) => setERole(e.target.value as string)}>
              {(rolesQuery.data ?? []).map(r => (
                <MenuItem key={r.name} value={r.name}>{r.name}</MenuItem>
              ))}
            </TextField>
            <TextField label="Új jelszó (opcionális)" type="password" value={ePassword} onChange={(e) => setEPassword(e.target.value)} fullWidth />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenEdit(false)}>Mégse</Button>
          <Button variant="contained" onClick={() => saveEditMut.mutate()} disabled={!eId}>Mentés</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
