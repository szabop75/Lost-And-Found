import { useEffect, useState, type ReactNode } from 'react';
import {
  Box,
  Button,
  Checkbox,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TableSortLabel,
  TextField,
  Typography,
  Paper,
  TablePagination,
  IconButton,
  Menu,
  Tooltip,
  OutlinedInput,
  Toolbar,
  Snackbar,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  List,
  ListItem,
  ListItemText,
  DialogActions,
} from '@mui/material';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import { Link as RouterLink } from 'react-router-dom';
import api from '../api/client';
import OfficeHandoverDialog from '../components/actions/OfficeHandoverDialog';
import OwnerHandoverDialog from '../components/actions/OwnerHandoverDialog';
import DisposalDialog from '../components/actions/DisposalDialog';
import BulkOwnerHandoverDialog from '../components/actions/BulkOwnerHandoverDialog';
import BulkOfficeHandoverDialog from '../components/actions/BulkOfficeHandoverDialog';
import BulkStorageTransferDialog from '../components/actions/BulkStorageTransferDialog';
import BulkDisposalDialog from '../components/actions/BulkDisposalDialog';
import BulkDestroyDialog from '../components/actions/BulkDestroyDialog';
import BulkReceiveStorageDialog from '../components/actions/BulkReceiveStorageDialog';
import BulkSellDialog from '../components/actions/BulkSellDialog';
import { useQuery } from '@tanstack/react-query';

type ItemListResponse = {
  id: string;
  category: string;
  otherCategoryText?: string | null;
  status: string;
  createdAt: string;
  foundAt?: string | null;
  foundLocation?: string | null;
  details?: string;
  depositNumber?: string | null;
  depositSubIndex?: number | null;
  finderName?: string | null;
  licensePlate?: string | null;
  busLineName?: string | null;
  storageLocationName?: string | null;
};

type PermissionsResponse = {
  handoverOwner: boolean;
  handoverOffice: boolean;
  transferStorage: boolean;
  receiveStorage: boolean;
  dispose: boolean;
  destroy: boolean;
  sell: boolean;
};

type ItemListResult = {
  items: ItemListResponse[];
  total: number;
};

async function fetchItems(params: {
  q: string;
  category: string;
  status: string;
  storageLocationId?: string;
  sortBy: string;
  sortDir: 'asc' | 'desc';
  page: number;
  pageSize: number;
  excludeClaimed?: boolean;
}): Promise<ItemListResult> {
  const searchParams = new URLSearchParams();
  if (params.q) searchParams.set('q', params.q);
  if (params.category) searchParams.set('category', params.category);
  if (params.status) searchParams.set('status', params.status);
  if (params.sortBy) searchParams.set('sortBy', params.sortBy);
  if (params.sortDir) searchParams.set('sortDir', params.sortDir);
  searchParams.set('page', String(params.page));
  searchParams.set('pageSize', String(params.pageSize));
  if (params.storageLocationId) searchParams.set('storageLocationId', params.storageLocationId);
  if (typeof params.excludeClaimed === 'boolean') searchParams.set('excludeClaimed', String(params.excludeClaimed));
  const qs = searchParams.toString();
  const url = `/api/items${qs ? `?${qs}` : ''}`;
  const res = await api.get<ItemListResult>(url);
  return res.data;
}

