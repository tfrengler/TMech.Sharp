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

        //[OneTimeSetUp]
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
                .ThenResponseHeaderHasValueEqualTo("Server", "gnargle", true)
                .ThenResponseHasHeaders((_,headers) =>
                {
                    TestContext.Out.WriteLine("Second predicate having a go!");
                    return headers.Response.Location.Length > 0;
                })
                //.ThenConsumeResponseBodyAsString((statuscode,body) =>
                //{
                //    //Console.WriteLine("String body: " + body);
                //    return true;
                //})
                .ThenConsumeResponseBodyAsJson<TokenResponse>()
                .GetResult();

            //Console.WriteLine($"Result content size: {result.ResponseBodyRaw.Length}");
            //Console.WriteLine($"Result content type: {result.GetResponseBodyType()}");
            //Console.WriteLine("Validation errors");
            //Console.WriteLine(string.Join(Environment.NewLine, result.Errors));
            //Console.WriteLine("Response headers:");
            //Console.WriteLine(result.Headers.ToString());
        }
    }
}
