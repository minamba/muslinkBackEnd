using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace HelpMuslimWebApp.Api.Services
{
    // Identité du compte (prénom, nom, email, téléphone) tenue par l'IdentityServer : la fiche candidat / recruteur
    // en garde une copie. Quand la fiche est modifiée, le compte l'est aussi, avec le jeton de l'appelant.
    public interface IIdentityAccountClient
    {
        // Administration : compte d'un autre utilisateur (PUT api/admin/users/{id}/identity)
        Task UpdateAccountAsync(string userId, AccountIdentity identity);

        // Utilisateur connecté : son propre compte (PUT api/account/me), email inchangé
        Task UpdateOwnAccountAsync(string? firstName, string? lastName, string? phone);

        // Adresse email du compte connecté confirmée ? (null : serveur de connexion injoignable)
        Task<bool?> GetOwnEmailConfirmedAsync();
    }

    public record AccountIdentity(string? FirstName, string? LastName, string? Email, string? PhoneNumber);

    public class IdentityAccountClient : IIdentityAccountClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _authority;

        public IdentityAccountClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _authority = config["Identity:Authority"]!.TrimEnd('/');
        }

        public async Task UpdateAccountAsync(string userId, AccountIdentity identity)
        {
            using var client = CreateClient();
            using var response = await client.PutAsJsonAsync($"{_authority}/api/admin/users/{Uri.EscapeDataString(userId)}/identity", identity);
            await EnsureSuccessAsync(response);
        }

        public async Task UpdateOwnAccountAsync(string? firstName, string? lastName, string? phone)
        {
            using var client = CreateClient();
            // L'email du compte est relu : celui du jeton peut dater d'avant une modification
            var current = await client.GetFromJsonAsync<AccountIdentity>($"{_authority}/api/account/me")
                ?? throw new InvalidOperationException("Compte introuvable.");
            using var response = await client.PutAsJsonAsync($"{_authority}/api/account/me", current with { FirstName = firstName, LastName = lastName, PhoneNumber = phone });
            await EnsureSuccessAsync(response);
        }

        public async Task<bool?> GetOwnEmailConfirmedAsync()
        {
            try
            {
                using var client = CreateClient();
                var account = await client.GetFromJsonAsync<JsonElement>($"{_authority}/api/account/me");
                return account.TryGetProperty("emailConfirmed", out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                return null;
            }
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authorization)) client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);
            return client;
        }

        // Erreur de l'IdentityServer (email déjà pris, droits…) : son message est renvoyé tel quel
        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return;

            string? message = null;
            try
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.TryGetProperty("message", out var value)) message = value.GetString();
            }
            catch (JsonException) { /* corps non JSON */ }

            message ??= "Le compte de connexion n'a pas pu être mis à jour.";
            throw response.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => new KeyNotFoundException("Compte de connexion introuvable."),
                System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized => new UnauthorizedAccessException(message),
                System.Net.HttpStatusCode.BadRequest => new ArgumentException(message),
                _ => new InvalidOperationException(message),
            };
        }
    }
}
