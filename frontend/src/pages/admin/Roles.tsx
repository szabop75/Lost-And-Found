import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, LinearProgress, Paper, Snackbar, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Tooltip, Typography, Alert } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import SaveIcon from '@mui/icons-material/Save';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import api from '../../api/client';

interface RoleDto { name: string; }
interface RolePermissionDto {
  roleName: string;
  handoverOwner: boolean;
  handoverOffice: boolean;
  transferStorage: boolean;
  receiveStorage: boolean;
  dispose: boolean;
  destroy: boolean;
  sell: boolean;
}

export default function AdminRolesPage() {
  const rolesQ = useQuery<RoleDto[]>({ queryKey: ['admin-roles'], queryFn: async () => (await api.get<RoleDto[]>('/api/admin/roles')).data });
  const permsQ = useQuery<RolePermissionDto[]>({ queryKey: ['admin-role-perms'], queryFn: async () => (await api.get<RolePermissionDto[]>('/api/admin/roles/permissions')).data });

  const [edited, setEdited] = useState<Record<string, RolePermissionDto>>({});
  const [snack, setSnack] = useState<{ open: boolean; msg: string; severity: 'success'|'error'|'info'}>({ open: false, msg: '', severity: 'success' });
  const [createOpen, setCreateOpen] = useState(false);
  const [newRoleName, setNewRoleName] = useState('');
  const roleNamesLower = useMemo(() => new Set((rolesQ.data ?? []).map(r => r.name.toLowerCase())), [rolesQ.data]);
  const trimmedName = newRoleName.trim();
  const isDuplicate = trimmedName.length > 0 && roleNamesLower.has(trimmedName.toLowerCase());
  const createDisabled = trimmedName.length === 0 || isDuplicate;

  const merged = useMemo(() => {
    const map: Record<string, RolePermissionDto> = {};
    (permsQ.data ?? []).forEach(p => { map[p.roleName] = { ...p }; });
    return map;
  }, [permsQ.data]);

  const list = useMemo(() => (rolesQ.data ?? []).map(r => merged[r.name] ?? ({
    roleName: r.name,
    handoverOwner: false,
    handoverOffice: false,
    transferStorage: false,
    receiveStorage: false,
    dispose: false,
    destroy: false,
    sell: false,
  })), [rolesQ.data, merged]);

  const handleToggle = (role: string, key: keyof Omit<RolePermissionDto, 'roleName'>) => (e: any) => {
    setEdited(prev => ({
      ...prev,
      [role]: { ...(prev[role] ?? merged[role] ?? { roleName: role, handoverOwner: false, handoverOffice: false, transferStorage: false, receiveStorage: false, dispose: false, destroy: false, sell: false }), [key]: Boolean(e.target.checked) }
    }));
  };

  const handleSave = async (role: string) => {
    try {
      const dto = edited[role] ?? merged[role];
      if (!dto) return;
      await api.put(`/api/admin/roles/permissions/${encodeURIComponent(role)}`, dto);
      setSnack({ open: true, msg: 'Mentve.', severity: 'success' });
      setEdited(prev => { const c = { ...prev }; delete c[role]; return c; });
      await permsQ.refetch();
    } catch (e: any) {
      setSnack({ open: true, msg: 'Mentés sikertelen.', severity: 'error' });
    }
  };

  const handleDelete = async (role: string) => {
    if (!confirm(`Biztosan törlöd a(z) ${role} szerepkört?`)) return;
    try {
      await api.delete(`/api/admin/roles/${encodeURIComponent(role)}`);
      setSnack({ open: true, msg: 'Szerepkör törölve.', severity: 'success' });
      await Promise.all([rolesQ.refetch(), permsQ.refetch()]);
    } catch (e: any) {
      const msg = e?.response?.data ?? 'Nem törölhető: a szerepkör használatban lehet.';
      setSnack({ open: true, msg: String(msg), severity: 'error' });
    }
  };

  const columns: Array<{ key: keyof Omit<RolePermissionDto, 'roleName'>; label: string }> = [
    { key: 'handoverOwner', label: 'Átadás tulajdonosnak' },
    { key: 'handoverOffice', label: 'Átadás Okmányirodába' },
    { key: 'transferStorage', label: 'Tárolási hely módosítása' },
    { key: 'receiveStorage', label: 'Átvétel tárolási helyen' },
    { key: 'dispose', label: 'Selejtezés' },
    { key: 'destroy', label: 'Megsemmisítés' },
    { key: 'sell', label: 'Értékesítés' },
  ];

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between">
        <Typography variant="h5">Szerepkörök és jogosultságok</Typography>
        <Button startIcon={<AddCircleOutlineIcon />} variant="contained" onClick={() => setCreateOpen(true)}>Új szerepkör</Button>
      </Stack>

      {(rolesQ.isLoading || permsQ.isLoading) && <LinearProgress />}

      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Szerepkör</TableCell>
              {columns.map(c => <TableCell key={c.key}>{c.label}</TableCell>)}
              <TableCell align="right">Műveletek</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {list.map(r => {
              const row = edited[r.roleName] ?? r;
              return (
                <TableRow key={r.roleName} hover>
                  <TableCell>{r.roleName}</TableCell>
                  {columns.map(c => (
                    <TableCell key={c.key} align="center">
                      <Checkbox checked={row[c.key]} onChange={handleToggle(r.roleName, c.key)} />
                    </TableCell>
                  ))}
                  <TableCell align="right">
                    <Tooltip title="Mentés"><span><IconButton color="primary" onClick={() => handleSave(r.roleName)} disabled={!edited[r.roleName]}><SaveIcon /></IconButton></span></Tooltip>
                    <Tooltip title="Törlés"><span><IconButton color="error" onClick={() => handleDelete(r.roleName)}><DeleteIcon /></IconButton></span></Tooltip>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>

      <Dialog open={createOpen} onClose={() => setCreateOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Új szerepkör</DialogTitle>
        <DialogContent>
          <Stack spacing={2} mt={1}>
            <Alert severity="info">Az új szerepkör alapértelmezés szerint minden műveletre tiltott. Mentés után állítsd be a jogosultságokat.</Alert>
            <TextField label="Szerepkör neve" value={newRoleName} onChange={e => setNewRoleName(e.target.value)} size="small" fullWidth
              error={createDisabled && trimmedName.length === 0 || isDuplicate}
              helperText={trimmedName.length === 0 ? 'Kötelező mező' : (isDuplicate ? 'Már létező szerepkör' : ' ')}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCreateOpen(false)}>Mégse</Button>
          <Button variant="contained" disabled={createDisabled} onClick={async () => {
            try {
              if (!trimmedName) return;
              await api.post('/api/admin/roles', { name: trimmedName });
              setNewRoleName('');
              setCreateOpen(false);
              await Promise.all([rolesQ.refetch(), permsQ.refetch()]);
              setSnack({ open: true, msg: 'Szerepkör létrehozva.', severity: 'success' });
            } catch (e: any) {
              const msg = e?.response?.data ?? 'Létrehozás sikertelen.';
              setSnack({ open: true, msg: String(msg), severity: 'error' });
            }
          }}>Létrehozás</Button>
        </DialogActions>
      </Dialog>

      <Snackbar open={snack.open} autoHideDuration={4000} onClose={() => setSnack(s => ({ ...s, open: false }))}>
        <Alert severity={snack.severity} onClose={() => setSnack(s => ({ ...s, open: false }))}>{snack.msg}</Alert>
      </Snackbar>
    </Stack>
  );
}
