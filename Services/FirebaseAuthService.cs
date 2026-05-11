namespace MiniTienda.Services;

using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class FirebaseAuthService
{
    private readonly HttpClient _http;

    private string apiKey = "AIzaSyCdZkl2BeuUwN5YReJuZtoQJjZHpmCzxAo";

    public FirebaseAuthService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> Login(string email, string password)
    {
        var data = new
        {
            email = email,
            password = password,
            returnSecureToken = true
        };

        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}",
            content
        );

        var result = await response.Content.ReadAsStringAsync();

        return result;
    }

    public async Task<string> Register(string email, string password)
{
    var data = new
    {
        email = email,
        password = password,
        returnSecureToken = true
    };

    var json = JsonSerializer.Serialize(data);

    var content = new StringContent(
        json,
        Encoding.UTF8,
        "application/json"
    );

    var response = await _http.PostAsync(
        $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}",
        content
    );

    return await response.Content.ReadAsStringAsync();
}
}