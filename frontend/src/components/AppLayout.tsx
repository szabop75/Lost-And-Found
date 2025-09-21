import React from 'react';
import { AppBar, Box, Button, Container, Menu, MenuItem, Toolbar, Typography } from '@mui/material';
import { Link as RouterLink, Outlet, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import api from '../api/client';

export default function AppLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const year = new Date().getFullYear();

  // Determine role from JWT
  const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
  let isAdmin = false;
  let userDisplayName: string | null = null;
  try {
    if (token) {
      const payload = JSON.parse(atob(token.split('.')[1] ?? '')) as Record<string, any>;
      const roleClaim = payload['role'] || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      isAdmin = String(roleClaim || '').toLowerCase() === 'admin';
      // Try common name claims first
      const nameClaims = [
        'name',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/name',
        'preferred_username',
        'unique_name',
        'email'
      ];
      for (const key of nameClaims) {
        if (payload[key]) { userDisplayName = String(payload[key]); break; }
      }
      // If not found, try to compose from given/family name
      if (!userDisplayName) {
        const given = payload['given_name'] || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'];
        const family = payload['family_name'] || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'];
        if (given || family) {
          userDisplayName = [given, family].filter(Boolean).join(' ');
        }
      }
      // Last resort: subject id
      if (!userDisplayName && payload['sub']) userDisplayName = String(payload['sub']);
    }
  } catch {}

  // Display name resolved solely from the authoritative account API
  const [resolvedName, setResolvedName] = React.useState<string | null>(null);
  React.useEffect(() => {
    // Always use the dedicated account endpoint
    (async () => {
      try {
        const me = await api.get<{ email?: string | null; fullName?: string | null }>(
          '/api/account/me'
        );
        const full = (me.data.fullName || '').trim();
        setResolvedName(full.length > 0 ? full : null);
      } catch {
        // If the endpoint is not available or unauthorized, leave as null
      }
    })();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const [adminAnchor, setAdminAnchor] = React.useState<null | HTMLElement>(null);
  const hoverCloseTimer = React.useRef<number | null>(null);
  const openAdmin = (e: React.MouseEvent<HTMLElement>) => setAdminAnchor(e.currentTarget);
  const closeAdmin = () => setAdminAnchor(null);
  const scheduleClose = () => {
    if (hoverCloseTimer.current) window.clearTimeout(hoverCloseTimer.current);
    hoverCloseTimer.current = window.setTimeout(() => setAdminAnchor(null), 500);
  };
  const cancelScheduledClose = () => {
    if (hoverCloseTimer.current) {
      window.clearTimeout(hoverCloseTimer.current);
      hoverCloseTimer.current = null;
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('accessToken');
    navigate('/login');
  };

  return (
    <Box display="flex" flexDirection="column" minHeight="100vh">
      <AppBar position="fixed" sx={{ bgcolor: '#102B4E' }}>
        <Toolbar sx={{ pl: 0 }}>
          <Box
            component="img"
            src="/icons/tbusz-logo-horizontal-white.svg"
            alt="TBUSZ"
            sx={{ height: { xs: 44, sm: 48 }, mr: 4, ml: '50px', display: 'block' }}
          />
          <Typography variant="h6" sx={{ flexGrow: 1, fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 800 }}>
            {t('appTitle')}
          </Typography>
          {resolvedName && (
            <Box sx={{ mr: 3, textAlign: 'right' }}>
              <Typography variant="caption" sx={{ lineHeight: 1, opacity: 0.9 }}>Felhasználó:</Typography>
              <Typography variant="subtitle1" sx={{ lineHeight: 1.1, fontWeight: 700 }}>{resolvedName}</Typography>
            </Box>
          )}
          <Button color="inherit" component={RouterLink} to="/" sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
            {t('nav.items')}
          </Button>
          {isAdmin && (
            <>
              <Button
                color="inherit"
                onMouseEnter={openAdmin}
                onMouseLeave={scheduleClose}
                sx={{
                  fontFamily: 'Montserrat, Roboto, Arial, sans-serif',
                  fontWeight: 700,
                  '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' }
                }}
              >
                {t('nav.admin')}
              </Button>
              <Menu
                anchorEl={adminAnchor}
                open={Boolean(adminAnchor)}
                onClose={closeAdmin}
                MenuListProps={{ onMouseEnter: cancelScheduledClose, onMouseLeave: scheduleClose }}
              >
                <MenuItem component={RouterLink} to="/admin/items-audit" onClick={closeAdmin} sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
                  {t('nav.itemsAudit')}
                </MenuItem>
                <MenuItem component={RouterLink} to="/admin/audit" onClick={closeAdmin} sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
                  {t('nav.audit')}
                </MenuItem>
                <MenuItem component={RouterLink} to="/admin/users" onClick={closeAdmin} sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
                  {t('nav.users')}
                </MenuItem>
                <MenuItem component={RouterLink} to="/admin/currencies" onClick={closeAdmin} sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
                  {t('nav.currencies')}
                </MenuItem>
                <MenuItem component={RouterLink} to="/admin/lines" onClick={closeAdmin} sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
                  Vonalak
                </MenuItem>
                <MenuItem component={RouterLink} to="/admin/vehicles" onClick={closeAdmin} sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
                  Járművek
                </MenuItem>
                <MenuItem component={RouterLink} to="/admin/drivers" onClick={closeAdmin} sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
                  Járművezetők
                </MenuItem>
                <MenuItem component={RouterLink} to="/admin/storage-locations" onClick={closeAdmin} sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
                  {t('nav.storageLocations')}
                </MenuItem>
              </Menu>
            </>
          )}
          <Button color="inherit" component={RouterLink} to="/deposits/new" sx={{ fontFamily: 'Montserrat, Roboto, Arial, sans-serif', fontWeight: 700, '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' } }}>
            Új leadás
          </Button>
          <Button
            color="inherit"
            onClick={handleLogout}
            sx={{
              fontFamily: 'Montserrat, Roboto, Arial, sans-serif',
              fontWeight: 700,
              '&:hover': { color: (t) => t.palette.primary.light, bgcolor: 'transparent' }
            }}
          >
            {t('nav.logout')}
          </Button>
        </Toolbar>
      </AppBar>
      {/* Spacer to offset the fixed AppBar height */}
      <Toolbar />
      <Container sx={{ flexGrow: 1, py: 3 }}>
        <Outlet />
      </Container>
      <Box component="footer" sx={{ textAlign: 'center', py: 2, borderTop: '1px solid', borderColor: 'divider', bgcolor: (t) => t.palette.grey[100] }}>
        <Typography variant="body2" color="text.secondary">
          © {year} VETRASYS Kft.
        </Typography>
      </Box>
    </Box>
  );
}
