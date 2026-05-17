namespace MiniTienda.Models;

public class Ticket
{
    public int IdTicket { get; set; }

    public int IdUsuario { get; set; }

    public int IdCarrito { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public string Estado { get; set; } = "pagado"; // pagado, pendiente, cancelado

    // Para la vista
    public List<CarritoItem>? Items { get; set; }

    public string? NombreUsuario { get; set; }

    public string? CorreoUsuario { get; set; }
}
