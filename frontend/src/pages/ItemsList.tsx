import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Box,
  Button,
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
} from '@mui/material';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import { Link as RouterLink } from 'react-router-dom';
import api from '../api/client';
import StorageTransferDialog from '../components/actions/StorageTransferDialog';
import OfficeHandoverDialog from '../components/actions/OfficeHandoverDialog';
import OwnerHandoverDialog from '../components/actions/OwnerHandoverDialog';
import DisposalDialog from '../components/actions/DisposalDialog';

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
};

type ItemListResult = {
  items: ItemListResponse[];
  total: number;
};

async function fetchItems(params: {
  q: string;
  category: string;
  status: string;
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
      case 'InStorage': return 'Tárolásban';
      case 'Transferred': return 'Átadva';
      case 'Claimed': return 'Tulajdonosnak átadva';
      case 'Disposed': return 'Selejtezve';
      default: return s;
    }
  };
  // Filters
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [idFilter, setIdFilter] = useState('');
  const [debouncedIdFilter, setDebouncedIdFilter] = useState('');

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

  // Sorting
  type Order = 'asc' | 'desc';
  type OrderBy = keyof Pick<ItemListResponse, 'category' | 'status' | 'foundAt' | 'foundLocation' | 'details'>;
  const [orderBy, setOrderBy] = useState<OrderBy>('foundAt');
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

  const openMenu = (e: React.MouseEvent<HTMLElement>, item: ItemListResponse) => {
    setSelected(item);
    setMenuAnchor(e.currentTarget);
  };
  const closeMenu = () => setMenuAnchor(null);

  const [openStorageDialog, setOpenStorageDialog] = useState(false);
  const [openOfficeDialog, setOpenOfficeDialog] = useState(false);
  const [openOwnerDialog, setOpenOwnerDialog] = useState(false);
  const [openDisposalDialog, setOpenDisposalDialog] = useState(false);
  const RETENTION_DAYS = 90; // TODO: move to config

  const { data, isLoading, isError } = useQuery<ItemListResult>({
    queryKey: ['items', { q: debouncedSearch, category, status, sortBy: orderBy, sortDir: order, page, rowsPerPage }],
    queryFn: () => fetchItems({ q: debouncedSearch, category, status, sortBy: orderBy, sortDir: order, page: page + 1, pageSize: rowsPerPage }),
  });

  if (isLoading) return <CircularProgress />;
  if (isError) return <Typography color="error">Hiba történt a betöltés közben.</Typography>;

  // Default behavior: when no status filter is selected, hide 'Claimed' from the list.
  // When a status is selected (including 'Claimed'), show according to the selected status (server already filters).
  const allItems = data?.items ?? [];
  const baseItems = status ? allItems : allItems.filter(i => i.status !== 'Claimed');
  const visibleItems = baseItems.filter(i => {
    if (!debouncedIdFilter) return true;
    const ident = (i.depositNumber && i.depositSubIndex)
      ? `${i.depositNumber}-${i.depositSubIndex}`
      : '';
    return ident.toLowerCase().includes(debouncedIdFilter.trim().toLowerCase());
  });

  // Derive dropdown options: include all statuses so 'Claimed' can be selected.
  const categories: string[] = Array.from(new Set(allItems.map((d) => d.category)));
  const statuses: string[] = Array.from(new Set(allItems.map((d) => d.status)));

  return (
    <Stack spacing={2}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5">Talált tárgyak</Typography>
        <Button variant="contained" component={RouterLink} to="/deposits/new">Új leadás</Button>
      </Stack>

      {/* Filters */}
      <Paper sx={{ p: 2 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ xs: 'stretch', md: 'flex-end' }}>
          <TextField
            size="small"
            label="Leírás"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            autoFocus
            fullWidth
            InputLabelProps={{ shrink: true }}
          />
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

      {/* Grid Table */}
      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Azonosító</TableCell>
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
              <TableCell>Rendszám</TableCell>
              <TableCell>Vonal / irány</TableCell>
              <TableCell sortDirection={orderBy === 'foundLocation' ? order : false}>
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
                <TableRow key={item.id} hover>
                  <TableCell>
                    {item.depositNumber && item.depositSubIndex ? `${item.depositNumber}-${item.depositSubIndex}` : ''}
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" color="text.secondary">
                      {item.category}{item.category === 'Egyéb' && item.otherCategoryText ? ` - ${item.otherCategoryText}` : ''}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ maxWidth: 360 }}>
                    <Typography variant="body2" color="text.secondary" noWrap title={item.details ?? ''}>
                      {item.details ?? ''}
                    </Typography>
                  </TableCell>
                  <TableCell>{item.finderName ?? ''}</TableCell>
                  <TableCell>{item.licensePlate ?? ''}</TableCell>
                  <TableCell>{item.busLineName ?? ''}</TableCell>
                  <TableCell>
                    {item.foundLocation ?? ''}
                  </TableCell>
                  <TableCell align="right">{item.foundAt ? new Date(item.foundAt).toLocaleString('hu-HU') : '-'}</TableCell>
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
          count={visibleItems.length}
          page={page}
          onPageChange={(_, newPage: number) => setPage(newPage)}
          rowsPerPage={rowsPerPage}
          onRowsPerPageChange={(e: any) => { setRowsPerPage(parseInt(e.target.value, 10)); setPage(0); }}
          rowsPerPageOptions={[5, 10, 25, 50]}
          labelRowsPerPage="Sor/oldal"
        />
      </Box>

      {/* Actions Menu */}
      <Menu anchorEl={menuAnchor} open={Boolean(menuAnchor)} onClose={closeMenu}>
        <MenuItem onClick={() => { setOpenStorageDialog(true); closeMenu(); }}>Tárolási hely módosítása</MenuItem>
        <MenuItem onClick={() => { setOpenOfficeDialog(true); closeMenu(); }}>Átadás Okmányirodába</MenuItem>
        <MenuItem onClick={() => { setOpenOwnerDialog(true); closeMenu(); }}>Átadás tulajdonosnak</MenuItem>
        <MenuItem onClick={() => { setOpenDisposalDialog(true); closeMenu(); }}>Selejtezés</MenuItem>
      </Menu>

      {/* Action Dialogs */}
      <StorageTransferDialog
        open={openStorageDialog}
        onClose={() => setOpenStorageDialog(false)}
        item={selected ? { id: selected.id } : null}
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
  </Stack>
  );
}
