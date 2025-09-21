import { Route, Routes, Navigate } from 'react-router-dom'
import ProtectedRoute from './components/ProtectedRoute.tsx'
import AdminRoute from './components/AdminRoute.tsx'
import AppLayout from './components/AppLayout.tsx'
import Login from './pages/Login.tsx'
import ItemsList from './pages/ItemsList.tsx'
import StorageLocations from './pages/StorageLocations.tsx'
import AdminUsers from './pages/AdminUsers.tsx'
import AdminAudit from './pages/AdminAudit.tsx'
import AdminItemsAudit from './pages/AdminItemsAudit.tsx'
import NewDeposit from './pages/NewDeposit.tsx'
import AdminCurrencies from './pages/AdminCurrencies.tsx'
import Lines from './pages/Lines.tsx'
import Vehicles from './pages/Vehicles.tsx'
import Drivers from './pages/Drivers.tsx'
import OwnerHandoverPrint from './pages/prints/OwnerHandoverPrint.tsx'
import OfficeHandoverPrint from './pages/prints/OfficeHandoverPrint.tsx'
import DisposalPrint from './pages/prints/DisposalPrint.tsx'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />

      <Route element={<ProtectedRoute />}> 
        <Route element={<AppLayout />}> 
          <Route index element={<ItemsList />} />
          <Route element={<AdminRoute />}> 
            <Route path="admin/items-audit" element={<AdminItemsAudit />} />
            <Route path="admin/currencies" element={<AdminCurrencies />} />
            <Route path="admin/lines" element={<Lines />} />
            <Route path="admin/vehicles" element={<Vehicles />} />
            <Route path="admin/drivers" element={<Drivers />} />
            <Route path="admin/audit" element={<AdminAudit />} />
            <Route path="admin/users" element={<AdminUsers />} />
            <Route path="admin/storage-locations" element={<StorageLocations />} />
          </Route>
          <Route path="deposits/new" element={<NewDeposit />} />
          <Route path="prints/owner-handover/:id" element={<OwnerHandoverPrint />} />
          <Route path="prints/office-handover/:id" element={<OfficeHandoverPrint />} />
          <Route path="prints/disposal/:id" element={<DisposalPrint />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
