using MiniTienda.Models;
using MySql.Data.MySqlClient;

namespace MiniTienda.Services;

public class PostgresService
{
    // Constantes de seguridad para cantidades
    private const int CANTIDAD_MINIMA = 1;
    private const int CANTIDAD_MAXIMA = 99;  // Máximo de unidades por producto
    private const int MAX_PRODUCTOS_POR_CARRITO = 50;  // Máximo de items diferentes
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
        if (nuevo is null)
        {
            throw new ArgumentNullException(nameof(nuevo));
        }

        if (string.IsNullOrWhiteSpace(nuevo.Nombre))
        {
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(nuevo.Nombre));
        }

        if (nuevo.Precio < 0)
        {
            throw new ArgumentException("El precio no puede ser negativo.", nameof(nuevo.Precio));
        }

        if (nuevo.Stock < 0)
        {
            throw new ArgumentException("El stock no puede ser negativo.", nameof(nuevo.Stock));
        }

        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        INSERT INTO productos (nombre, descripcion, precio, stock, categoria)
        VALUES (@nombre, @descripcion, @precio, @stock, @categoria)";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@nombre", nuevo.Nombre);
        cmd.Parameters.AddWithValue("@descripcion", nuevo.Descripcion ?? string.Empty);
        cmd.Parameters.AddWithValue("@precio", nuevo.Precio);
        cmd.Parameters.AddWithValue("@stock", nuevo.Stock);
        cmd.Parameters.AddWithValue("@categoria", string.IsNullOrWhiteSpace(nuevo.Categoria) ? string.Empty : nuevo.Categoria);

        cmd.ExecuteNonQuery();
    }
    public void ActualizarProducto(Producto p)
    {
        if (p is null)
        {
            throw new ArgumentNullException(nameof(p));
        }

        if (string.IsNullOrWhiteSpace(p.Nombre))
        {
            throw new ArgumentException("El nombre del producto es obligatorio.", nameof(p.Nombre));
        }

        if (p.Precio < 0)
        {
            throw new ArgumentException("El precio no puede ser negativo.", nameof(p.Precio));
        }

        if (p.Stock < 0)
        {
            throw new ArgumentException("El stock no puede ser negativo.", nameof(p.Stock));
        }

        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        string query = @"UPDATE productos 
                     SET nombre=@n, descripcion=@d, precio=@p, stock=@s, categoria=@c 
                     WHERE idProducto=@id";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@n", p.Nombre);
        cmd.Parameters.AddWithValue("@d", p.Descripcion ?? string.Empty);
        cmd.Parameters.AddWithValue("@p", p.Precio);
        cmd.Parameters.AddWithValue("@s", p.Stock);
        cmd.Parameters.AddWithValue("@c", string.IsNullOrWhiteSpace(p.Categoria) ? string.Empty : p.Categoria);
        cmd.Parameters.AddWithValue("@id", p.IdProducto);
        cmd.ExecuteNonQuery();
    }
    public void ClearUsuarioActual()
    {
        _usuarioLogueado = null;
    }
    // Variable para guardar al usuario que inició sesión
    private Usuario? _usuarioLogueado;

    // Método para establecer al usuario cuando hace Login
    public void SetUsuarioActual(Usuario usuario)
    {
        _usuarioLogueado = usuario;
    }

    // Obtener usuario actual desde la sesión
    public Usuario? ObtenerUsuarioActual()
    {
        return _usuarioLogueado;
    }

    // ========== MÉTODOS DE CARRITO ==========

    public Carrito? ObtenerCarritoActivo(int idUsuario)
    {
        // Validar que el idUsuario sea válido
        if (idUsuario <= 0)
        {
            throw new UnauthorizedAccessException("Usuario no válido");
        }
        
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT idCarrito, idUsuario, fechaCreacion, estado
        FROM carrito
        WHERE idUsuario = @idUsuario AND estado = 'activo'
        LIMIT 1";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Carrito
            {
                IdCarrito = reader.GetInt32(0),
                IdUsuario = reader.GetInt32(1),
                FechaCreacion = reader.GetDateTime(2),
                Estado = reader.GetString(3)
            };
        }

        return null;
    }

    public Carrito CrearCarrito(int idUsuario)
    {
        // Validar que el idUsuario sea válido
        if (idUsuario <= 0)
        {
            throw new UnauthorizedAccessException("Usuario no válido");
        }
        
        // Verificar que el usuario exista y sea cliente
        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        
        string queryUsuario = @"
        SELECT rol, activo FROM usuarios
        WHERE idUsuario = @idUsuario";
        
        using var cmdUsuario = new MySqlCommand(queryUsuario, conn);
        cmdUsuario.Parameters.AddWithValue("@idUsuario", idUsuario);
        
        using var reader = cmdUsuario.ExecuteReader();
        if (!reader.Read())
        {
            throw new UnauthorizedAccessException("El usuario no existe");
        }
        
        string rol = reader.GetString(0);
        bool activo = reader.GetBoolean(1);
        reader.Close();
        
        if (rol != "cliente" || !activo)
        {
            throw new UnauthorizedAccessException("No tienes permiso para crear un carrito");
        }
        
        string query = @"
        INSERT INTO carrito (idUsuario, fechaCreacion, estado)
        VALUES (@idUsuario, NOW(), 'activo');
        SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

        var idCarrito = cmd.ExecuteScalar();

        return new Carrito
        {
            IdCarrito = Convert.ToInt32(idCarrito),
            IdUsuario = idUsuario,
            FechaCreacion = DateTime.Now,
            Estado = "activo"
        };
    }

    public CarritoItem AgregarAlCarrito(int idCarrito, int idProducto, int cantidad)
    {
        // Validar rango de cantidad
        if (cantidad < CANTIDAD_MINIMA)
        {
            throw new ArgumentException($"La cantidad mínima es {CANTIDAD_MINIMA}");
        }
        
        if (cantidad > CANTIDAD_MAXIMA)
        {
            throw new ArgumentException($"No puedes agregar más de {CANTIDAD_MAXIMA} unidades del mismo producto");
        }
        
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        // Verificar que el carrito existe y está activo
        string queryCarrito = @"
        SELECT c.idUsuario, c.estado, u.rol, u.activo
        FROM carrito c
        JOIN usuarios u ON c.idUsuario = u.idUsuario
        WHERE c.idCarrito = @idCarrito";
        
        using var cmdCarrito = new MySqlCommand(queryCarrito, conn);
        cmdCarrito.Parameters.AddWithValue("@idCarrito", idCarrito);
        
        using var readerCarrito = cmdCarrito.ExecuteReader();
        if (!readerCarrito.Read())
        {
            throw new UnauthorizedAccessException("El carrito no existe");
        }
        
        string estadoCarrito = readerCarrito.GetString(1);
        readerCarrito.Close();
        
        if (estadoCarrito != "activo")
        {
            throw new InvalidOperationException("El carrito no está activo");
        }
        
        // Obtener precio real y stock del producto
        string queryProducto = @"
        SELECT precio, stock FROM productos
        WHERE idProducto = @idProducto";
        
        using var cmdProducto = new MySqlCommand(queryProducto, conn);
        cmdProducto.Parameters.AddWithValue("@idProducto", idProducto);
        
        using var readerProducto = cmdProducto.ExecuteReader();
        if (!readerProducto.Read())
        {
            throw new InvalidOperationException("El producto no existe");
        }
        
        decimal precioReal = readerProducto.GetDecimal(0);
        int stockActual = readerProducto.GetInt32(1);
        readerProducto.Close();
        
        // Validar stock disponible
        if (stockActual < cantidad)
        {
            throw new InvalidOperationException($"Stock insuficiente. Solo hay {stockActual} unidades disponibles");
        }
        
        // Verificar cantidad total en carrito (sumando lo que ya tiene)
        string queryCantidadActual = @"
        SELECT COALESCE(SUM(cantidad), 0) FROM carritoItems
        WHERE idCarrito = @idCarrito AND idProducto = @idProducto";
        
        using var cmdCantidadActual = new MySqlCommand(queryCantidadActual, conn);
        cmdCantidadActual.Parameters.AddWithValue("@idCarrito", idCarrito);
        cmdCantidadActual.Parameters.AddWithValue("@idProducto", idProducto);
        
        int cantidadExistente = Convert.ToInt32(cmdCantidadActual.ExecuteScalar());
        int cantidadTotal = cantidadExistente + cantidad;
        
        // Validar que no exceda el máximo por producto
        if (cantidadTotal > CANTIDAD_MAXIMA)
        {
            throw new InvalidOperationException($"No puedes tener más de {CANTIDAD_MAXIMA} unidades de este producto en el carrito");
        }
        
        // Verificar límite de productos diferentes en carrito
        string queryCantidadItems = @"
        SELECT COUNT(*) FROM carritoItems
        WHERE idCarrito = @idCarrito";
        
        using var cmdCantidadItems = new MySqlCommand(queryCantidadItems, conn);
        cmdCantidadItems.Parameters.AddWithValue("@idCarrito", idCarrito);
        
        int itemsEnCarrito = Convert.ToInt32(cmdCantidadItems.ExecuteScalar());
        
        if (cantidadExistente == 0 && itemsEnCarrito >= MAX_PRODUCTOS_POR_CARRITO)
        {
            throw new InvalidOperationException($"No puedes agregar más de {MAX_PRODUCTOS_POR_CARRITO} productos diferentes en el carrito");
        }
        
        // Validar stock nuevamente con la cantidad total
        if (stockActual < cantidadTotal)
        {
            throw new InvalidOperationException($"Stock insuficiente. Solo hay {stockActual} unidades disponibles");
        }
        
        // Insertar o actualizar carrito
        if (cantidadExistente > 0)
        {
            ActualizarCantidadItem(idCarrito, idProducto, cantidadTotal);
            
            return new CarritoItem
            {
                IdCarrito = idCarrito,
                IdProducto = idProducto,
                Cantidad = cantidadTotal,
                PrecioUnitario = precioReal
            };
        }
        
        string queryInsert = @"
        INSERT INTO carritoItems (idCarrito, idProducto, cantidad, precioUnitario)
        VALUES (@idCarrito, @idProducto, @cantidad, @precioUnitario);
        SELECT LAST_INSERT_ID();";
        
        using var cmdInsert = new MySqlCommand(queryInsert, conn);
        cmdInsert.Parameters.AddWithValue("@idCarrito", idCarrito);
        cmdInsert.Parameters.AddWithValue("@idProducto", idProducto);
        cmdInsert.Parameters.AddWithValue("@cantidad", cantidad);
        cmdInsert.Parameters.AddWithValue("@precioUnitario", precioReal);
        
        var idItem = cmdInsert.ExecuteScalar();
        
        return new CarritoItem
        {
            IdCarritoItem = Convert.ToInt32(idItem),
            IdCarrito = idCarrito,
            IdProducto = idProducto,
            Cantidad = cantidad,
            PrecioUnitario = precioReal
        };
    }

    public List<CarritoItem> ObtenerItemsCarrito(int idCarrito)
    {
        var items = new List<CarritoItem>();

        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT ci.idCarritoItem, ci.idCarrito, ci.idProducto, ci.cantidad, ci.precioUnitario,
               p.nombre, p.descripcion
        FROM carritoItems ci
        JOIN productos p ON ci.idProducto = p.idProducto
        WHERE ci.idCarrito = @idCarrito";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idCarrito", idCarrito);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new CarritoItem
            {
                IdCarritoItem = reader.GetInt32(0),
                IdCarrito = reader.GetInt32(1),
                IdProducto = reader.GetInt32(2),
                Cantidad = reader.GetInt32(3),
                PrecioUnitario = reader.GetDecimal(4),
                NombreProducto = reader.GetString(5),
                DescripcionProducto = reader.IsDBNull(6) ? "" : reader.GetString(6)
            });
        }

        return items;
    }

    public void ActualizarCantidadItem(int idCarrito, int idProducto, int nuevaCantidad)
    {
        // Validar rango de cantidad
        if (nuevaCantidad < CANTIDAD_MINIMA)
        {
            throw new ArgumentException($"La cantidad mínima es {CANTIDAD_MINIMA}");
        }
        
        if (nuevaCantidad > CANTIDAD_MAXIMA)
        {
            throw new ArgumentException($"La cantidad máxima es {CANTIDAD_MAXIMA}");
        }
        
        using var conn = new MySqlConnection(connectionString);
        conn.Open();
        
        // Validar que el carrito pertenezca a un usuario autenticado
        string queryCarrito = @"
        SELECT c.estado, u.rol, u.activo
        FROM carrito c
        JOIN usuarios u ON c.idUsuario = u.idUsuario
        WHERE c.idCarrito = @idCarrito";
        
        using var cmdCarrito = new MySqlCommand(queryCarrito, conn);
        cmdCarrito.Parameters.AddWithValue("@idCarrito", idCarrito);
        
        using var readerCarrito = cmdCarrito.ExecuteReader();
        if (!readerCarrito.Read())
        {
            throw new UnauthorizedAccessException("El carrito no existe");
        }
        
        string estadoCarrito = readerCarrito.GetString(0);
        string rolUsuario = readerCarrito.GetString(1);
        bool activo = readerCarrito.GetBoolean(2);
        readerCarrito.Close();
        
        if (estadoCarrito != "activo")
        {
            throw new InvalidOperationException("No puedes modificar un carrito que ya fue convertido");
        }
        
        if (rolUsuario != "cliente" || !activo)
        {
            throw new UnauthorizedAccessException("No tienes permiso para modificar este carrito");
        }
        
        // Validar stock disponible
        string queryStock = @"
        SELECT stock FROM productos
        WHERE idProducto = @idProducto";
        
        using var cmdStock = new MySqlCommand(queryStock, conn);
        cmdStock.Parameters.AddWithValue("@idProducto", idProducto);
        
        var stockDisponible = cmdStock.ExecuteScalar();
        if (stockDisponible == null)
        {
            throw new InvalidOperationException("El producto no existe");
        }
        
        if (Convert.ToInt32(stockDisponible) < nuevaCantidad)
        {
            throw new InvalidOperationException($"Stock insuficiente. Solo hay {stockDisponible} unidades disponibles");
        }
        
        // Actualizar la cantidad
        string query = @"
        UPDATE carritoItems
        SET cantidad = @cantidad
        WHERE idCarrito = @idCarrito AND idProducto = @idProducto";
        
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@cantidad", nuevaCantidad);
        cmd.Parameters.AddWithValue("@idCarrito", idCarrito);
        cmd.Parameters.AddWithValue("@idProducto", idProducto);
        
        int filasAfectadas = cmd.ExecuteNonQuery();
        
        if (filasAfectadas == 0)
        {
            throw new InvalidOperationException("No se pudo actualizar la cantidad. El producto no está en el carrito");
        }
    }

    public void EliminarDelCarrito(int idCarrito, int idProducto)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        DELETE FROM carritoItems
        WHERE idCarrito = @idCarrito AND idProducto = @idProducto";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idCarrito", idCarrito);
        cmd.Parameters.AddWithValue("@idProducto", idProducto);

        cmd.ExecuteNonQuery();
    }

    public void VaciarCarrito(int idCarrito)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        DELETE FROM carritoItems
        WHERE idCarrito = @idCarrito";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idCarrito", idCarrito);

        cmd.ExecuteNonQuery();
    }

    // ========== MÉTODOS DE VALIDACIÓN ==========

    public bool ValidarStockDisponible(int idProducto, int cantidadSolicitada)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT stock FROM productos
        WHERE idProducto = @idProducto";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idProducto", idProducto);

        var stockDisponible = cmd.ExecuteScalar();

        if (stockDisponible == null)
            return false;

        return Convert.ToInt32(stockDisponible) >= cantidadSolicitada;
    }

    public int ObtenerStockActual(int idProducto)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT stock FROM productos
        WHERE idProducto = @idProducto";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idProducto", idProducto);

        var resultado = cmd.ExecuteScalar();
        return resultado == null ? 0 : Convert.ToInt32(resultado);
    }

    // ========== MÉTODOS DE AUDITORÍA ==========

    public void RegistrarAuditoria(int idUsuario, string accion, string entidad, int? idEntidad, string? detalle = null, string? ipCliente = null, string? dispositivo = null)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        // ✅ Especificar el schema 'log'
        string query = @"
        INSERT INTO log.auditoria (idUsuario, accion, entidad, idEntidad, fechaHora, detalle, ipCliente, dispositivo)
        VALUES (@idUsuario, @accion, @entidad, @idEntidad, NOW(), @detalle, @ipCliente, @dispositivo)";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
        cmd.Parameters.AddWithValue("@accion", accion);
        cmd.Parameters.AddWithValue("@entidad", entidad);
        cmd.Parameters.AddWithValue("@idEntidad", idEntidad ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@detalle", detalle ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ipCliente", ipCliente ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@dispositivo", dispositivo ?? (object)DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    // ========== MÉTODOS DE TICKETS (COMPRAS SIMULADAS) ==========

    public Ticket? FinalizarCompra(int idUsuario, int idCarrito)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();

        try
        {
            // ========== VALIDACIONES DE AUTENTICACIÓN ==========
            // Verificar que el usuario existe y está activo
            string queryUsuario = @"
            SELECT rol, activo FROM usuarios
            WHERE idUsuario = @idUsuario";

            using var cmdUsuario = new MySqlCommand(queryUsuario, conn, transaction);
            cmdUsuario.Parameters.AddWithValue("@idUsuario", idUsuario);

            using var readerUsuario = cmdUsuario.ExecuteReader();
            if (!readerUsuario.Read())
            {
                throw new UnauthorizedAccessException("El usuario no existe");
            }

            string rolUsuario = readerUsuario.GetString(0);
            bool activo = readerUsuario.GetBoolean(1);
            readerUsuario.Close();

            if (rolUsuario != "cliente" || !activo)
            {
                throw new UnauthorizedAccessException("No tienes permiso para realizar compras");
            }

            // ========== VALIDACIONES DEL CARRITO ==========
            // Verificar que el carrito existe, pertenece al usuario y está activo
            string queryVerificarCarrito = @"
            SELECT estado FROM carrito
            WHERE idCarrito = @idCarrito AND idUsuario = @idUsuario";

            using var cmdVerificar = new MySqlCommand(queryVerificarCarrito, conn, transaction);
            cmdVerificar.Parameters.AddWithValue("@idCarrito", idCarrito);
            cmdVerificar.Parameters.AddWithValue("@idUsuario", idUsuario);

            var estadoCarrito = cmdVerificar.ExecuteScalar();
            if (estadoCarrito == null)
            {
                throw new InvalidOperationException("El carrito no existe");
            }
            
            if (estadoCarrito.ToString() != "activo")
            {
                throw new InvalidOperationException("El carrito no está activo o ya fue convertido");
            }

            // ========== OBTENER ITEMS DEL CARRITO CON PRECIOS REALES ==========
            string queryItems = @"
            SELECT ci.idProducto, ci.cantidad, p.precio as precioReal, p.stock, p.nombre
            FROM carritoItems ci
            JOIN productos p ON ci.idProducto = p.idProducto
            WHERE ci.idCarrito = @idCarrito";

            var items = new List<(int idProducto, int cantidad, decimal precioReal, int stockActual, string nombre)>();

            using var cmdItems = new MySqlCommand(queryItems, conn, transaction);
            cmdItems.Parameters.AddWithValue("@idCarrito", idCarrito);

            using var readerItems = cmdItems.ExecuteReader();
            while (readerItems.Read())
            {
                int cantidad = readerItems.GetInt32(1);
                
                // ========== VALIDACIONES DE CANTIDAD ==========
                // Validar que la cantidad sea positiva
                if (cantidad <= 0)
                {
                    throw new InvalidOperationException($"Cantidad inválida para un producto en el carrito");
                }
                
                // Validar que no exceda el máximo permitido
                if (cantidad > 99)
                {
                    throw new InvalidOperationException($"La cantidad máxima por producto es 99 unidades");
                }
                
                items.Add((
                    readerItems.GetInt32(0),
                    cantidad,
                    readerItems.GetDecimal(2),
                    readerItems.GetInt32(3),
                    readerItems.GetString(4)
                ));
            }
            readerItems.Close();

            if (items.Count == 0)
            {
                throw new InvalidOperationException("El carrito está vacío");
            }
            
            // Validar límite de productos diferentes en el carrito
            if (items.Count > 50)
            {
                throw new InvalidOperationException("El carrito excede el límite de 50 productos diferentes");
            }

            // ========== CALCULAR TOTAL Y VALIDAR STOCK ==========
            decimal total = 0;
            
            foreach (var (idProducto, cantidad, precioReal, stockActual, nombre) in items)
            {
                // Validar stock disponible
                if (stockActual < cantidad)
                {
                    throw new InvalidOperationException($"Stock insuficiente para el producto '{nombre}'. Stock actual: {stockActual}, solicitado: {cantidad}");
                }
                
                // Acumular total usando precios reales de la base de datos
                total += precioReal * cantidad;
            }

            // ========== REDUCIR STOCK DE CADA PRODUCTO ==========
            foreach (var (idProducto, cantidad, _, _, nombre) in items)
            {
                string queryActualizarStock = @"
                UPDATE productos
                SET stock = stock - @cantidad
                WHERE idProducto = @idProducto AND stock >= @cantidad";

                using var cmdActualizar = new MySqlCommand(queryActualizarStock, conn, transaction);
                cmdActualizar.Parameters.AddWithValue("@cantidad", cantidad);
                cmdActualizar.Parameters.AddWithValue("@idProducto", idProducto);

                int filasAfectadas = cmdActualizar.ExecuteNonQuery();
                
                if (filasAfectadas == 0)
                {
                    throw new InvalidOperationException($"No se pudo actualizar el stock del producto '{nombre}'. Es posible que el stock haya cambiado");
                }
            }

            // ========== CREAR TICKET ==========
            string queryTicket = @"
            INSERT INTO ticket (idUsuario, idCarrito, fecha, total, estado)
            VALUES (@idUsuario, @idCarrito, NOW(), @total, 'pagado');
            SELECT LAST_INSERT_ID();";

            using var cmdTicket = new MySqlCommand(queryTicket, conn, transaction);
            cmdTicket.Parameters.AddWithValue("@idUsuario", idUsuario);
            cmdTicket.Parameters.AddWithValue("@idCarrito", idCarrito);
            cmdTicket.Parameters.AddWithValue("@total", total);

            var idTicketObj = cmdTicket.ExecuteScalar();
            int idTicket = Convert.ToInt32(idTicketObj);

            // ========== CAMBIAR ESTADO DEL CARRITO A 'CONVERTIDO' ==========
            string queryCarrito = @"
            UPDATE carrito
            SET estado = 'convertido'
            WHERE idCarrito = @idCarrito";

            using var cmdCarrito = new MySqlCommand(queryCarrito, conn, transaction);
            cmdCarrito.Parameters.AddWithValue("@idCarrito", idCarrito);
            cmdCarrito.ExecuteNonQuery();

            // ========== REGISTRAR AUDITORÍA ==========
            var detalleJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                ticketId = idTicket,
                totalCalculado = total,
                cantidadProductos = items.Count,
                metodoValidacion = "precios_desde_bd",
                validacionesAplicadas = new
                {
                    precioValidado = true,
                    stockValidado = true,
                    cantidadMaximaValidada = true,
                    usuarioAutenticado = true
                },
                timestamp = DateTime.Now
            });

            string queryAuditoria = @"
            INSERT INTO log.auditoria (idUsuario, accion, entidad, idEntidad, fechaHora, detalle)
            VALUES (@idUsuario, @accion, @entidad, @idEntidad, NOW(), @detalle)";

            using var cmdAuditoria = new MySqlCommand(queryAuditoria, conn, transaction);
            cmdAuditoria.Parameters.AddWithValue("@idUsuario", idUsuario);
            cmdAuditoria.Parameters.AddWithValue("@accion", "compra_simulada");
            cmdAuditoria.Parameters.AddWithValue("@entidad", "ticket");
            cmdAuditoria.Parameters.AddWithValue("@idEntidad", idTicket);
            cmdAuditoria.Parameters.AddWithValue("@detalle", detalleJson);

            cmdAuditoria.ExecuteNonQuery();

            // ========== CONFIRMAR TRANSACCIÓN ==========
            transaction.Commit();

            // ========== RETORNAR EL TICKET CREADO ==========
            return ObtenerTicketPorId(idTicket);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Ticket? ObtenerTicketPorId(int idTicket)
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT t.idTicket, t.idUsuario, t.idCarrito, t.fecha, t.total, t.estado,
               u.nombre, u.correo
        FROM ticket t
        JOIN usuarios u ON t.idUsuario = u.idUsuario
        WHERE t.idTicket = @idTicket";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idTicket", idTicket);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var ticket = new Ticket
            {
                IdTicket = reader.GetInt32(0),
                IdUsuario = reader.GetInt32(1),
                IdCarrito = reader.GetInt32(2),
                Fecha = reader.GetDateTime(3),
                Total = reader.GetDecimal(4),
                Estado = reader.GetString(5),
                NombreUsuario = reader.GetString(6),
                CorreoUsuario = reader.GetString(7)
            };

            // Obtener items
            ticket.Items = ObtenerItemsCarrito(ticket.IdCarrito);

            return ticket;
        }

        return null;
    }

    public List<Ticket> ObtenerTicketsPorUsuario(int idUsuario)
    {
        var tickets = new List<Ticket>();

        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT t.idTicket, t.idUsuario, t.idCarrito, t.fecha, t.total, t.estado,
               u.nombre, u.correo
        FROM ticket t
        JOIN usuarios u ON t.idUsuario = u.idUsuario
        WHERE t.idUsuario = @idUsuario
        ORDER BY t.fecha DESC";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var ticket = new Ticket
            {
                IdTicket = reader.GetInt32(0),
                IdUsuario = reader.GetInt32(1),
                IdCarrito = reader.GetInt32(2),
                Fecha = reader.GetDateTime(3),
                Total = reader.GetDecimal(4),
                Estado = reader.GetString(5),
                NombreUsuario = reader.GetString(6),
                CorreoUsuario = reader.GetString(7)
            };

            // Obtener items
            ticket.Items = ObtenerItemsCarrito(ticket.IdCarrito);

            tickets.Add(ticket);
        }

        return tickets;
    }

    // ========== MÉTODOS PARA ADMIN - ESTADÍSTICAS ==========

    public List<(string NombreProducto, int TotalVendido, decimal IngresoTotal)> ObtenerProductosMasVendidos(int limit = 5)
    {
        var resultado = new List<(string NombreProducto, int TotalVendido, decimal IngresoTotal)>();

        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT p.nombre, SUM(ci.cantidad) as total_vendido, SUM(ci.cantidad * ci.precioUnitario) as ingreso_total
        FROM carritoItems ci
        JOIN productos p ON ci.idProducto = p.idProducto
        JOIN carrito c ON ci.idCarrito = c.idCarrito
        WHERE c.estado = 'convertido'
        GROUP BY p.idProducto, p.nombre
        ORDER BY total_vendido DESC
        LIMIT @limit";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            resultado.Add((
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetDecimal(2)
            ));
        }

        return resultado;
    }

    public List<Carrito> ObtenerCarritosAbandonados(int diasDesdeCreacion = 7)
    {
        var carritos = new List<Carrito>();

        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT idCarrito, idUsuario, fechaCreacion, estado
        FROM carrito
        WHERE estado = 'activo'
        AND fechaCreacion < DATE_SUB(NOW(), INTERVAL @dias DAY)
        ORDER BY fechaCreacion DESC";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@dias", diasDesdeCreacion);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            carritos.Add(new Carrito
            {
                IdCarrito = reader.GetInt32(0),
                IdUsuario = reader.GetInt32(1),
                FechaCreacion = reader.GetDateTime(2),
                Estado = reader.GetString(3)
            });
        }

        return carritos;
    }

    public List<Producto> ObtenerProductosNuncaComprados()
    {
        var productos = new List<Producto>();

        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT p.idProducto, p.nombre, p.descripcion, p.precio, p.stock, p.categoria
        FROM productos p
        WHERE p.idProducto NOT IN (
            SELECT DISTINCT ci.idProducto
            FROM carritoItems ci
            JOIN carrito c ON ci.idCarrito = c.idCarrito
            WHERE c.estado = 'convertido'
        )
        ORDER BY p.nombre";

        using var cmd = new MySqlCommand(query, conn);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            productos.Add(new Producto
            {
                IdProducto = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Descripcion = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Precio = reader.GetDecimal(3),
                Stock = reader.GetInt32(4),
                Categoria = reader.IsDBNull(5) ? "" : reader.GetString(5)
            });
        }

        return productos;
    }

    public decimal ObtenerIngresosTotales()
    {
        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT COALESCE(SUM(total), 0)
        FROM ticket
        WHERE estado = 'pagado'";

        using var cmd = new MySqlCommand(query, conn);

        var resultado = cmd.ExecuteScalar();
        return resultado == null || resultado == DBNull.Value ? 0 : Convert.ToDecimal(resultado);
    }

    public List<Ticket> ObtenerPedidosRecientes(int limit = 10)
    {
        var tickets = new List<Ticket>();

        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT t.idTicket, t.idUsuario, t.idCarrito, t.fecha, t.total, t.estado,
               u.nombre, u.correo
        FROM ticket t
        JOIN usuarios u ON t.idUsuario = u.idUsuario
        ORDER BY t.fecha DESC
        LIMIT @limit";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var ticket = new Ticket
            {
                IdTicket = reader.GetInt32(0),
                IdUsuario = reader.GetInt32(1),
                IdCarrito = reader.GetInt32(2),
                Fecha = reader.GetDateTime(3),
                Total = reader.GetDecimal(4),
                Estado = reader.GetString(5),
                NombreUsuario = reader.GetString(6),
                CorreoUsuario = reader.GetString(7)
            };

            tickets.Add(ticket);
        }

        return tickets;
    }

    public List<Ticket> ObtenerTodosLosTickets()
    {
        var tickets = new List<Ticket>();

        using var conn = new MySqlConnection(connectionString);
        conn.Open();

        string query = @"
        SELECT t.idTicket, t.idUsuario, t.idCarrito, t.fecha, t.total, t.estado,
            u.nombre, u.correo
        FROM ticket t
        JOIN usuarios u ON t.idUsuario = u.idUsuario
        ORDER BY t.fecha DESC";

        using var cmd = new MySqlCommand(query, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var ticket = new Ticket
            {
                IdTicket = reader.GetInt32(0),
                IdUsuario = reader.GetInt32(1),
                IdCarrito = reader.GetInt32(2),
                Fecha = reader.GetDateTime(3),
                Total = reader.GetDecimal(4),
                Estado = reader.GetString(5),
                NombreUsuario = reader.GetString(6),
                CorreoUsuario = reader.GetString(7)
            };

            tickets.Add(ticket);
        }

        return tickets;
    }
}