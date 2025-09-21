import { Navigate, Outlet } from 'react-router-dom';

function isAdminFromToken(token: string | null): boolean {
  try {
    if (!token) return false;
    const payload = JSON.parse(atob((token.split('.')[1] ?? '').replace(/-/g, '+').replace(/_/g, '/')));
    const role = payload['role'] || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    return String(role || '').toLowerCase() === 'admin';
  } catch {
    return false;
  }
}

export default function AdminRoute() {
  const token = localStorage.getItem('accessToken');
  if (!token) return <Navigate to="/login" replace />;
  const isAdmin = isAdminFromToken(token);
  if (!isAdmin) return <Navigate to="/" replace />;
  return <Outlet />;
}
