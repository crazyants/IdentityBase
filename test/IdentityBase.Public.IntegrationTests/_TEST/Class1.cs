using System.Linq;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Moq;
using ServiceBase.Logging;
using ServiceBase.Notification.Email;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using AngleSharp.Parser.Html;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServiceBase.Extensions;
using IdentityBase.Configuration;
using ServiceBase.Notification.Email;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.Diagnostics;

namespace IdentityBase.Public.IntegrationTests
{
    public class FooStartup
    {

    }

    [Collection("Lulz")]
    public class ClientStoreTests /*: IClassFixture<TestServerFixture<FooStartup>>*/
    {
        public static readonly TheoryData<IConfigurationRoot> TestConfiguration = new TheoryData<IConfigurationRoot>
        {
             ConfigBuilder.BuildConfig(),
             ConfigBuilder.Default.Alter("App:EnableLocalLogin", "false").Build(),
             ConfigBuilder.Default.Alter("App:EnableRememberLogin", "false").Build()
        };


        //[Theory]
        //[MemberData(nameof(ClientStoreTests.TestConfiguration), MemberType = typeof(ClientStoreTests))]
        public async Task Test_Some_Foo(IConfigurationRoot configuration)
        {
            var server = TestServerBuilder.BuildServer<Startup>(configuration);
            var client = server.CreateClient();

            var response = await client.GetAsync("/");
            response.EnsureSuccessStatusCode();
        }

        private string _returnUrl = "%2Fconnect%2Fauthorize%2Flogin%3Fclient_id%3Dmvc%26redirect_uri%3Dhttp%253A%252F%252Flocalhost%253A3308%252Fsignin-oidc%26response_type%3Dcode%2520id_token%26scope%3Dopenid%2520profile%2520api1%26response_mode%3Dform_post%26nonce%3D636170876883483776.ZGUwYWY2NDctNDJlNy00MTVmLTkwZTYtZjVjMTQ4ZWVlMzAwMWM2OWNhODQtYzZjOS00ZDljLTk3NTktYWE1ZWExMDEwYzk2%26state%3DCfDJ8McEKbBuVCdHkFjjPyy6vSPN5QZvt6xKTHnnKEyNzXwN1YpWo0Mslqn-wBoHhp9vMSjqo3GQGU7emMMhZlgu0BK3G03m2uqLc5vrYBz06tcWr8S4f9oKl2u1S0cAiJEOw13GnuF-EJ0E3by0nUJ3m1MhhnovobqqTEpKMldmLGpaUxPS4YGxSQVgzDzo3XsyHB4KvWlsdnb3InqNoPKnTQ4ljgDOAeKTAMj39Jz1SMauTcfOXHDyCnJdLt7I0v0up1oY5Az9b7xjzk0oBq5P7lADyq88YTEG0EALJG8SgjYi-Ch-0jd26w74LJ5UyQNScc1ZS4n9dMKUHXvuuIWllzNK86la5X-ydnsNZo2a1HsHyPT4NHe6EG2LdVkh6Y-2-A";

        [Fact]
        public async Task RecoverAccount_ConfirmMail_RedirectToLogin()
        {
            var emailServiceMock = EmailServiceHelper.GetEmailServiceMock(
                "UserAccountRecover", "alice@localhost", (templateName, email, viewData) =>
            {
                // 3 confirm the email link
                //var confirmUrl = viewData.ToDictionary()["ConfirmUrl"] as string;
                //var getResponse2 = await client.GetAsync(confirmUrl);
                //getResponse2.EnsureSuccessStatusCode();

                // 4 submit new password 
            }); 
            
            var config = ConfigBuilder.Default
                .RemoveDefaultMailService() // remove the default service since we mocking it
                .Build();

            var server = TestServerBuilder.BuildServer<Startup>(config, (services) =>
            {
                services.AddSingleton(emailServiceMock.Object);
            });
            var client = server.CreateClient();

            // 1 call the recover page
            var response = client.GetAndAssert("/recover?returnUrl=" + _returnUrl); 
            
            // 2 submit form on recover page with valid input
            var getResponseContent = await response.Content.ReadAsStringAsync();
            var doc = (new HtmlParser().Parse(getResponseContent));
            var formPostBodyData = new Dictionary<string, string>
            {
                {"Email","alice@localhost"},
                {"__RequestVerificationToken", doc.GetAntiForgeryToken() },
                {"ReturnUrl", doc.GetReturnUrl()}
            };
            var postRequest = response.CreatePostRequest("/recover", formPostBodyData);

            var postResponse = await client.SendAsync(postRequest);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Found);
            postResponse.Headers.Location.ToString().Should().StartWith("/recover/success");

