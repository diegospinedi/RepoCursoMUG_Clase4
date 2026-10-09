import { useParams } from 'react-router'
import { DetalleFactura } from './DetalleFactura'

export function PaginaFactura() {
  const { id } = useParams()
  return <DetalleFactura key={id} id={Number(id)} />
}