export default function ItemsList() {
  
  // Map backend status codes to Hungarian labels
  const statusLabel = (s: string) => {
    switch (s) {
      case 'Received': return 'Beérkezett';
      case 'InStorage': return 'Letárolva';
      case 'Transferred': return 'Átadva';
      case 'Claimed': return 'Tulajdonosnak átadva';
      case 'Disposed': return 'Selejtezve';
      case 'ReadyToDispose': return 'Selejtezendő';
      case 'InTransit': return 'Szállítás alatt';
      case 'Destroyed': return 'Megsemmisítve';
      case 'Sold': return 'Értékesítve';
      default: return s;
    }
  };
  // Filters
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [idFilter, setIdFilter] = useState('');
  const [debouncedIdFilter, setDebouncedIdFilter] = useState('');
  // Storage location filter
  const [storageLocationId, setStorageLocationId] = useState<string>('');
  type StorageLocationRef = { id: string; name: string };
  const [storageLocations, setStorageLocations] = useState<StorageLocationRef[]>([]);

  // Debounce search to avoid excessive refetch and keep input focus stable
  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(t);
  }, [search]);
  useEffect(() => {
    const t = setTimeout(() => setDebouncedIdFilter(idFilter), 300);
    return () => clearTimeout(t);
  }, [idFilter]);
  const [category, setCategory] = useState<string>('');
  const [status, setStatus] = useState<string>('');

  // Load storage locations for filter dropdown
  useEffect(() => {
    (async () => {
      try {
        const res = await api.get<StorageLocationRef[]>('/api/storage-locations');
        setStorageLocations(res.data);
      } catch {}
    })();
  }, []);

  // Sorting
  type Order = 'asc' | 'desc';
  type OrderBy = 'identifier' | keyof Pick<ItemListResponse, 'category' | 'status' | 'foundAt' | 'foundLocation' | 'details'>;
  const [orderBy, setOrderBy] = useState<OrderBy>('identifier');
  const [order, setOrder] = useState<Order>('desc');

  // Pagination (TablePagination is 0-based; API is 1-based)
  const [page, setPage] = useState<number>(0);
  const [rowsPerPage, setRowsPerPage] = useState<number>(10);

  const handleSort = (property: OrderBy) => () => {
    const isAsc = orderBy === property && order === 'asc';
    setOrder(isAsc ? 'desc' : 'asc');
    setOrderBy(property);
  };

  // Actions menu/dialog state (must be before any early returns)
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
  const [selected, setSelected] = useState<ItemListResponse | null>(null);
  const [openDocsDialog, setOpenDocsDialog] = useState(false);
  const [docs, setDocs] = useState<Array<{ id: string; fileName: string; size: number; createdAt: string }>>([]);

  const openMenu = (e: React.MouseEvent<HTMLElement>, item: ItemListResponse) => {
    setSelected(item);
    setMenuAnchor(e.currentTarget);
  };
  const closeMenu = () => setMenuAnchor(null);

  const [openSingleStorageTransfer, setOpenSingleStorageTransfer] = useState(false);
  const [openOfficeDialog, setOpenOfficeDialog] = useState(false);
  const [openOwnerDialog, setOpenOwnerDialog] = useState(false);
  const [openDisposalDialog, setOpenDisposalDialog] = useState(false);
  const RETENTION_DAYS = 90; // TODO: move to config

  // Bulk selection and feedback/dialog states (declare hooks before any early returns)
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [snack, setSnack] = useState<{ open: boolean; msg: string; severity: 'success'|'error'|'info'}>({ open: false, msg: '', severity: 'success' });
  const [openBulkOwner, setOpenBulkOwner] = useState(false);
  const [openBulkOffice, setOpenBulkOffice] = useState(false);
  const [openBulkStorage, setOpenBulkStorage] = useState(false);
  const [openBulkDispose, setOpenBulkDispose] = useState(false);
  const [openBulkDestroy, setOpenBulkDestroy] = useState(false);
  const [openSingleDestroy, setOpenSingleDestroy] = useState(false);
  const [openBulkReceive, setOpenBulkReceive] = useState(false);
  const [openSingleReceive, setOpenSingleReceive] = useState(false);
  const [openBulkSell, setOpenBulkSell] = useState(false);
  const [openSingleSell, setOpenSingleSell] = useState(false);

  const { data, isLoading, isError, refetch } = useQuery<ItemListResult>({
    queryKey: ['items', { q: debouncedSearch, category, status, storageLocationId, sortBy: orderBy, sortDir: order, page, rowsPerPage }],
    queryFn: () => fetchItems({ q: debouncedSearch, category, status, storageLocationId: storageLocationId || undefined, sortBy: orderBy, sortDir: order, page: page + 1, pageSize: rowsPerPage }),
  });

  const permsQuery = useQuery<PermissionsResponse>({
    queryKey: ['account-permissions'],
    queryFn: async () => (await api.get<PermissionsResponse>('/api/account/permissions')).data,
    staleTime: 5 * 60 * 1000,
  });
  const perms: PermissionsResponse = permsQuery.data ?? { handoverOwner: false, handoverOffice: false, transferStorage: false, receiveStorage: false, dispose: false, destroy: false, sell: false };

  if (isLoading) return <CircularProgress />;
  if (isError) return <Typography color="error">Hiba történt a betöltés közben.</Typography>;

  // Use server-side pagination results as-is to avoid page-size drift.
  // Apply only the optional local ID filter.
  const allItems = data?.items ?? [];
  const visibleItems = allItems.filter(i => {
    if (!debouncedIdFilter) return true;
    const ident = (i.depositNumber && i.depositSubIndex)
      ? `${i.depositNumber}/${i.depositSubIndex}`
      : (i.depositNumber || '');
    return ident.toLowerCase().includes(debouncedIdFilter.toLowerCase());
  });

  // Dropdown options
  const categories: string[] = Array.from(new Set(allItems.map((d) => d.category)));
  // Show all statuses regardless of what exists in current dataset
  const statuses: string[] = [
    'InStorage',
    'Transferred',
    'InTransit',
    'ReadyToDispose',
    'Claimed',
    'Disposed',
    'Sold',
  ];

  // Bulk selection state (derived values)
  const allVisibleIds = visibleItems.map(i => i.id);
  const allSelectedOnPage = allVisibleIds.length > 0 && allVisibleIds.every(id => selectedIds.includes(id));
  const someSelectedOnPage = allVisibleIds.some(id => selectedIds.includes(id));
  const selectedItems = (data?.items ?? []).filter(i => selectedIds.includes(i.id));
  const allSelectedAreInTransit = selectedItems.length > 0 && selectedItems.every(i => i.status === 'InTransit');

  const toggleSelectAllVisible = () => {
    if (allSelectedOnPage) {
      setSelectedIds(prev => prev.filter(id => !allVisibleIds.includes(id)));
    } else {
      setSelectedIds(prev => Array.from(new Set([...prev, ...allVisibleIds])));
    }
  };
  const toggleSelectOne = (id: string) => {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(x => x !== id) : [...prev, id]);
  };

  // Bulk dialog state and handlers
  const onBulkSuccess = async () => {
    setSnack({ open: true, msg: 'Művelet sikeres.', severity: 'success' });
    setSelectedIds([]);
    await refetch();
  };

  return (
    <Stack spacing={1}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5">Talált tárgyak</Typography>
        <Button variant="contained" component={RouterLink} to="/deposits/new">Új leadás</Button>
      </Stack>

      {/* Filters: Azonosító, Kategória, Leírás, Tárolás helye, Státusz */}
      <Paper sx={{ p: 1 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} alignItems={{ xs: 'stretch', md: 'flex-end' }}>
          <TextField
            size="small"
            label="Azonosító"
            value={idFilter}
            onChange={(e) => setIdFilter(e.target.value)}
            sx={{ minWidth: 180 }}
            InputLabelProps={{ shrink: true }}
          />
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel shrink>Kategória</InputLabel>
            <Select
              size="small"
              label="Kategória"
              input={<OutlinedInput notched label="Kategória" />}
              value={category}
              onChange={(e) => setCategory(e.target.value)}
            >
              <MenuItem value=""><em>Összes</em></MenuItem>
              {categories.map(c => (
                <MenuItem key={c} value={c}>{c}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <TextField
            size="small"
            label="Leírás"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            fullWidth
            InputLabelProps={{ shrink: true }}
          />
          <FormControl size="small" sx={{ minWidth: 220 }}>
            <InputLabel shrink>Tárolási hely</InputLabel>
            <Select
              size="small"
              label="Tárolási hely"
              input={<OutlinedInput notched label="Tárolási hely" />}
              value={storageLocationId}
              onChange={(e) => setStorageLocationId(e.target.value)}
            >
              <MenuItem value=""><em>Összes</em></MenuItem>
              {storageLocations.map(sl => (
                <MenuItem key={sl.id} value={sl.id}>{sl.name}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel shrink>Státusz</InputLabel>
            <Select
              size="small"
              label="Státusz"
              input={<OutlinedInput notched label="Státusz" />}
              value={status}
              onChange={(e) => setStatus(e.target.value)}
            >
              <MenuItem value=""><em>Összes</em></MenuItem>
              {statuses.map(s => (
                <MenuItem key={s} value={s}>{statusLabel(s)}</MenuItem>
              ))}
            </Select>
          </FormControl>
          <Box flexGrow={1} />
        </Stack>
      </Paper>

      {/* Bulk toolbar */}
      {selectedIds.length > 0 && (
        <Toolbar variant="dense" sx={{ bgcolor: 'action.hover', borderRadius: 1 }}>
          <Typography sx={{ mr: 2 }}>{selectedIds.length} kiválasztva</Typography>
          {allSelectedAreInTransit ? (
            perms.receiveStorage ? (
              <Button size="small" variant="contained" onClick={() => setOpenBulkReceive(true)}>Átvétel</Button>
            ) : null
          ) : (
            <>
              {perms.handoverOwner && (
                <>
                  <Button size="small" onClick={() => setOpenBulkOwner(true)} variant="contained">Átadás tulajdonosnak</Button>
                  <Box sx={{ width: 8 }} />
                </>
              )}
              {perms.handoverOffice && (
                <>
                  <Button size="small" onClick={() => setOpenBulkOffice(true)} variant="outlined">Átadás Okmányirodába</Button>
                  <Box sx={{ width: 8 }} />
                </>
              )}
              {perms.transferStorage && (
                <>
                  <Button size="small" onClick={() => setOpenBulkStorage(true)} variant="outlined">Tárolási hely módosítása</Button>
                  <Box sx={{ width: 8 }} />
                </>
              )}
              {perms.dispose && (
                <>
                  <Button size="small" color="error" onClick={() => setOpenBulkDispose(true)} variant="outlined">Selejtezés</Button>
                  <Box sx={{ width: 8 }} />
                </>
              )}
              {perms.destroy && (
                <>
                  <Button size="small" color="error" onClick={() => setOpenBulkDestroy(true)} variant="contained">Megsemmisítés</Button>
                  <Box sx={{ width: 8 }} />
                </>
              )}
              {perms.sell && (
                <Button size="small" onClick={() => setOpenBulkSell(true)} variant="contained">Értékesítés</Button>
              )}
            </>
          )}
        </Toolbar>
      )}

      {/* Grid Table */}
      <TableContainer component={Paper} sx={{ overflowX: 'auto' }}>
        <Table size="small" sx={{ '& .MuiTableCell-root': { py: 0.5, px: 1 } }}>
          <TableHead>
            <TableRow>
              <TableCell padding="checkbox">
                <Checkbox
                  color="primary"
                  indeterminate={someSelectedOnPage && !allSelectedOnPage}
                  checked={allSelectedOnPage}
                  onChange={toggleSelectAllVisible}
                />
              </TableCell>
              <TableCell sx={{ width: 140 }} sortDirection={orderBy === 'identifier' ? order : false}>
                <TableSortLabel
                  active={orderBy === 'identifier'}
                  direction={orderBy === 'identifier' ? order : 'desc'}
                  onClick={handleSort('identifier')}
                >
                  Azonosító
                </TableSortLabel>
              </TableCell>
              <TableCell sortDirection={orderBy === 'category' ? order : false}>
                <TableSortLabel
                  active={orderBy === 'category'}
                  direction={orderBy === 'category' ? order : 'asc'}
                  onClick={handleSort('category')}
                >
                  Kategória
                </TableSortLabel>
              </TableCell>
              <TableCell sortDirection={orderBy === 'details' ? order : false}>
                <TableSortLabel
                  active={orderBy === 'details'}
                  direction={orderBy === 'details' ? order : 'asc'}
                  onClick={handleSort('details')}
                >
                  Leírás
                </TableSortLabel>
              </TableCell>
              <TableCell>Megtaláló neve</TableCell>
              <TableCell sx={{ width: 120 }}>Rendszám</TableCell>
              <TableCell>Vonal / irány</TableCell>
              <TableCell sortDirection={orderBy === 'foundLocation' ? order : false} sx={{ width: 160 }}>
                <TableSortLabel
                  active={orderBy === 'foundLocation'}
                  direction={orderBy === 'foundLocation' ? order : 'asc'}
                  onClick={handleSort('foundLocation')}
                >
                  Megtalálás helye
                </TableSortLabel>
              </TableCell>
              <TableCell align="right" sortDirection={orderBy === 'foundAt' ? order : false}>
                <TableSortLabel
                  active={orderBy === 'foundAt'}
                  direction={orderBy === 'foundAt' ? order : 'desc'}
                  onClick={handleSort('foundAt')}
                >
                  Megtalálás időpontja
                </TableSortLabel>
              </TableCell>
              <TableCell>Tárolási hely</TableCell>
              <TableCell sortDirection={orderBy === 'status' ? order : false}>
                <TableSortLabel
                  active={orderBy === 'status'}
                  direction={orderBy === 'status' ? order : 'asc'}
                  onClick={handleSort('status')}
                >
                  Státusz
                </TableSortLabel>
              </TableCell>
              <TableCell align="center" sx={{ width: 56 }}>Műveletek</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {visibleItems.length > 0 ? (
              visibleItems.map((item) => (
                <TableRow key={item.id} hover selected={selectedIds.includes(item.id)}>
                  <TableCell padding="checkbox">
                    <Checkbox
                      color="primary"
                      checked={selectedIds.includes(item.id)}
                      onChange={() => toggleSelectOne(item.id)}
                    />
                  </TableCell>
                  <TableCell sx={{ width: 140 }}>
                    {item.depositNumber && item.depositSubIndex ? `${item.depositNumber}-${item.depositSubIndex}` : ''}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {item.category}{item.category === 'Egyéb' && item.otherCategoryText ? ` - ${item.otherCategoryText}` : ''}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ maxWidth: 1000 }}>
                    <Typography variant="body2" color="text.secondary" noWrap title={item.details ?? ''}>
                      {item.details ?? ''}
                    </Typography>
                  </TableCell>
                  <TableCell>{item.finderName ?? ''}</TableCell>
                  <TableCell sx={{ width: 120 }}>
                    <Typography variant="body2" color="text.secondary" noWrap title={item.licensePlate ?? ''}>
                      {item.licensePlate ?? ''}
                    </Typography>
                  </TableCell>
                  <TableCell>{item.busLineName ?? ''}</TableCell>
                  <TableCell sx={{ maxWidth: 180 }}>
                    <Typography variant="body2" color="text.secondary" noWrap title={item.foundLocation ?? ''}>
                      {item.foundLocation ?? ''}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">{item.foundAt ? new Date(item.foundAt).toLocaleDateString('hu-HU') : '-'}</TableCell>
                  <TableCell>{item.storageLocationName ?? ''}</TableCell>
                  <TableCell>{statusLabel(item.status)}</TableCell>
                  <TableCell align="center">
                    <Tooltip title="Műveletek">
                      <IconButton size="small" onClick={(e) => openMenu(e, item)}>
                        <MoreVertIcon />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={7}>
                  <Typography align="center">Nincs megjeleníthető tárgy.</Typography>
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Pagination */}
      <Box display="flex" justifyContent="flex-end">
        <TablePagination
          component="div"
          count={data?.total ?? 0}
          page={page}
          onPageChange={(_, newPage: number) => setPage(newPage)}
          rowsPerPage={rowsPerPage}
          onRowsPerPageChange={(e: any) => { setRowsPerPage(parseInt(e.target.value, 10)); setPage(0); }}
          rowsPerPageOptions={[5, 10, 25, 50]}
          labelRowsPerPage="Sor/oldal"
        />
      </Box>

      {/* Actions Menu */}
      {(() => {
        const items: ReactNode[] = [];
        if (selected?.status === 'InTransit') {
          if (perms.receiveStorage) items.push(
            <MenuItem key="receive" onClick={() => { setOpenSingleReceive(true); closeMenu(); }}>Átvétel</MenuItem>
          );
        } else {
          if (perms.transferStorage) items.push(
            <MenuItem key="transfer" onClick={() => { setOpenSingleStorageTransfer(true); closeMenu(); }}>Tárolási hely módosítása</MenuItem>
          );
          if (perms.handoverOffice) items.push(
            <MenuItem key="office" onClick={() => { setOpenOfficeDialog(true); closeMenu(); }}>Átadás Okmányirodába</MenuItem>
          );
          if (perms.handoverOwner) items.push(
            <MenuItem key="owner" onClick={() => { setOpenOwnerDialog(true); closeMenu(); }}>Átadás tulajdonosnak</MenuItem>
          );
          if (perms.dispose) items.push(
            <MenuItem key="dispose" onClick={() => { setOpenDisposalDialog(true); closeMenu(); }}>Selejtezés</MenuItem>
          );
          if (perms.destroy) items.push(
            <MenuItem key="destroy" onClick={() => { setOpenSingleDestroy(true); closeMenu(); }}>Megsemmisítés</MenuItem>
          );
          if (perms.sell) items.push(
            <MenuItem key="sell" onClick={() => { setOpenSingleSell(true); closeMenu(); }}>Értékesítés</MenuItem>
          );

          // Top-level: Deposit documents selector
          if (selected?.depositNumber) items.push(
            <MenuItem key="docs" onClick={async () => {
              try {
                const number = selected?.depositNumber; if (!number) return;
                const depRes = await api.get(`/api/deposits/by-number/${encodeURIComponent(number)}`);
                const id = (depRes.data as any)?.id as string | undefined;
                if (!id) return;
                const res = await api.get(`/api/deposits/${id}/documents`);
                const list = (res.data as any[])
                  .map(d => ({ id: d.id as string, fileName: d.fileName as string, size: d.size as number, createdAt: d.createdAt as string }))
                  .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
                setDocs(list);
                setOpenDocsDialog(true);
              } catch {}
              closeMenu();
            }}>Nyomtatványok…</MenuItem>
          );
          // Removed nested print submenu and specific print actions
        }
        return (
          <Menu anchorEl={menuAnchor} open={Boolean(menuAnchor)} onClose={closeMenu}>
            {items}
          </Menu>
        );
      })()}

      {/* Deposit Documents Dialog */}
      <Dialog open={openDocsDialog} onClose={() => setOpenDocsDialog(false)} fullWidth maxWidth="sm">
        <DialogTitle>Leadáshoz tartozó nyomtatványok</DialogTitle>
        <DialogContent dividers>
          {docs.length === 0 ? (
            <Typography>Nincs elérhető nyomtatvány.</Typography>
          ) : (
            <List>
              {docs.map(d => (
                <ListItem key={d.id} secondaryAction={
                  <Stack direction="row" spacing={1}>
                    <Button size="small" variant="outlined" onClick={async () => {
                      const pdfRes = await api.get(`/api/deposits/documents/${encodeURIComponent(d.id)}`, { responseType: 'blob', params: { t: Date.now() } });
                      const url = URL.createObjectURL(new Blob([pdfRes.data], { type: 'application/pdf' }));
                      window.open(url, '_blank');
                      setTimeout(() => URL.revokeObjectURL(url), 60_000);
                      setOpenDocsDialog(false);
                    }}>Nyomtatás</Button>
                    <Button size="small" variant="contained" onClick={async () => {
                      const pdfRes = await api.get(`/api/deposits/documents/${encodeURIComponent(d.id)}`, { responseType: 'blob', params: { t: Date.now(), download: true } });
                      const blob = new Blob([pdfRes.data], { type: 'application/pdf' });
                      const url = URL.createObjectURL(blob);
                      const a = document.createElement('a');
                      a.href = url;
                      a.download = d.fileName || 'dokumentum.pdf';
                      document.body.appendChild(a);
                      a.click();
                      a.remove();
                      setTimeout(() => URL.revokeObjectURL(url), 60_000);
                      setOpenDocsDialog(false);
                    }}>Letöltés</Button>
                  </Stack>
                }>
                  <ListItemText primary={d.fileName} secondary={new Date(d.createdAt).toLocaleString('hu-HU')} />
                </ListItem>
              ))}
            </List>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDocsDialog(false)}>Mégse</Button>
        </DialogActions>
      </Dialog>

      {/* Action Dialogs */}
      <BulkStorageTransferDialog
        open={openSingleStorageTransfer}
        onClose={() => setOpenSingleStorageTransfer(false)}
        itemIds={selected ? [selected.id] : []}
        onSuccess={onBulkSuccess}
      />
      <OfficeHandoverDialog
        open={openOfficeDialog}
        onClose={() => setOpenOfficeDialog(false)}
        item={selected ? { id: selected.id } : null}
      />
      <OwnerHandoverDialog
        open={openOwnerDialog}
        onClose={() => setOpenOwnerDialog(false)}
        item={selected ? { id: selected.id } : null}
      />
      <DisposalDialog
        open={openDisposalDialog}
        onClose={() => setOpenDisposalDialog(false)}
        item={selected ? { id: selected.id } : null}
        retentionDays={RETENTION_DAYS}
      />
      {/* Bulk dialogs */}
      <BulkDestroyDialog
        open={openSingleDestroy}
        onClose={() => setOpenSingleDestroy(false)}
        itemIds={selected ? [selected.id] : []}
        onSuccess={onBulkSuccess}
      />
      <BulkOwnerHandoverDialog
        open={openBulkOwner}
        onClose={() => setOpenBulkOwner(false)}
        itemIds={selectedIds}
        onSuccess={onBulkSuccess}
      />
      <BulkOfficeHandoverDialog
        open={openBulkOffice}
        onClose={() => setOpenBulkOffice(false)}
        itemIds={selectedIds}
        onSuccess={onBulkSuccess}
      />
      <BulkStorageTransferDialog
        open={openBulkStorage}
        onClose={() => setOpenBulkStorage(false)}
        itemIds={selectedIds}
        onSuccess={onBulkSuccess}
      />
      <BulkDisposalDialog
        open={openBulkDispose}
        onClose={() => setOpenBulkDispose(false)}
        itemIds={selectedIds}
        onSuccess={onBulkSuccess}
      />
      <BulkDestroyDialog
        open={openBulkDestroy}
        onClose={() => setOpenBulkDestroy(false)}
        itemIds={selectedIds}
        onSuccess={onBulkSuccess}
      />
      <BulkReceiveStorageDialog
        open={openBulkReceive}
        onClose={() => setOpenBulkReceive(false)}
        itemIds={selectedIds}
        onSuccess={onBulkSuccess}
      />
      <BulkReceiveStorageDialog
        open={openSingleReceive}
        onClose={() => setOpenSingleReceive(false)}
        itemIds={selected ? [selected.id] : []}
        onSuccess={onBulkSuccess}
      />
      <BulkSellDialog
        open={openBulkSell}
        onClose={() => setOpenBulkSell(false)}
        itemIds={selectedIds}
        onSuccess={onBulkSuccess}
      />
      <BulkSellDialog
        open={openSingleSell}
        onClose={() => setOpenSingleSell(false)}
        itemIds={selected ? [selected.id] : []}
        onSuccess={onBulkSuccess}
      />

      <Snackbar open={snack.open} autoHideDuration={4000} onClose={() => setSnack(s => ({ ...s, open: false }))}>
        <Alert severity={snack.severity} onClose={() => setSnack(s => ({ ...s, open: false }))}>{snack.msg}</Alert>
      </Snackbar>
  </Stack>
  );
}
