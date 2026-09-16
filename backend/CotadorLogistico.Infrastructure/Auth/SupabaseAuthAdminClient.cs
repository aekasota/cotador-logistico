using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CotadorLogistico.Core.Domain;
using CotadorLogistico.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CotadorLogistico.Infrastructure.Auth;

public sealed class SupabaseAuthAdminClient : ISupabaseAuthAdminClient
{
    private static readonly JsonSerializerOptions UpdateUserJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseAuthAdminClient> _logger;

    public SupabaseAuthAdminClient(
        IHttpClientFactory httpClientFactory, IOptions<SupabaseOptions> options, ILogger<SupabaseAuthAdminClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Guid> CreateUserAsync(string email, string password, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(SupabaseAuthAdminClient));

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.AuthBaseUrl}/admin/users")
        {
            Content = JsonContent.Create(new CreateUserRequest(email, password, EmailConfirm: true))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        request.Headers.TryAddWithoutValidation("apikey", _options.SecretKey);

        using var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var created = await response.Content.ReadFromJsonAsync<CreateUserResponse>(cancellationToken: cancellationToken);
            if (created?.Id is null)
                throw new SupabaseAdminException("Conta criada, mas a resposta do Supabase não trouxe o id do usuário.");

            _logger.LogInformation("Conta de usuário criada no Supabase Auth (id={UserId}).", created.Id);
            return created.Id.Value;
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("Falha ao criar usuário no Supabase Auth: HTTP {StatusCode}.", response.StatusCode);

        if (errorBody.Contains("already been registered", StringComparison.OrdinalIgnoreCase) ||
            errorBody.Contains("already registered", StringComparison.OrdinalIgnoreCase) ||
            errorBody.Contains("email_exists", StringComparison.OrdinalIgnoreCase))
        {
            throw new SupabaseAdminException("Já existe uma conta cadastrada com este e-mail.");
        }

        throw new SupabaseAdminException("Não foi possível criar a conta agora. Tente novamente em alguns instantes.");
    }

    public async Task UpdateUserAsync(Guid userId, string? newEmail, string? newPassword, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(SupabaseAuthAdminClient));

        using var request = new HttpRequestMessage(HttpMethod.Put, $"{_options.AuthBaseUrl}/admin/users/{userId}")
        {
            Content = JsonContent.Create(
                new UpdateUserRequest(Email: newEmail, Password: newPassword, EmailConfirm: newEmail is not null ? true : null),
                options: UpdateUserJsonOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);
        request.Headers.TryAddWithoutValidation("apikey", _options.SecretKey);

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Usuário atualizado no Supabase Auth (id={UserId}).", userId);
            return;
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("Falha ao atualizar usuário no Supabase Auth: HTTP {StatusCode}.", response.StatusCode);

        if (errorBody.Contains("already been registered", StringComparison.OrdinalIgnoreCase) ||
            errorBody.Contains("already registered", StringComparison.OrdinalIgnoreCase) ||
            errorBody.Contains("email_exists", StringComparison.OrdinalIgnoreCase))
        {
            throw new SupabaseAdminException("Já existe uma conta cadastrada com este e-mail.");
        }

        throw new SupabaseAdminException("Não foi possível atualizar a conta agora. Tente novamente em alguns instantes.");
    }

    private sealed record CreateUserRequest(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("password")] string Password,
        [property: JsonPropertyName("email_confirm")] bool EmailConfirm);

    private sealed record CreateUserResponse([property: JsonPropertyName("id")] Guid? Id);

    private sealed record UpdateUserRequest(
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("password")] string? Password,
        [property: JsonPropertyName("email_confirm")] bool? EmailConfirm);
}
