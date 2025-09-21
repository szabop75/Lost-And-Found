import { useEffect, useState } from 'react';
import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, Paper, Stack, Switch, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import EditIcon from '@mui/icons-material/Edit';
import api from '../api/client';

type DenomDto = { id: string; valueMinor: number; label: string; sortOrder: number; isActive: boolean };
type CurrencyDto = { id: string; code: string; name: string; isActive: boolean; sortOrder: number; denominations: DenomDto[] };

export default function AdminCurrencies() {
  const [currencies, setCurrencies] = useState<CurrencyDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await api.get<CurrencyDto[]>('/api/admin/currencies');
      setCurrencies(res.data);
    } catch {
      setError('Nem sikerült betölteni a pénznemeket.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  // Currency dialog state
  const [openCur, setOpenCur] = useState(false);
  const [editCurId, setEditCurId] = useState<string | null>(null);
  const [curCode, setCurCode] = useState('');
  const [curName, setCurName] = useState('');
  const [curActive, setCurActive] = useState(true);
  const [curSort, setCurSort] = useState<string>('');

  const startAddCurrency = () => { setEditCurId(null); setCurCode(''); setCurName(''); setCurActive(true); setCurSort(''); setOpenCur(true); };
  const startEditCurrency = (c: CurrencyDto) => { setEditCurId(c.id); setCurCode(c.code); setCurName(c.name); setCurActive(c.isActive); setCurSort(String(c.sortOrder)); setOpenCur(true); };

  const saveCurrency = async () => {
    try {
      if (!curCode || !curName) return;
      const payload = { code: curCode, name: curName, isActive: curActive, sortOrder: parseInt(curSort || '0', 10) };
      if (editCurId) await api.put(`/api/admin/currencies/${editCurId}`, payload);
      else await api.post('/api/admin/currencies', payload);
      setOpenCur(false);
      await load();
    } catch {
      alert('Mentés sikertelen');
    }
  };

  const deleteCurrency = async (id: string) => {
    if (!confirm('Biztosan törlöd a pénznemet?')) return;
    try { await api.delete(`/api/admin/currencies/${id}`); await load(); } catch { alert('Törlés sikertelen'); }
  };

  // Denomination dialog state
  const [openDen, setOpenDen] = useState(false);
  const [denCurrencyId, setDenCurrencyId] = useState<string>('');
  const [editDenId, setEditDenId] = useState<string | null>(null);
  const [denValueMinor, setDenValueMinor] = useState<string>('');
  const [denLabel, setDenLabel] = useState<string>('');
  const [denSort, setDenSort] = useState<string>('');
  const [denActive, setDenActive] = useState<boolean>(true);

  const startAddDenom = (currencyId: string) => {
    setDenCurrencyId(currencyId);
    setEditDenId(null);
    setDenValueMinor('');
    setDenLabel('');
    setDenSort('');
    setDenActive(true);
    setOpenDen(true);
  };
  const startEditDenom = (currencyId: string, d: DenomDto) => {
    setDenCurrencyId(currencyId);
    setEditDenId(d.id);
    // Show value in MAJOR units for currencies with minor factor (e.g., EUR -> 2 decimals), with comma as decimal separator
    const cur = currencies.find(c => c.id === currencyId);
    const factor = cur?.code === 'HUF' ? 1 : 100;
    const major = d.valueMinor / factor;
    const display = factor === 1 ? String(d.valueMinor) : String(major).replace('.', ',');
    setDenValueMinor(display);
    setDenLabel(d.label);
    setDenSort(String(d.sortOrder));
    setDenActive(d.isActive);
    setOpenDen(true);
  };

  const saveDenom = async () => {
    try {
      const cur = currencies.find(c => c.id === denCurrencyId);
      const factor = cur?.code === 'HUF' ? 1 : 100;
      // Accept comma or dot decimal, convert to minor integer
      const raw = (denValueMinor || '').trim().replace(',', '.');
      const numeric = raw === '' ? 0 : Number(raw);
      const valueMinor = factor === 1 ? Math.round(Number.isFinite(numeric) ? numeric : 0) : Math.round((Number.isFinite(numeric) ? numeric : 0) * factor);
      const payload = { valueMinor, label: denLabel, sortOrder: parseInt(denSort || '0', 10), isActive: denActive };
      if (editDenId) await api.put(`/api/admin/currencies/${denCurrencyId}/denominations/${editDenId}`, payload);
      else await api.post(`/api/admin/currencies/${denCurrencyId}/denominations`, payload);
      setOpenDen(false);
      await load();
    } catch {
      alert('Címlet mentése sikertelen');
    }
  };

  const deleteDenom = async (currencyId: string, denomId: string) => {
    if (!confirm('Biztosan törlöd a címletet?')) return;
    try { await api.delete(`/api/admin/currencies/${currencyId}/denominations/${denomId}`); await load(); } catch { alert('Címlet törlése sikertelen'); }
  };

  return (
    <Stack spacing={2}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5">Pénznemek és címletek</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={startAddCurrency}>Új pénznem</Button>
      </Stack>

      {error && <Typography color="error">{error}</Typography>}
      {loading && <Typography variant="body2">Betöltés…</Typography>}

      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Kód</TableCell>
              <TableCell>Név</TableCell>
              <TableCell>Aktív</TableCell>
              <TableCell>Rendezés</TableCell>
              <TableCell>Címletek</TableCell>
              <TableCell align="right">Műveletek</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {currencies.map(c => (
              <TableRow key={c.id}>
                <TableCell>{c.code}</TableCell>
                <TableCell>{c.name}</TableCell>
                <TableCell>{c.isActive ? 'Igen' : 'Nem'}</TableCell>
                <TableCell>{c.sortOrder}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={1} alignItems="center">
                    <Button size="small" onClick={() => startAddDenom(c.id)} startIcon={<AddIcon />}>Új címlet</Button>
                  </Stack>
                  <Table size="small" sx={{ mt: 1 }}>
                    <TableHead>
                      <TableRow>
                        <TableCell>Label</TableCell>
                        <TableCell>Érték</TableCell>
                        <TableCell>Aktív</TableCell>
                        <TableCell>Rendezés</TableCell>
                        <TableCell align="right">Műveletek</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {c.denominations.map(d => {
                        const factor = c.code === 'HUF' ? 1 : 100;
                        const display = factor === 1
                          ? String(d.valueMinor)
                          : (d.valueMinor / factor).toLocaleString('hu-HU', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                        return (
                          <TableRow key={d.id}>
                            <TableCell>{d.label}</TableCell>
                            <TableCell>{display}</TableCell>
                        
                          <TableCell>{d.isActive ? 'Igen' : 'Nem'}</TableCell>
                          <TableCell>{d.sortOrder}</TableCell>
                          <TableCell align="right">
                            <IconButton size="small" onClick={() => startEditDenom(c.id, d)}><EditIcon fontSize="small" /></IconButton>
                            <IconButton size="small" color="error" onClick={() => deleteDenom(c.id, d.id)}><DeleteIcon fontSize="small" /></IconButton>
                          </TableCell>
                          </TableRow>
                        );
                      })}
                    </TableBody>
                  </Table>
                </TableCell>
                <TableCell align="right">
                  <IconButton size="small" onClick={() => startEditCurrency(c)}><EditIcon fontSize="small" /></IconButton>
                  <IconButton size="small" color="error" onClick={() => deleteCurrency(c.id)}><DeleteIcon fontSize="small" /></IconButton>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Currency dialog */}
      <Dialog open={openCur} onClose={() => setOpenCur(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editCurId ? 'Pénznem szerkesztése' : 'Új pénznem'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField label="Kód" value={curCode} onChange={e => setCurCode(e.target.value.toUpperCase())} inputProps={{ maxLength: 8 }} />
            <TextField label="Név" value={curName} onChange={e => setCurName(e.target.value)} />
            <TextField type="number" label="Rendezés" value={curSort} onChange={e => setCurSort(e.target.value)} />
            <Box>
              <Switch checked={curActive} onChange={e => setCurActive(e.target.checked)} /> Aktív
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenCur(false)}>Mégse</Button>
          <Button onClick={saveCurrency} variant="contained">Mentés</Button>
        </DialogActions>
      </Dialog>

      {/* Denomination dialog */}
      <Dialog open={openDen} onClose={() => setOpenDen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{editDenId ? 'Címlet szerkesztése' : 'Új címlet'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField label="Érték" value={denValueMinor} onChange={e => setDenValueMinor(e.target.value)} helperText="Decimális érték megengedett (vesszővel). HUF: egész, EUR: tizedes." />
            <TextField label="Felirat" value={denLabel} onChange={e => setDenLabel(e.target.value)} />
            <TextField type="number" label="Rendezés" value={denSort} onChange={e => setDenSort(e.target.value)} />
            <Box>
              <Switch checked={denActive} onChange={e => setDenActive(e.target.checked)} /> Aktív
            </Box>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDen(false)}>Mégse</Button>
          <Button onClick={saveDenom} variant="contained">Mentés</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
