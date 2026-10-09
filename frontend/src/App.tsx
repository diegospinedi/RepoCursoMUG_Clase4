import { useLayoutEffect } from 'react'
import { BrowserRouter, NavLink, Route, Routes } from 'react-router'
import { aplicarMarca, marca } from './marca'

const secciones = [
  { ruta: '/presupuestos', nombre: 'Presupuestos' },
  { ruta: '/facturas', nombre: 'Facturas' },
  { ruta: '/articulos', nombre: 'Artículos' },
  { ruta: '/proveedores', nombre: 'Proveedores' },
  { ruta: '/importacion', nombre: 'Importar precios' },
  { ruta: '/configuracion', nombre: 'Configuración' },
]

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
        <Routes>
          <Route path="*" element={<h1>Óptica Sistema</h1>} />
        </Routes>
      </main>
    </BrowserRouter>
  )
}
