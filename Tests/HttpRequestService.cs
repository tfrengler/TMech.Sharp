using NUnit.Framework;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tests.HttpRequestServiceScenarios;

[Parallelizable(ParallelScope.Children)]
[TestFixture]
public class HttpRequestServiceTests
{
    private sealed record TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string AccessTokenExpires { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string RefreshTokenExpires { get; set; } = string.Empty;
    }

    private TMech.Sharp.HttpRequestService.HttpService _httpService = null!;

    [OneTimeSetUp]
    public void BeforeAll()
    {
        //_httpService = new TMech.Sharp.HttpRequestService.HttpService("http://localhost:5000/api");
        _httpService = new TMech.Sharp.HttpRequestService.HttpService("https://portalservice.pgb.local.df12.lf.lan/api");
    }

    private sealed record AanmakenAccountViewModel
    {
        public List<ApplicatieRolType> ApplicatieRollen { get; } = new List<ApplicatieRolType>();
        public RolType Rol { get; set; }
        public string Gebruikersnaam { get; set; } = string.Empty;
        public string Wachtwoord { get; set; } = string.Empty;
        public string BevestigWachtwoord { get; set; } = string.Empty;
        public AanmakenAccountRelatieViewModel Relatie { get; set; } = new AanmakenAccountRelatieViewModel();
    }

    private enum ApplicatieRolType
    {
        Onbekend = 0,
        RaadplegenKlantbeeld = 1,    // Svb
        MedewerkerRegioteam = 2,     // Svb
        WerkvoorraadOverig = 3,      // Svb
        GemnetPortaalRaadplegen = 4,
        GemnetPortaalMuteren = 5,
        GemnetPortaalManagement = 6,
        FunctioneelApplicatiebeheerder = 7,
        ZorgkantoorRaadplegen = 8,
        ZorgkantoorMuteren = 9,
        ZorgkantoorGoedkeuren = 10,
        ZorgkantoorSuperuser = 11,
        GemnetPortaalRaadplegenJw = 12,
        GemnetPortaalRaadplegenWmo = 13,
        GemnetPortaalMuterenJw = 14,
        GemnetPortaalMuterenWmo = 15,
        GemnetPortaalManagementJw = 16,
        GemnetPortaalManagementWmo = 17
    }

    private enum RolType
    {
        Onbekend = 0,
        Budgethouder = 1,
        Vertegenwoordiger = 2,
        Zorgverlener = 4,
        PortaalMedewerker = 5,
        FunctioneelApplicatiebeheerder = 8,
        ZorgkantoorMedewerker = 9,
        GemeenteMedewerker = 10,
        Systeemgebruiker = 99
    }

    private sealed record AanmakenAccountRelatieViewModel
    {
        public string BurgerServiceNummerBudgethouder { get; set; } = string.Empty;
        public string BsnVertegenwoordiger { get; set; } = string.Empty;
        public string KvkVertegenwoordiger { get; set; } = string.Empty;
        public string AgbZorgverlener { get; set; } = string.Empty;
        public string KvkZorgverlener { get; set; } = string.Empty;
        public string BurgerServiceNummerZorgverlener { get; set; } = string.Empty;
        public string VerstrekkerCode { get; set; } = string.Empty;
        public string VoorlettersMedewerker { get; set; } = string.Empty;
        public string AchternaamMedewerker { get; set; } = string.Empty;
        public GeslachtType? GeslachtMedewerker { get; set; }
    }

    private enum GeslachtType : byte
    {
        Onbekend = 0,
        Man = 1,
        Vrouw = 2
    }

    [Test]
    public async Task Debug()
    {
        var payload = JsonSerializer.Serialize(new AanmakenAccountViewModel()
        {
            ApplicatieRollen = { ApplicatieRolType.FunctioneelApplicatiebeheerder },
            Rol = RolType.Budgethouder,
            Gebruikersnaam = "thomastest",
            Wachtwoord = "Tester123#",
            BevestigWachtwoord = "Tester123#"
        },
        new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        });

        TestContext.Out.WriteLine("=== PAYLOAD ===");
        TestContext.Out.WriteLine(payload);

        var response = await _httpService
            .NewPostRequest("Account/AanmakenAccount")
            .WithJsonBody(payload)
            .Send();

        if (!response.IsSuccessResponse())
        {
            TestContext.Out.WriteLine("ERROR: Did not get a success response");
            if (response.ConsumeAsRawBytes().Length > 0)
            {
                TestContext.Out.WriteLine("=== RESPONSE BODY ===");
                TestContext.Out.WriteLine(response.ConsumeAsString());
                return;
            }
        }

        var responseObject = response.ConsumeAsJson<JsonElement>();

        TestContext.Out.WriteLine(JsonSerializer.Serialize(responseObject, new JsonSerializerOptions()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Converters = { new JsonStringEnumConverter() }
        }));
    }

    /*
    [Test]
    public async Task Debug()
    {
        TokenResponse tokenResponse;
        var response = await _httpService
            .NewPostRequest("sessions/authenticate")
            .WithJsonBody("""
                {
                    "Username": "tfrengler",
                    "Password": "tf499985"
                }
            """)
            .Send();

        Assert.That(
            response.IsSuccessResponse(),
            Is.True,
            $"Expected response to be OK but it was {response.GetStatusCodeAsInt()}"
        );

        tokenResponse = response.ConsumeAsJson<TokenResponse>();

        TestContext.Out.WriteLine(tokenResponse);
    }
    */
}