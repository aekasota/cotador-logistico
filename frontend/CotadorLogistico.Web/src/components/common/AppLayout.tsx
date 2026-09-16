import { Outlet } from 'react-router-dom';
import { Header } from '../Header/Header';
import { DemoBanner } from './DemoBanner';

export function AppLayout() {
  return (
    <>
      <DemoBanner />
      <div className="page-shell">
        <Header />
        <Outlet />
      </div>
    </>
  );
}

export function NarrowAppLayout() {
  return (
    <>
      <DemoBanner />
      <div className="page-shell is-narrow">
        <Header />
        <Outlet />
      </div>
    </>
  );
}
