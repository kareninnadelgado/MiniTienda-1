namespace MiniTienda.Models;

public class CarritoItem
{
    public int IdCarritoItem { get; set; }

    public int IdCarrito { get; set; }

    public int IdProducto { get; set; }

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    // Para la vista, traemos datos del producto
    public string? NombreProducto { get; set; }

    public string? DescripcionProducto { get; set; }
}
