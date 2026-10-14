using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Pidar.Services.Assistant
{
    /// <summary>
    /// How the contribution assistant runs:
    ///   Off       — not shown at all.
    ///   Mock      — test mode: no AI model, a small rule-based stand-in that recognises a few facts
    ///               (species, strain, sex, modality, numbers of animals…) so the whole flow can be tried
    ///               locally without an API key. Never used in Production unless asked for explicitly.
    ///   Anthropic — Claude through the Anthropic Messages API.
    /// </summary>
    public enum AssistantMode
    {
        Off,
        Mock,
        Anthropic,
    }

    /// <summary>
    /// Configuration section "Assistant" (appsettings.json, user secrets, or environment variables such as
    /// Assistant__ApiKey). Every value has a default; with nothing configured the assistant runs in test
    /// mode in Development and is off elsewhere.
    /// </summary>
    public sealed class AssistantOptions
    {
        public const string Section = "Assistant";

        /// <summary>Master switch. False hides the assistant everywhere.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>"auto" (default), "anthropic", "mock" or "off".
        /// auto = Anthropic when an ApiKey is set, otherwise test mode in Development and off elsewhere.</summary>
        public string Provider { get; set; } = "auto";

        /// <summary>Anthropic API key. Keep it in user secrets or an environment variable, never in git.</summary>
        public string? ApiKey { get; set; }

        /// <summary>Claude model id. A small, fast model is enough for filling a form.</summary>
        public string Model { get; set; } = "claude-haiku-5-5";

        public string BaseUrl { get; set; } = "https://api.anthropic.com/";

        /// <summary>Longest reply the model may write, per round.</summary>
        public int MaxTokens { get; set; } = 1500;

        /// <summary>Messages one user may send per minute (all submissions together).</summary>
        public int RequestsPerMinute { get; set; } = 10;

        /// <summary>Messages one user may send per 24 hours (all submissions together).</summary>
        public int MessagesPerDay { get; set; } = 150;

        /// <summary>Longest message a user may send (characters). Long enough for a pasted methods section.</summary>
        public int MaxMessageLength { get; set; } = 20000;

        /// <summary>How many earlier messages of the conversation are sent back to the model for context.</summary>
        public int HistoryMessages { get; set; } = 16;
    }

    /// <summary>The resolved settings, registered as a singleton.</summary>
    public sealed class AssistantSettings
    {
        public AssistantMode Mode { get; }
        public AssistantOptions Options { get; }

        public AssistantSettings(AssistantMode mode, AssistantOptions options)
        {
            Mode = mode;
            Options = options;
        }

        public bool IsOn => Mode != AssistantMode.Off;

        public static AssistantSettings Resolve(AssistantOptions o, IHostEnvironment env)
        {
            var hasKey = !string.IsNullOrWhiteSpace(o.ApiKey);
            var mode = !o.Enabled ? AssistantMode.Off : (o.Provider ?? "auto").Trim().ToLowerInvariant() switch
            {
                "off" => AssistantMode.Off,
                "mock" => AssistantMode.Mock,
                "anthropic" => hasKey ? AssistantMode.Anthropic : AssistantMode.Off,
                _ => hasKey ? AssistantMode.Anthropic : env.IsDevelopment() ? AssistantMode.Mock : AssistantMode.Off,
            };
            return new AssistantSettings(mode, o);
        }
    }

    public static class AssistantServiceCollectionExtensions
    {
        /// <summary>Name of the rate-limiting policy on the assistant endpoint.</summary>
        public const string RateLimitPolicy = "assistant";

        /// <summary>Registers the contribution assistant (settings, model, orchestrator).</summary>
        public static IServiceCollection AddContributionAssistant(this IServiceCollection services, IConfiguration config, IHostEnvironment env)
        {
            var options = config.GetSection(AssistantOptions.Section).Get<AssistantOptions>() ?? new AssistantOptions();
            var settings = AssistantSettings.Resolve(options, env);
            services.AddSingleton(settings);

            services.AddHttpClient<AnthropicAssistantModel>(http =>
            {
                http.BaseAddress = new Uri(options.BaseUrl);
                http.Timeout = TimeSpan.FromSeconds(90);
            });
            services.AddScoped<IAssistantModel>(sp => settings.Mode == AssistantMode.Anthropic
                ? (IAssistantModel)sp.GetRequiredService<AnthropicAssistantModel>()
                : new MockAssistantModel());

            services.AddScoped<ContributionAssistant>();

            // Per-user limit on the assistant endpoint (each message can cost an API call)
            services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                o.AddPolicy(RateLimitPolicy, http =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = Math.Max(1, options.RequestsPerMinute),
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                        }));
                o.OnRejected = async (ctx, ct) =>
                {
                    ctx.HttpContext.Response.ContentType = "application/json";
                    await ctx.HttpContext.Response.WriteAsync(
                        "{\"error\":\"You are sending messages very quickly. Please wait a minute and try again.\"}", ct);
                };
            });
            return services;
        }
    }
}
