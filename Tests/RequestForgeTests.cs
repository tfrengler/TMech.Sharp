using NUnit.Framework;
using TMech.Sharp.RequestMonkey;
using System.Threading.Tasks;
using System;
using System.Text;

namespace Tests
{
    [TestFixture]
    public class RequestForgeTests
    {
        [TestCase]
        public async Task Debuggery()
        {
            var result = await RequestForge
                .FromBaseAddress("http://localhost:5000/api")
                .WithTimeout(TimeSpan.FromSeconds(30.0d))
                .POST("/api/sessions/authenticate")
                .WithJsonBody("""
                    {
                        "Username": "TheAdmin",
                        "Password": "gnargle"
                    }
                """)
                .WithKnownHeaders(x =>
                {
                    x.Accept("application/json");
                })
                .WhenSendingRequest()
                .ThenContinueOnFailure()
                .ThenResponseStatusShouldBeOK()
                .ThenResponseHeaderHasValueEqualTo("Server", "gnargle")
                .ThenConsumeResponseBodyAsString((statuscode,body) =>
                {
                    Console.WriteLine("String body: " + body);
                    return true;
                })
                .ThenConsumeResponseBodyAsJson((statuscode, body) =>
                {
                    Console.WriteLine("The body is indeed valid JSON");
                    return true;
                })
                .Receive();

            Console.WriteLine($"Result content size: {result.ResponseBodyRaw.Length}");
            Console.WriteLine($"Result content type: {result.GetResponseBodyType()}");
            Console.WriteLine("Validation errors");
            Console.WriteLine(string.Join(Environment.NewLine, result.Errors));
            Console.WriteLine("Response headers:");
            Console.WriteLine(result.Headers.Response.ToString());
        }
    }
}
