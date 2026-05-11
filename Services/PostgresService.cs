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
}