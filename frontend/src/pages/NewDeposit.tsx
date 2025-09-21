import { useEffect, useRef, useState } from 'react';
import { Box, Button, Container, FormControl, InputLabel, MenuItem, Paper, Select, TextField, Typography, IconButton, Divider, Stack, OutlinedInput } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import { useNavigate } from 'react-router-dom';
import api from '../api/client';

const CATEGORIES = [
  'Irat',
  'Mobiltelefon',
  'Pénztárca',
  'Kulcs',
  'Ruházat',
  'Készpénz',
  'Egyéb'
];

type DepositItem = {
  category: string;
  otherCategoryText?: string;
  details: string;
  cashCurrencyId?: string; // only if category is Készpénz
  cashDenoms?: Record<string, string>; // denomId -> count (string for better input UX)
};

// deposit-level cash block removed; cash is now per-item for 'Készpénz'

export default function NewDeposit() {
  const navigate = useNavigate();

  // Leadás adatai (leadó + megtalálás helye/időpont)
  const [finderName, setFinderName] = useState('');
  const [finderAddress, setFinderAddress] = useState('');
  const [finderEmail, setFinderEmail] = useState('');
  const [finderPhone, setFinderPhone] = useState('');
  const [finderIdNumber, setFinderIdNumber] = useState('');
  const [foundLocation, setFoundLocation] = useState('');
  const [foundAt, setFoundAt] = useState('');
  const foundAtRef = useRef<HTMLInputElement | null>(null);

  // New: vehicle license and line selection
  const [licensePlate, setLicensePlate] = useState('');
  type LineRef = { id: string; name: string; sortOrder: number; active: boolean };
  const [lines, setLines] = useState<LineRef[]>([]);
  const [lineId, setLineId] = useState('');
  // New: drivers (for code dropdown)
  type DriverRef = { id: string; code: string; name: string; active: boolean };
  const [drivers, setDrivers] = useState<DriverRef[]>([]);
  const [driverId, setDriverId] = useState('');
  const [nameAutoFilled, setNameAutoFilled] = useState(false);
  type VehicleRef = { id: string; licensePlate: string; active: boolean };
  const [vehicles, setVehicles] = useState<VehicleRef[]>([]);
  const [vehicleId, setVehicleId] = useState('');

  // Items
  const [items, setItems] = useState<DepositItem[]>([
    { category: 'Irat', details: '' }
  ]);

  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  // Currencies reference
  type DenomRef = { id: string; valueMinor: number; label: string; sortOrder: number };
  type CurrencyRef = { id: string; code: string; name: string; denominations: DenomRef[] };
  const [currencies, setCurrencies] = useState<CurrencyRef[]>([]);

  useEffect(() => {
    (async () => {
      try {
        const res = await api.get<CurrencyRef[]>('/api/reference/currencies');
        setCurrencies(res.data);
      } catch {
        // ignore for now
      }
    })();
  }, []);

  // Load lines for selection
  useEffect(() => {
    (async () => {
      try {
        const res = await api.get<LineRef[]>('/api/lines');
        setLines(res.data);
      } catch {
        // ignore load error
      }
    })();
  }, []);

  // Load vehicles for selection
  useEffect(() => {
    (async () => {
      try {
        const res = await api.get<VehicleRef[]>('/api/vehicles');
        setVehicles(res.data);
      } catch {
        // ignore load error
      }
    })();
  }, []);

  // Load drivers for selection
  useEffect(() => {
    (async () => {
      try {
        const res = await api.get<DriverRef[]>('/api/drivers');
        setDrivers(res.data);
      } catch {
        // ignore load error (endpoint might be admin-only)
      }
    })();
  }, []);

  const addItem = () => setItems(prev => [...prev, { category: 'Irat', details: '' }]);
  const removeItem = (idx: number) => setItems(prev => prev.filter((_, i) => i !== idx));

  const findCurrency = (id?: string) => currencies.find(c => c.id === id);
  const currencyFactor = (code?: string) => (code === 'HUF' ? 1 : 100);
  const formatTotal = (minorTotal: number, code?: string) => {
    const factor = currencyFactor(code);
    if (factor === 1) {
      const formatted = new Intl.NumberFormat('hu-HU').format(minorTotal);
      return `Összesen: ${formatted} ${code ?? ''}`.trim();
    } else {
      const major = minorTotal / factor;
      const formatted = new Intl.NumberFormat('hu-HU', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(major);
      return `Összesen: ${formatted} ${code ?? ''}`.trim();
    }
  };

  const recomputeCashDetails = (it: DepositItem): string => {
    const cur = findCurrency(it.cashCurrencyId);
    if (!cur || !it.cashDenoms) return it.details;
    const denomsMap = new Map(cur.denominations.map(d => [d.id, d.valueMinor] as const));
    let totalMinor = 0;
    for (const [denId, countStr] of Object.entries(it.cashDenoms)) {
      const val = denomsMap.get(denId) ?? 0;
      const count = parseInt(countStr || '0', 10);
      if (Number.isFinite(count) && count > 0) totalMinor += val * count;
    }
    return formatTotal(totalMinor, cur.code);
  };

  const updateItem = (idx: number, patch: Partial<DepositItem>) => {
    setItems(prev => prev.map((it, i) => {
      if (i !== idx) return it;
      const updated = { ...it, ...patch } as DepositItem;
      if (updated.category === 'Készpénz') {
        updated.details = recomputeCashDetails(updated);
      }
      return updated;
    }));
  };

  const setItemCategory = (idx: number, category: string) => {
    setItems(prev => prev.map((it, i) => {
      if (i !== idx) return it;
      const updated: DepositItem = { ...it, category };
      if (category === 'Készpénz') {
        const first = currencies[0];
        updated.cashCurrencyId = updated.cashCurrencyId || first?.id;
        // initialize denoms map to zeros for selected currency
        const denoms = (currencies.find(c => c.id === updated.cashCurrencyId)?.denominations) || first?.denominations || [];
        const map: Record<string, string> = {};
        denoms.forEach(d => { map[d.id] = updated.cashDenoms?.[d.id] ?? ''; });
        updated.cashDenoms = map;
        const cur = findCurrency(updated.cashCurrencyId);
        updated.details = formatTotal(0, cur?.code);
      } else {
        delete updated.cashCurrencyId;
        delete updated.cashDenoms;
      }
      return updated;
    }));
  };

  const setItemCashCurrency = (idx: number, currencyId: string) => {
    setItems(prev => prev.map((it, i) => {
      if (i !== idx) return it;
      const denoms = currencies.find(c => c.id === currencyId)?.denominations || [];
      const map: Record<string, string> = {};
      denoms.forEach(d => { map[d.id] = ''; });
      const updated: DepositItem = { ...it, cashCurrencyId: currencyId, cashDenoms: map };
      updated.details = recomputeCashDetails(updated);
      return updated;
    }));
  };

  const setItemCashDenom = (idx: number, denomId: string, countStr: string) => {
    setItems(prev => prev.map((it, i) => {
      if (i !== idx) return it;
      const map = { ...(it.cashDenoms || {}) } as Record<string, string>;
      // Allow empty string during edit; validate on submit
      map[denomId] = countStr;
      const updated: DepositItem = { ...it, cashDenoms: map };
      updated.details = recomputeCashDetails(updated);
      return updated;
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!finderName || finderName.trim().length === 0) { setError('A Név mező kötelező.'); return; }
    if (items.length === 0) { setError('Legalább egy tárgy szükséges.'); return; }
    for (const it of items) {
      if (!it.details) { setError('Minden tételnél kötelező a leírás.'); return; }
    }
    try {
      setSubmitting(true);
      const payload: any = {
        finderName: finderName || null,
        finderAddress: finderAddress || null,
        finderEmail: finderEmail || null,
        finderPhone: finderPhone || null,
        finderIdNumber: finderIdNumber || null,
        foundLocation: foundLocation || null,
        foundAt: foundAt ? new Date(Number(foundAt.split('-')[0]), Number(foundAt.split('-')[1]) - 1, Number(foundAt.split('-')[2])).toISOString() : null,
        licensePlate: licensePlate || null,
        busLineId: lineId || null,
        items: items.map(it => {
          const base: any = {
            category: it.category,
            otherCategoryText: it.category === 'Egyéb' ? (it.otherCategoryText || null) : it.otherCategoryText || null,
            details: it.details,
          };
          if (it.category === 'Készpénz' && it.cashCurrencyId && it.cashDenoms) {
            const entries = Object.entries(it.cashDenoms)
              .map(([denomId, cntStr]) => ({ denomId, count: parseInt(cntStr || '0', 10) }))
              .filter(e => Number.isFinite(e.count) && e.count > 0)
              .map(e => ({ currencyDenominationId: e.denomId, count: e.count }));
            if (entries.length > 0) {
              base.cash = { currencyId: it.cashCurrencyId, entries };
            }
          }
          return base;
        })
      };

      console.debug('Creating deposit with payload:', payload);
      const res = await api.post('/api/deposits', payload);
      const depNumber = res.data?.depositNumber as string | undefined;
      navigate('/');
      if (depNumber) {
        // Optionally, could route to a deposit summary page later
        console.log('Deposit created:', depNumber);
      }
    } catch (err: any) {
      const serverMsg = err?.response?.data;
      setError(typeof serverMsg === 'string' && serverMsg ? serverMsg : 'Hiba történt a leadás rögzítése során.');
      console.error('Deposit create failed:', err?.response ?? err);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h5" gutterBottom>Új leadás</Typography>
        <Box component="form" onSubmit={handleSubmit}>

          <Typography variant="h6" sx={{ mt: 2, mb: 1 }}>Leadás adatai</Typography>
          <Box sx={{
            display: 'grid',
            gap: 2,
            gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }
          }}>
            {/* Left: nested grid (Törzsszám + Név = together width of one column), Right: Lakcím */}
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 2fr' }, gap: 2 }}>
              <FormControl variant="outlined" size="small" fullWidth>
                <InputLabel id="driver-code-label" shrink>Törzsszám</InputLabel>
                <Select
                  size="small"
                  labelId="driver-code-label"
                  id="driver-code-select"
                  label="Törzsszám"
                  value={driverId}
                  input={<OutlinedInput notched label="Törzsszám" />}
                  displayEmpty
                  renderValue={(val) => {
                    const d = drivers.find(x => x.id === val);
                    return d ? d.code : '';
                  }}
                  onChange={(e) => {
                    const id = e.target.value as string;
                    setDriverId(id);
                    if (id) {
                      const d = drivers.find(x => x.id === id);
                      if (d) { setFinderName(d.name); setNameAutoFilled(true); }
                    } else {
                      if (nameAutoFilled) {
                        setFinderName('');
                        setNameAutoFilled(false);
                      }
                    }
                  }}
                >
                  <MenuItem value=""><em>—</em></MenuItem>
                  {drivers.map(d => (
                    <MenuItem key={d.id} value={d.id}>{d.code} - {d.name}</MenuItem>
                  ))}
                </Select>
              </FormControl>
              <TextField size="small" required fullWidth label="Név" value={finderName} onChange={e => { setFinderName(e.target.value); setNameAutoFilled(false); }} InputLabelProps={{ shrink: true }} />
            </Box>
            <TextField size="small" fullWidth label="Lakcím" value={finderAddress} onChange={e => setFinderAddress(e.target.value)} InputLabelProps={{ shrink: true }} />
            <TextField size="small" fullWidth label="E-mail" value={finderEmail} onChange={e => setFinderEmail(e.target.value)} InputLabelProps={{ shrink: true }} />
            <TextField size="small" fullWidth label="Telefonszám" value={finderPhone} onChange={e => setFinderPhone(e.target.value)} InputLabelProps={{ shrink: true }} />
            <TextField size="small" fullWidth label="Igazolványszám" value={finderIdNumber} onChange={e => setFinderIdNumber(e.target.value)} InputLabelProps={{ shrink: true }} />
            {/* spacer to keep right column empty on this row (md+) */}
            <Box sx={{ display: { xs: 'none', md: 'block' } }} />

            {/* New row: two sibling children in parent grid */}
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 2fr' }, gap: 2 }}>
              <FormControl variant="outlined" size="small" fullWidth>
                <InputLabel id="vehicle-label" shrink>Rendszám</InputLabel>
                <Select
                  size="small"
                  labelId="vehicle-label"
                  id="vehicle-select"
                  label="Rendszám"
                  input={<OutlinedInput notched label="Rendszám" />}
                  value={vehicleId}
                  onChange={(e) => {
                    const vid = e.target.value as string;
                    setVehicleId(vid);
                    const v = vehicles.find(vv => vv.id === vid);
                    setLicensePlate(v?.licensePlate || '');
                  }}
                >
                  {vehicles.map(v => (
                    <MenuItem key={v.id} value={v.id}>{v.licensePlate}</MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl variant="outlined" size="small" fullWidth>
                <InputLabel id="line-label" shrink>Vonal / irány</InputLabel>
                <Select
                  size="small"
                  labelId="line-label"
                  id="line-select"
                  label="Vonal / irány"
                  input={<OutlinedInput notched label="Vonal / irány" />}
                  value={lineId}
                  onChange={(e) => setLineId(e.target.value as string)}
                >
                  {lines.map(l => (
                    <MenuItem key={l.id} value={l.id}>{l.name}</MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>
            <TextField size="small" fullWidth label="Megtalálás helyszíne" value={foundLocation} onChange={e => setFoundLocation(e.target.value)} InputLabelProps={{ shrink: true }} />

            {/* Date field aligned to Rendszám width */}
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 2fr' }, gap: 2 }}>
              <TextField
                size="small"
                label="Megtalálás dátuma"
                type="date"
                value={foundAt}
                onChange={e => { setFoundAt(e.target.value); foundAtRef.current?.blur(); }}
                InputLabelProps={{ shrink: true }}
                inputRef={foundAtRef}
              />
              <Box />
            </Box>
          </Box>

          <Divider sx={{ my: 3 }} />

          <Stack direction="row" alignItems="center" justifyContent="space-between">
            <Typography variant="h6">Tárgyak</Typography>
            <Button startIcon={<AddIcon />} variant="outlined" onClick={addItem}>Tárgy hozzáadása</Button>
          </Stack>

          {items.map((it, idx) => (
            <Paper key={idx} variant="outlined" sx={{ p: 2, pt: 1, mt: 2 }}>
              <Typography variant="subtitle2" sx={{ mb: 2 }}>{idx + 1}. tárgy</Typography>
              <Box sx={{
                display: 'grid',
                gap: 2,
                gridTemplateColumns: { xs: '1fr', md: '2fr 1fr 3fr' },
                alignItems: 'end'
              }}>
                <FormControl size="small" fullWidth>
                  <InputLabel id={`cat-${idx}`} shrink>Kategória</InputLabel>
                  <Select size="small" labelId={`cat-${idx}`} label="Kategória" value={it.category} onChange={e => setItemCategory(idx, e.target.value as string)}>
                    {CATEGORIES.map(c => <MenuItem key={c} value={c}>{c}</MenuItem>)}
                  </Select>
                </FormControl>
                <Box>
                  <IconButton color="error" onClick={() => removeItem(idx)} disabled={items.length === 1} aria-label="Tárgy törlése"><DeleteIcon /></IconButton>
                </Box>
                {it.category === 'Egyéb' && (
                  <TextField size="small" fullWidth label="Egyéb kategória" value={it.otherCategoryText || ''} onChange={e => updateItem(idx, { otherCategoryText: e.target.value })} InputLabelProps={{ shrink: true }} />
                )}
                <Box sx={{ gridColumn: '1 / -1' }}>
                  <TextField
                    fullWidth
                    required
                    multiline
                    minRows={it.category === 'Készpénz' ? 2 : 3}
                    label="Leírás / sérülések"
                    value={it.details}
                    onChange={e => updateItem(idx, { details: e.target.value })}
                    InputProps={{ readOnly: it.category === 'Készpénz' }}
                    InputLabelProps={{ shrink: true }}
                    helperText={it.category === 'Készpénz' ? 'A leírás automatikusan az összeget mutatja.' : undefined}
                    size={it.category === 'Készpénz' ? 'small' : 'medium'}
                  />
                </Box>
                {it.category === 'Készpénz' && (
                  <>
                    <Box sx={{ gridColumn: { xs: '1 / -1', md: '1 / span 2' } }}>
                      <FormControl size="small" fullWidth>
                        <InputLabel id={`curr-${idx}`} shrink>Pénznem</InputLabel>
                        <Select size="small" labelId={`curr-${idx}`} label="Pénznem" value={it.cashCurrencyId || ''} onChange={(e) => setItemCashCurrency(idx, e.target.value as string)}>
                          {currencies.map(cur => (
                            <MenuItem key={cur.id} value={cur.id}>{cur.code} - {cur.name}</MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Box>
                    <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: 'repeat(2, 1fr)', md: 'repeat(6, 1fr)' }, gridColumn: '1 / -1' }}>
                      {currencies.find(c => c.id === (it.cashCurrencyId || ''))?.denominations.map(den => (
                        <TextField
                          key={den.id}
                          type="number"
                          label={den.label}
                          value={it.cashDenoms?.[den.id] ?? ''}
                          onChange={(e) => setItemCashDenom(idx, den.id, e.target.value)}
                          inputProps={{ min: 0 }}
                          InputLabelProps={{ shrink: true }}
                          size="small"
                        />
                      ))}
                    </Box>
                  </>
                )}
              </Box>
            </Paper>
          ))}

          {error && <Typography color="error" sx={{ mt: 2 }}>{error}</Typography>}

          <Box mt={3} display="flex" gap={2}>
            <Button type="submit" variant="contained" disabled={submitting}>Leadás rögzítése</Button>
            <Button variant="outlined" onClick={() => navigate(-1)}>Mégse</Button>
          </Box>
        </Box>
      </Paper>
    </Container>
  );
}
