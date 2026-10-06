using System.Net;
using System.Text;

namespace Herald.Core.Test.Unit;

/// <summary>
/// Answers every request from a function instead of from the network, and keeps the last request,
/// so that a test can assert on the url and the headers herald sent. Shared, because the lesson in
/// SendAsync below is one nobody should have to learn twice.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    private StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

    public HttpRequestMessage? LastRequest { get; private set; }

    public static StubHttpMessageHandler Returning(
        string body,
        HttpStatusCode status = HttpStatusCode.OK,
        IReadOnlyDictionary<string, string>? headers = null) =>
        new(_ =>
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            foreach (var (name, value) in headers ?? new Dictionary<string, string>())
            {
                response.Headers.TryAddWithoutValidation(name, value);
            }

            return response;
        });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;

        var response = _respond(request);

        // Refit builds its exception from the request behind the response, so a stub that leaves it
        // unset turns every error case into a confusing InvalidOperationException.
        response.RequestMessage = request;

        return Task.FromResult(response);
    }
}
