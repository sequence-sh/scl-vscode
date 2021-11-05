//using System.IO.Abstractions;
//using System.Threading.Tasks;
//using FluentAssertions;
//using LanguageServer.Services;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Configuration.Json;
//using Microsoft.Extensions.Logging.Abstractions;
//using Reductech.EDR.Core;
//using Xunit;

//namespace LanguageServer.Test
//{
//    public class StepFactoryStoreFactoryTests
//    {
//        [Fact]
//        public async Task TestDynamicAsync()
//        {
//            var json = @"{
//    ""Reductech.EDR.Connectors.REST"": {
//      ""Id"": ""Reductech.EDR.Connectors.REST"",
//      ""Version"": ""0.12.0-a.main.2111021310"",
//      ""Settings"": {
//        ""Specifications"": [{
//            ""Name"": ""Reveal"",
//            ""BaseURL"": ""http://baseURL"",
//            ""SpecificationURL"": ""https://salient-eu.revealdata.com/rest/swagger/docs/V2""
//          }]
//      }
//    }
//  }";

//            var stringStream = new StringStream(json);

//            IConfiguration configuration = new ConfigurationRoot(new[]
//            {
//                new JsonStreamConfigurationProvider(new JsonStreamConfigurationSource()
//                {
//                    Stream = stringStream.GetStream().stream
//                })
//            });

//            var factory = new StepFactoryStoreFactory(
//                new EntityChangeSync<SCLLanguageServerConfiguration>(configuration),
//                new NullLoggerFactory(),
//                NullLogger<StepFactoryStoreFactory>.Instance,
//                new FileSystem()
//            );

//            var sfs = await factory.GetValueAsync();

//            sfs.Dictionary.Should().NotBeEmpty();
//        }

//    }
//}