            // Email service must be triggered
            emailServiceMock.Verify(c => c.SendEmailAsync("UserAccountRecover", "alice@localhost", It.IsAny<object>()), Times.Once());
        }
    }

    public static class ConfigBuilder
    {
        public static Dictionary<string, string> Default
        {
            get
            {
                var configPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                    "../../../../../src/IdentityBase.Public/Config"));

                var configData = new Dictionary<string, string>()
                {
                    { "EntityFramework:MigrateDatabase", "false" },
                    { "EntityFramework:SeedExampleData", "true" },
                    { "EntityFramework:EnsureDeleted", "true" },
                    { "EntityFramework:SeedExampleDataPath", configPath },
                    { "Services:modules:0:type", "IdentityBase.Public.EntityFramework.InMemoryModule, IdentityBase.Public.EntityFramework" },
                    { "Services:modules:1:type", "IdentityBase.Public.EntityFramework.ExampleDataStoreInitializerModule, IdentityBase.Public.EntityFramework" },
                    { "Services:modules:2:type", "IdentityBase.Public.DebugSmsModule, IdentityBase.Public" },
                    { "Services:modules:3:type", "IdentityBase.Public.DebugEmailModule, IdentityBase.Public" },
                    { "Services:modules:4:type", "IdentityBase.Public.DefaultEventModule, IdentityBase.Public" },
                };

                return configData;
            }
        }

        public static Dictionary<string, string> RemoveDefaultMailService(this Dictionary<string, string> config)
        {
            return config.RemoveByValue("IdentityBase.Public.DebugEmailModule, IdentityBase.Public").ReorderServices();
        }

        private static Dictionary<string, string> ReorderServices(this Dictionary<string, string> config)
        {
            var items = config.Where(c => c.Key.StartsWith("Services:modules:")).ToArray();

            foreach (var item in items)
            {
                config.Remove(item.Key);
            }

            for (int i = 0; i < items.Length; i++)
            {
                config.Add($"Services:modules:{i}:type", items[i].Value);
            }

            return config;
        }

        /// <summary>
        /// Alters the key
        /// </summary>
        /// <param name="config"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Dictionary<string, string> Alter(this Dictionary<string, string> config, string key, string value)
        {
            if (config.ContainsKey(key))
            {
                config[key] = value;
            }
            else
            {
                config.Add(key, value);
            }

            return config;
        }

        /// <summary>
        /// Removes the key
        /// </summary>
        /// <param name="config"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Dictionary<string, string> RemoveByKey(this Dictionary<string, string> config, string key)
        {
            if (config.ContainsKey(key))
            {
                config.Remove(key);
            }

            return config;
        }

        public static Dictionary<string, string> RemoveByValue(this Dictionary<string, string> config, string value)
        {
            var item = config.Where(e => e.Value.Equals(value, StringComparison.OrdinalIgnoreCase))
               .Select(e => (KeyValuePair<string, string>?)e)
               .FirstOrDefault();

            if (item.HasValue)
            {
                config.Remove(item.Value.Key);
            }

            return config;
        }

        /// <summary>
        /// Creates a default IdentityBase IConfigurationRoot object 
        /// </summary>
        /// <param name="config">Configuration data</param>
        /// <returns><see cref="IConfigurationRoot"/></returns>
        public static IConfigurationRoot Build(this Dictionary<string, string> config)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(config)
                .Build();
        }

        /// <summary>
        /// Creates a default IdentityBase IConfigurationRoot object 
        /// </summary>
        /// <returns><see cref="IConfigurationRoot"/></returns>
        public static IConfigurationRoot BuildConfig()
        {
            return Default.Build();
        }

    }

    public static class TestServerBuilder
    {
        public static HttpResponseMessage GetAndAssert(this HttpClient client, string url)
        {
            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            return response; 
        }

        public static TestServer BuildServer<TStartup>(IConfigurationRoot configuration)
            where TStartup : class
        {
            return BuildServer<TStartup>(configuration, null);
        }

        public static TestServer BuildServer<TStartup>(Action<IServiceCollection> configureServices)
            where TStartup : class
        {
            return BuildServer<TStartup>(null, configureServices);
        }

        public static TestServer BuildServer<TStartup>(IConfigurationRoot configuration = null,
            Action<IServiceCollection> configureServices = null) where TStartup : class
        {
            if (configuration == null)
            {
                configuration = ConfigBuilder.BuildConfig();
            }

            var contentRoot = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(), "src/IdentityBase.Public"));
            if (!Directory.Exists(contentRoot))
            {
                contentRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
                    "../../../../../src/IdentityBase.Public"));
            }

            var environment = new HostingEnvironment
            {
                EnvironmentName = "Development",
                ApplicationName = "IdentityBase.Public",
                ContentRootPath = contentRoot,
                ContentRootFileProvider = new PhysicalFileProvider(contentRoot)
            };

            var logger = new NullLogger<Startup>();

            var startup = new Startup(environment, logger);
            startup.Configuration = configuration;
            var builder = new WebHostBuilder()
                .UseContentRoot(contentRoot)
                .ConfigureServices(services =>
                {
                    configureServices?.Invoke(services);
                    services.AddSingleton<IStartup>(startup);
                });

            return new TestServer(builder);
        }
    }

    public static class EmailServiceHelper
    {
        public static Mock<IEmailService> GetEmailServiceMock(string templateName, string email, Action<string, string, object> returns = null)
        {
            var emailServiceMock = new Mock<IEmailService>();

            emailServiceMock.Setup(c => c.SendEmailAsync(templateName, email, It.IsAny<object>()))
                .Returns(new Func<string, string, object, Task>(async (templateNameOut, emailOut, viewData) =>
                 {
                     Assert.Equal(templateName, templateNameOut);
                     Assert.Equal(email, emailOut);
                     returns?.Invoke(templateNameOut, emailOut, viewData);
                 }));
                        
            return emailServiceMock;
        }
    }

    

    // (T)Activator.CreateInstance(typeof(T), option, StoreOptions))

    // https://github.com/aspnet/Hosting/issues/333#issuecomment-211727037
    //  public class TestServerFixture<TStartup> : IDisposable where TStartup : class
    //  {
    //      public TestServer Server;
    //      public HttpClient Client;
    // 
    //      public void Initialize(Action<IServiceCollection> configureServices = null)
    //      {
    //          //    TestServerBuilder.BuildServer<TStartup>(configureServices);
    // 
    //      }
    // 
    //      public void Dispose()
    //      {
    //          /*foreach (var option in Options.ToList())
    //          {
    //              using (var context = (T)Activator.CreateInstance(typeof(T), option, StoreOptions))
    //              {
    //                  context.Database.EnsureDeleted();
    //              }
    //          }*/
    //      }
    //  }
}
