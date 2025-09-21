import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Box, Button, Container, Paper, TextField, Typography } from '@mui/material';
import api from '../api/client';

export default function Login() {
  const { t } = useTranslation();
  const [email, setEmail] = useState('admin@lostandfound.local');
  const [password, setPassword] = useState('Admin123!');
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      const res = await api.post('/api/auth/login', { email, password });
      localStorage.setItem('accessToken', res.data.accessToken);
      window.location.href = '/';
    } catch (err) {
      setError('Hibás bejelentkezés');
    }
  };

  return (
    <Container maxWidth="sm" sx={{ mt: 8 }}>
      <Paper sx={{ p: 4 }}>
        <Typography variant="h5" gutterBottom>{t('login.title')}</Typography>
        <Box component="form" onSubmit={onSubmit}>
          <TextField fullWidth margin="normal" label={t('login.email')} value={email} onChange={e => setEmail(e.target.value)} />
          <TextField fullWidth margin="normal" type="password" label={t('login.password')} value={password} onChange={e => setPassword(e.target.value)} />
          {error && <Typography color="error" variant="body2">{error}</Typography>}
          <Box mt={2}>
            <Button type="submit" variant="contained">{t('login.submit')}</Button>
          </Box>
        </Box>
      </Paper>
    </Container>
  );
}
