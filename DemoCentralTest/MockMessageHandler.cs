using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

internal class MockMessageHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc;

    public MockMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    {
        this.handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => handlerFunc(request, cancellationToken);
}