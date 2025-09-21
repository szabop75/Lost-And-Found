import { useState } from 'react';
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

type AuditDto = {
  id: string;
  foundItemId: string;
  action: string;
  performedByUserId: string;
  performedByEmail?: string | null;
  details?: string | null;
  createdAt: string;
  occurredAt?: string | null;
};

type AuditQueryResult = {
  items: AuditDto[];
  total: number;
};

async function fetchItemAudit(params: {
  itemId?: string;
  action?: string;
  actor?: string;
  from?: string;
  to?: string;
  page: number;
  pageSize: number;
}): Promise<AuditQueryResult> {
  const sp = new URLSearchParams();
  if (params.itemId) sp.set('itemId', params.itemId);
  if (params.action) sp.set('action', params.action);
  if (params.actor) sp.set('actor', params.actor);
  if (params.from) sp.set('from', params.from);
  if (params.to) sp.set('to', params.to);
  sp.set('page', String(params.page));
  sp.set('pageSize', String(params.pageSize));
  const res = await api.get<AuditQueryResult>(`/api/admin/items/audit?${sp.toString()}`);
  return res.data;
}

export default function AdminItemsAudit() {
  const [itemId, setItemId] = useState('');
  const [action, setAction] = useState('');
  const [actor, setActor] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(20);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-items-audit', { itemId, action, actor, from, to, page, rowsPerPage }],
    queryFn: () => fetchItemAudit({
      itemId: itemId || undefined,
      action: action || undefined,
      actor: actor || undefined,
      from: from ? new Date(from).toISOString() : undefined,
      to: to ? new Date(to).toISOString() : undefined,
      page: page + 1,
      pageSize: rowsPerPage,
    })
  });

  if (isLoading) return <Typography>Betöltés...</Typography>;
  if (isError) return <Typography color="error">Hiba történt az audit napló betöltése közben.</Typography>;

  const rows = data?.items ?? [];
  const total = data?.total ?? 0;

  return (
    <Stack spacing={2}>
      <Typography variant="h5">Talált tárgyak audit napló</Typography>

      {/* Filters */}
      <Paper sx={{ p: 2 }}>
        <Stack direction="row" spacing={2} alignItems="center" flexWrap="nowrap" sx={{ overflowX: 'auto', whiteSpace: 'nowrap' }}>
          <TextField label="Tárgy azonosító" placeholder="GUID" value={itemId} onChange={(e) => { setItemId(e.target.value); setPage(0); }} size="small" sx={{ minWidth: 220 }} />
          <TextField select label="Művelet" value={action} onChange={(e) => { setAction(e.target.value); setPage(0); }} size="small" sx={{ minWidth: 160 }}>
            <MenuItem value=""><em>Összes</em></MenuItem>
            <MenuItem value="Create">Create</MenuItem>
            <MenuItem value="Store">Store</MenuItem>
            <MenuItem value="TransferToOffice">TransferToOffice</MenuItem>
            <MenuItem value="ReleaseToOwner">ReleaseToOwner</MenuItem>
            <MenuItem value="Dispose">Dispose</MenuItem>
          </TextField>
          <TextField label="Végrehajtó" value={actor} onChange={(e) => { setActor(e.target.value); setPage(0); }} size="small" sx={{ minWidth: 200 }} />
          <TextField label="Dátumtól" type="datetime-local" InputLabelProps={{ shrink: true }} value={from} onChange={(e) => { setFrom(e.target.value); setPage(0); }} size="small" sx={{ minWidth: 220 }} />
          <TextField label="Dátumig" type="datetime-local" InputLabelProps={{ shrink: true }} value={to} onChange={(e) => { setTo(e.target.value); setPage(0); }} size="small" sx={{ minWidth: 220 }} />
          <Box flexGrow={1} />
        </Stack>
      </Paper>

      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Dátum</TableCell>
              <TableCell>Művelet</TableCell>
              <TableCell>Végrehajtó</TableCell>
              <TableCell>Leírás</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map(a => (
              <TableRow key={a.id}>
                <TableCell>{new Date(a.createdAt).toLocaleString('hu-HU')}</TableCell>
                <TableCell>{a.action}</TableCell>
                <TableCell>{a.performedByEmail ?? a.performedByUserId}</TableCell>
                <TableCell>{a.details ?? ''}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

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
