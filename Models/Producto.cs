namespace MiniTienda.Models
{
    public class Producto
    {
        public int IdProducto { get; set; } // Así se llama en tu SQL
        public string Nombre { get; set; } = "";
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string? Categoria { get; set; }
    }
}