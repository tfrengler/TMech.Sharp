using NUnit.Framework;
using System.Threading.Tasks;
using System;
using SharpCompress.Compressors.ZStandard.Unsafe;

namespace Tests
{
    sealed record TokenResponse
    {
        public string AccessToken {get;set;} = string.Empty;
        public string AccessTokenExpires {get;set;} = string.Empty;
        public string RefreshToken {get;set;} = string.Empty;
        public string RefreshTokenExpires { get; set; } = string.Empty;
    }

    [TestFixture]
    public sealed class RequestForgeTests
    {
        private TokenResponse _ApiToken = null!;

        [OneTimeSetUp]
        public async Task BeforeAll()
        {
            var result = await RequestForge.Core.RequestForge
                .FromBaseAddress("http://localhost:5000/")
                .WithTimeout(TimeSpan.FromSeconds(10.0d))
                .POST("/api/sessions/authenticate")
                .WithJsonBody("""
                    {
                        "Username": "tfrengler",
                        "Password": "tf499985"
                    }
                """)
                .WithKnownHeaders(x =>
                {
                    x.Accept("application/json");
                })
                .WhenSendingRequest()
                .ThenResponseStatusShouldBeOK()
                .ThenConsumeResponseBodyAsJson<TokenResponse>((_, body) =>
                {
                    Console.WriteLine("AccessToken: " + body.AccessToken);
                    Console.WriteLine("AccessTokenExpires: " + body.AccessTokenExpires);
                    Console.WriteLine("RefreshToken: " + body.RefreshToken);
                    Console.WriteLine("RefreshTokenExpires: " + body.RefreshTokenExpires);
                    _ApiToken = body;

                    return true;
                })
                .GetResult();

            Assert.That(result.Errors, Is.Empty);
        }

        [Test(Description = "Doing debugging")]
        public async Task Debuggery()
        {
            var result = await RequestForge.Core.RequestForge
                .FromBaseAddress("http://localhost:5000/")
                .WithTimeout(TimeSpan.FromSeconds(30.0d))
                .POST("/api/sessions/authenticate")
                .WithJsonBody("""
                    {
                        "Username": "tfrengler",
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
                //.ThenConsumeResponseBodyAsString((statuscode,body) =>
                //{
                //    //Console.WriteLine("String body: " + body);
                //    return true;
                //})
                .ThenConsumeResponseBodyAsJson<TokenResponse>((statuscode, body) =>
                {
                    Console.WriteLine("AccessToken: " + body.AccessToken);
                    Console.WriteLine("AccessTokenExpires: " + body.AccessTokenExpires);
                    Console.WriteLine("RefreshToken: " + body.RefreshToken);
                    Console.WriteLine("RefreshTokenExpires: " + body.RefreshTokenExpires);

                    return true;
                })
                .GetResult();

            Console.WriteLine($"Result content size: {result.ResponseBodyRaw.Length}");
            Console.WriteLine($"Result content type: {result.GetResponseBodyType()}");
            Console.WriteLine("Validation errors");
            Console.WriteLine(string.Join(Environment.NewLine, result.Errors));
            Console.WriteLine("Response headers:");
            Console.WriteLine(result.Headers.ToString());
        }

        [Test(Description = "A second debugging")]
        public async Task Debuggery2()
        {
            /*
            var result = await RequestForge.Core.RequestForge
                .FromBaseAddress("https://login.microsoftonline.com/")
                .WithTimeout(TimeSpan.FromSeconds(30.0d))
                .POST("/{tenantId}/oauth2/v2.0/token")
                .WithTemplateParameter("tenantId", "9ec2cac9-3602-4bc5-87d2-9dffd83927bc")
                .WithMultipartFormBody(builder =>
                {
                    builder
                        .WithString(
                            Encoding.UTF8.GetString(Convert.FromBase64String("ZTNkZDg5ZjItOWRhNy00MjJkLWFmNTktZTg1ZDcxN2FmMzg0")),
                            Encoding.UTF8.GetString(Convert.FromBase64String("Y2xpZW50X2lk"))
                        )
                        .WithString(
                            Encoding.UTF8.GetString(Convert.FromBase64String("U0pGOFF+ZmozcDZNX25NMEZxaXdMbEpsSEdmdC1NeX5QVGFJWGNSSg==")),
                            Encoding.UTF8.GetString(Convert.FromBase64String("Y2xpZW50X3NlY3JldA=="))
                        )
                        .WithString(
                            Encoding.UTF8.GetString(Convert.FromBase64String("ZmRiNjBlYWQtMjczMi00MzUzLTljNzctZTRhMjZhNDQxYmNkLy5kZWZhdWx0")),
                            Encoding.UTF8.GetString(Convert.FromBase64String("c2NvcGU="))
                        )
                        .WithString(
                            Encoding.UTF8.GetString(Convert.FromBase64String("Y2xpZW50X2NyZWRlbnRpYWxz")),
                            Encoding.UTF8.GetString(Convert.FromBase64String("Z3JhbnRfdHlwZQ=="))
                        );
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
            */
        }
    }
}
