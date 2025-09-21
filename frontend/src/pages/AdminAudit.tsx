import { useQuery } from '@tanstack/react-query';
import api from '../api/client';
import {
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Stack,
  TextField,
  MenuItem,
  Box,
  TablePagination,
} from '@mui/material';
import { useState } from 'react';

type AuditDto = {
  id: string;
  targetUserId: string;
  targetEmail?: string | null;
  oldRole?: string | null;
  newRole?: string | null;
  action: string;
  createdAt: string;
  performedByUserId: string;
  performedByEmail?: string | null;
};

type AuditQueryResult = {
  items: AuditDto[];
  total: number;
};

async function fetchAudit(params: {
  from?: string;
  to?: string;
  action?: string;
  targetEmail?: string;
  performedByEmail?: string;
  page: number;
  pageSize: number;
}): Promise<AuditQueryResult> {
  const sp = new URLSearchParams();
  if (params.from) sp.set('from', params.from);
  if (params.to) sp.set('to', params.to);
  if (params.action) sp.set('action', params.action);
  if (params.targetEmail) sp.set('targetEmail', params.targetEmail);
  if (params.performedByEmail) sp.set('performedByEmail', params.performedByEmail);
  sp.set('page', String(params.page));
  sp.set('pageSize', String(params.pageSize));
  const res = await api.get<AuditQueryResult>(`/api/admin/users/audit?${sp.toString()}`);
  return res.data;
}

export default function AdminAudit() {
  const [from, setFrom] = useState(''); // datetime-local ISO string
  const [to, setTo] = useState('');
  const [action, setAction] = useState('');
  const [targetEmail, setTargetEmail] = useState('');
  const [performedByEmail, setPerformedByEmail] = useState('');
  const [page, setPage] = useState(0); // UI 0-based
  const [rowsPerPage, setRowsPerPage] = useState(20);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-audit', { from, to, action, targetEmail, performedByEmail, page, rowsPerPage }],
    queryFn: () => fetchAudit({
      from: from ? new Date(from).toISOString() : undefined,
      to: to ? new Date(to).toISOString() : undefined,
      action: action || undefined,
      targetEmail: targetEmail || undefined,
      performedByEmail: performedByEmail || undefined,
      page: page + 1,
      pageSize: rowsPerPage,
    }),
  });

  if (isLoading) return <Typography>Betöltés...</Typography>;
  if (isError) return <Typography color="error">Hiba történt az audit napló betöltése közben.</Typography>;

  const rows = data?.items ?? [];
  const total = data?.total ?? 0;

  return (
    <Stack spacing={2}>
      <Typography variant="h5">Jogosultság kezelés audit napló</Typography>

      {/* Filters */}
      <Paper sx={{ p: 2 }}>
        <Stack
          direction="row"
          spacing={2}
          alignItems="center"
          flexWrap="nowrap"
          sx={{ overflowX: 'auto', whiteSpace: 'nowrap' }}
        >
          <TextField
            label="Dátumtól"
            type="datetime-local"
            InputLabelProps={{ shrink: true }}
            value={from}
            size="small"
            sx={{ minWidth: 220 }}
            onChange={(e) => { setFrom(e.target.value); setPage(0); }}
          />
          <TextField
            label="Dátumig"
            type="datetime-local"
            InputLabelProps={{ shrink: true }}
            value={to}
            size="small"
            sx={{ minWidth: 220 }}
            onChange={(e) => { setTo(e.target.value); setPage(0); }}
          />
          <TextField
            select
            label="Művelet"
            value={action}
            onChange={(e) => { setAction(e.target.value); setPage(0); }}
            size="small"
            sx={{ minWidth: 160 }}
          >
            <MenuItem value=""><em>Összes</em></MenuItem>
            <MenuItem value="CreateUser">CreateUser</MenuItem>
            <MenuItem value="UpdateRole">UpdateRole</MenuItem>
            <MenuItem value="DeleteUser">DeleteUser</MenuItem>
          </TextField>
          <TextField
            label="Cél e-mail"
            value={targetEmail}
            size="small"
            sx={{ minWidth: 220 }}
            onChange={(e) => { setTargetEmail(e.target.value); setPage(0); }}
          />
          <TextField
            label="Végrehajtó e-mail"
            value={performedByEmail}
            size="small"
            sx={{ minWidth: 220 }}
            onChange={(e) => { setPerformedByEmail(e.target.value); setPage(0); }}
          />
          <Box flexGrow={1} />
        </Stack>
      </Paper>
      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Dátum</TableCell>
              <TableCell>Művelet</TableCell>
              <TableCell>Felhasználó (cél)</TableCell>
              <TableCell>Régi szerep</TableCell>
              <TableCell>Új szerep</TableCell>
              <TableCell>Végrehajtó</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map(a => (
              <TableRow key={a.id}>
                <TableCell>{new Date(a.createdAt).toLocaleString('hu-HU')}</TableCell>
                <TableCell>{a.action}</TableCell>
                <TableCell>{a.targetEmail ?? a.targetUserId}</TableCell>
                <TableCell>{a.oldRole ?? ''}</TableCell>
                <TableCell>{a.newRole ?? ''}</TableCell>
                <TableCell>{a.performedByEmail ?? a.performedByUserId}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Pagination */}
      <Box display="flex" justifyContent="flex-end">
        <TablePagination
          component="div"
          count={total}
          page={page}
          onPageChange={(_, newPage) => setPage(newPage)}
          rowsPerPage={rowsPerPage}
          onRowsPerPageChange={(e: any) => { setRowsPerPage(parseInt(e.target.value, 10)); setPage(0); }}
          rowsPerPageOptions={[10, 20, 50, 100]}
          labelRowsPerPage="Sor/oldal"
        />
      </Box>
    </Stack>
  );
}
