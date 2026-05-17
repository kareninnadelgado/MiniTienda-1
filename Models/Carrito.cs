namespace MiniTienda.Models;

public class Carrito
{
    public int IdCarrito { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string Estado { get; set; } = "activo"; // activo, abandonado, convertido
}
