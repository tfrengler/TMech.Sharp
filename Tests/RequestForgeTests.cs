using NUnit.Framework;
using TMech.Sharp.RequestMonkey;
using System.Threading.Tasks;

namespace Tests
{
    [TestFixture]
    public sealed class RequestForgeTests
    {
        public async Task Debuggery()
        {
            var test = RequestForge
                .FromBaseAddress("http://localhost:5000/api")
                .WithTimeout(System.TimeSpan.FromSeconds(30.0d))
                .GET("/filesandfolders/enumerate")
                .WithUrlParameter("relativePath", "gnargle")
                .WithUrlParameter("parent", false)
                .Send();
        }
    }
}
