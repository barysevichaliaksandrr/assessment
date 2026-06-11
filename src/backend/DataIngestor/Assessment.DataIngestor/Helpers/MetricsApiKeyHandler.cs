using Assessment.DataIngestor.Configuration;
using Microsoft.Extensions.Options;

namespace Assessment.DataIngestor.Helpers;

public sealed class MetricsApiKeyHandler(IOptions<MetricsApiOptions> options) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Add("X-Api-Key", options.Value.ApiKey);
        return base.SendAsync(request, cancellationToken);
    }
}