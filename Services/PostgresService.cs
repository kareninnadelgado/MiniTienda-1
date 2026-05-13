using MiniTienda.Models;
using MySql.Data.MySqlClient;

namespace MiniTienda.Services;

public class PostgresService
{
    private readonly string connectionString =
        "Server=34.42.44.71;Port=3306;Database=core;Uid=app_writer;Pwd=U?e}bq<@+yY8ex?+;SslMode=Disabled;";

    public Usuario? ObtenerUsuarioPorCorreo(string correo)
    {
        using var conn = new MySqlConnection(connectionString);

        Console.WriteLine(connectionString);
        Console.WriteLine("Intentando conectar...");
        conn.Open();
        Console.WriteLine("Conectado");

        string query = @"
        SELECT idUsuario, firebaseUid, nombre, correo, rol, activo
        FROM usuarios
        WHERE correo = @correo";

        using var cmd = new MySqlCommand(query, conn);

        cmd.Parameters.AddWithValue("@correo", correo);

        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            return new Usuario
            {
                IdUsuario = reader.GetInt32(0),
                FirebaseUid = reader.GetString(1),
                Nombre = reader.GetString(2),
                Correo = reader.GetString(3),
                Rol = reader.GetString(4),
                Activo = reader.GetBoolean(5)
            };
        }

        return null;
    }

    public void InsertarUsuario(
    string firebaseUid,
    string nombre,
    string correo)
    {
        using var conn = new MySql.Data.MySqlClient.MySqlConnection(connectionString);

        conn.Open();

        string query = @"
        INSERT INTO usuarios
        (firebaseUid, nombre, correo, rol, activo)
        VALUES
        (@firebaseUid, @nombre, @correo, 'cliente', true)";

        using var cmd = new MySql.Data.MySqlClient.MySqlCommand(query, conn);

        cmd.Parameters.AddWithValue("@firebaseUid", firebaseUid);
        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@correo", correo);

        cmd.ExecuteNonQuery();
    }
    // Método para obtener la lista de productos
    public List<Producto> ObtenerProductos()
    {
        List<Producto> lista = new List<Producto>();
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        // Usamos 'productos' porque tu cadena de conexión ya apunta a la base 'core'
        string query = "SELECT idProducto, nombre, descripcion, precio, stock, categoria FROM productos";

        using var cmd = new MySqlCommand(query, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            lista.Add(new Producto
            {
                IdProducto = reader.GetInt32("idProducto"),
                Nombre = reader.GetString("nombre"),
                Descripcion = reader.IsDBNull(reader.GetOrdinal("descripcion")) ? "" : reader.GetString("descripcion"),
                Precio = reader.GetDecimal("precio"),
                Stock = reader.GetInt32("stock"),
                Categoria = reader.IsDBNull(reader.GetOrdinal("categoria")) ? "" : reader.GetString("categoria")
            });
        }
        return lista;
    }

    // Método para eliminar un producto (parte del CRUD)
    public void EliminarProducto(int id)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        string query = "DELETE FROM productos WHERE idProducto = @id";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
    public void InsertarProducto(Producto nuevo)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        INSERT INTO productos (nombre, descripcion, precio, stock, categoria)
        VALUES (@nombre, @descripcion, @precio, @stock, @categoria)";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@nombre", nuevo.Nombre);
        cmd.Parameters.AddWithValue("@descripcion", nuevo.Descripcion);
        cmd.Parameters.AddWithValue("@precio", nuevo.Precio);
        cmd.Parameters.AddWithValue("@stock", nuevo.Stock);
        cmd.Parameters.AddWithValue("@categoria", nuevo.Categoria);

        cmd.ExecuteNonQuery();
    }
    public void ActualizarProducto(Producto p)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        string query = @"UPDATE productos 
                     SET nombre=@n, descripcion=@d, precio=@p, stock=@s, categoria=@c 
                     WHERE idProducto=@id";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@n", p.Nombre);
        cmd.Parameters.AddWithValue("@d", p.Descripcion);
        cmd.Parameters.AddWithValue("@p", p.Precio);
        cmd.Parameters.AddWithValue("@s", p.Stock);
        cmd.Parameters.AddWithValue("@c", p.Categoria);
        cmd.Parameters.AddWithValue("@id", p.IdProducto);
        cmd.ExecuteNonQuery();
    }
    // Variable para guardar al usuario que inició sesión
    private Usuario? _usuarioLogueado;

    // Método para establecer al usuario cuando hace Login
    public void SetUsuarioActual(Usuario usuario)
    {
        _usuarioLogueado = usuario;
    }

    // El método que te marcaba error:
    public Usuario? ObtenerUsuarioActual()
    {
        return _usuarioLogueado;
    }
}