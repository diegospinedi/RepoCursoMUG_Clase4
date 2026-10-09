import { useLayoutEffect } from 'react'
import { BrowserRouter, NavLink, Route, Routes } from 'react-router'
import { pedir } from './api/cliente'
import { CambiarContrasena } from './acceso/CambiarContrasena'
import { SesionContexto } from './acceso/SesionContexto'
import { Configuracion } from './configuracion/Configuracion'
import { aplicarMarca, marca } from './marca'
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
            <Route path="/configuracion" element={<Configuracion />} />
            <Route path="/proveedores" element={<Proveedores />} />
            <Route path="/contrasena" element={<CambiarContrasena />} />
            <Route path="*" element={<h1>Óptica Sistema</h1>} />
          </Routes>
        </SesionContexto>
      </main>
    </BrowserRouter>
  )
}
