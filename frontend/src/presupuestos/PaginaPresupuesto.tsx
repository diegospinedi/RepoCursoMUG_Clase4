import { useParams } from 'react-router'
import { EditorPresupuesto } from './EditorPresupuesto'

/** /presupuestos/nuevo y /presupuestos/:id. */
export function PaginaPresupuesto() {
  const { id } = useParams()
  const numero = id && id !== 'nuevo' ? Number(id) : undefined
  return <EditorPresupuesto key={id} id={numero} />
}
