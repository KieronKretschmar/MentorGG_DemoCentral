using System;
using System.Collections.Generic;
using System.Net.Http;

namespace DemoCentralTests
{
    class MockHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string,HttpClient> _clients;
        public MockHttpClientFactory()
        {
            _clients = new Dictionary<string, HttpClient>();
        }

        public void RegisterClient(string name, HttpClient client)
        {
            _clients[name] = client;
        }


        public HttpClient CreateClient(string name)
        {
            return _clients[name];
        }
    }
}
