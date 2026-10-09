import { useLayoutEffect } from 'react'
import { BrowserRouter, Navigate, NavLink, Route, Routes } from 'react-router'
import { pedir } from './api/cliente'
import { Articulos } from './articulos/Articulos'
import { CambiarContrasena } from './acceso/CambiarContrasena'
import { SesionContexto } from './acceso/SesionContexto'
import { Configuracion } from './configuracion/Configuracion'
import { PaginaFactura } from './facturas/PaginaFactura'
import { Importacion } from './importacion/Importacion'
import { aplicarMarca, marca } from './marca'
import { BuscarPresupuestos } from './presupuestos/BuscarPresupuestos'
import { PaginaPresupuesto } from './presupuestos/PaginaPresupuesto'
import { Proveedores } from './proveedores/Proveedores'

const secciones = [
  { ruta: '/presupuestos', nombre: 'Presupuestos' },
  { ruta: '/facturas', nombre: 'Facturas' },
  { ruta: '/articulos', nombre: 'Artículos' },
  { ruta: '/proveedores', nombre: 'Proveedores' },
  { ruta: '/importacion', nombre: 'Importar precios' },
  { ruta: '/configuracion', nombre: 'Configuración' },
  { ruta: '/contrasena', nombre: 'Contraseña' },
]

async function salir() {
  await pedir('/api/acceso/salir', { method: 'POST' })
  window.location.assign('/')
}

export function Encabezado() {
  return (
    <header className="encabezado">
      <img src={marca.logo} alt="Óptica Sistema" />
      <nav className="navegacion" aria-label="Secciones">
        {secciones.map((s) => (
          <NavLink key={s.ruta} to={s.ruta}>
            {s.nombre}
          </NavLink>
        ))}
        <button className="boton" type="button" onClick={() => void salir()}>
          Salir
        </button>
      </nav>
    </header>
  )
}

export default function App() {
  useLayoutEffect(() => aplicarMarca(document.documentElement), [])
  return (
    <BrowserRouter>
      <Encabezado />
      <main className="contenido">
        <SesionContexto>
          <Routes>
            <Route path="/presupuestos/:id" element={<PaginaPresupuesto />} />
            <Route path="/presupuestos" element={<BuscarPresupuestos />} />
            <Route path="/facturas/:id" element={<PaginaFactura />} />
            <Route path="/articulos" element={<Articulos />} />
            <Route path="/configuracion" element={<Configuracion />} />
            <Route path="/importacion" element={<Importacion />} />
            <Route path="/proveedores" element={<Proveedores />} />
            <Route path="/contrasena" element={<CambiarContrasena />} />
            <Route path="*" element={<Navigate to="/presupuestos" replace />} />
          </Routes>
        </SesionContexto>
      </main>
    </BrowserRouter>
  )
}
