using NUnit.Framework;
using System.Threading.Tasks;
using System;
using RequestForge.Serialization;
using System.Text;

namespace Tests
{
    [TestFixture]
    public class RequestForgeTests
    {
        [TestCase]
        public async Task Debuggery()
        {/*
            var result = await RequestForge
                .FromBaseAddress("http://localhost:5000/")
                .WithTimeout(TimeSpan.FromSeconds(30.0d))
                .POST("/api/sessions/authenticate")
                .WithJsonBody("""
                    {
                        "Username": "tester",
                        "Password": "tf499985"
                    }
                """)
                .WithKnownHeaders(x =>
                {
                    x.Accept("application/json");
                })
                .WhenSendingRequest()
                .ThenContinueOnFailure()
                .ThenResponseStatusShouldBeOK()
                .ThenResponseHeaderHasValueEqualTo("Server", "Kestrel")
                .ThenConsumeResponseBodyAsString((statuscode,body) =>
                {
                    //Console.WriteLine("String body: " + body);
                    return true;
                })
                .ThenConsumeResponseBodyAsJson((statuscode, body) =>
                {
                    Console.WriteLine("The body is indeed valid JSON");
                    string? token = body.GetProperty("AccessToken").GetString();
                    Console.WriteLine("Token: " + token);
                    return true;
                })
                .Receive();

            Console.WriteLine($"Result content size: {result.ResponseBodyRaw.Length}");
            Console.WriteLine($"Result content type: {result.GetResponseBodyType()}");
            Console.WriteLine("Validation errors");
            Console.WriteLine(string.Join(Environment.NewLine, result.Errors));
            Console.WriteLine("Response headers:");
            Console.WriteLine(result.Headers.ToString());*/
        }

        [TestCase]
        public async Task Debuggery2()
        {
            var result = await RequestForge.Core.RequestForge
                .FromBaseAddress("https://login.microsoftonline.com/")
                .WithTimeout(TimeSpan.FromSeconds(30.0d))
                .POST("/{tenantId}/oauth2/v2.0/token")
                .WithTemplateParameter("tenantId", "9ec2cac9-3602-4bc5-87d2-9dffd83927bc")
                .WithMultipartFormBody(builder =>
                {
                    builder
                        .WithString("e3dd89f2-9da7-422d-af59-e85d717af384", "client_id")
                        .WithString(
                            Encoding.UTF8.GetString(Convert.FromBase64String("U0pGOFF+ZmozcDZNX25NMEZxaXdMbEpsSEdmdC1NeX5QVGFJWGNSSg==")),
                            Encoding.UTF8.GetString(Convert.FromBase64String("Y2xpZW50X3NlY3JldA=="))
                        )
                        .WithString("fdb60ead-2732-4353-9c77-e4a26a441bcd/.default", "scope")
                        .WithString("client_credentials", "grant_type");
                })
                .WhenSendingRequest()
                .ThenContinueOnFailure()
                .ThenResponseStatusShouldBeOK()
                .ThenConsumeResponseBodyAsJson((statuscode, body) =>
                {
                    Console.WriteLine("The body is indeed valid JSON:");
                    Console.WriteLine(JsonSerialization.Serialize(body));
                    return true;
                })
                .Receive();

            Console.WriteLine($"Result content type: {result.GetResponseBodyType()}");
            Console.WriteLine("Validation errors");
            Console.WriteLine(string.Join(Environment.NewLine, result.Errors));
            Console.WriteLine("Response headers:");
            Console.WriteLine(result.Headers.ToString());
        }
    }
}
