using System.Net;

namespace Assessment.DataIngestor.Tests.Utilities;

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return await handler(request, cancellationToken);
    }
}

internal static class HttpTestResponses
{
    public static HttpResponseMessage Json(HttpStatusCode statusCode, string content)
        => new(statusCode)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
        };
}
