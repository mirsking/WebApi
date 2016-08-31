using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using Microsoft.Spatial;
using Newtonsoft.Json.Linq;
using Nuwa;
using WebStack.QA.Test.OData.Common;
using Xunit;

namespace WebStack.QA.Test.OData.QueryComposition
{
    public class ODataQueryWithGeoFunctionsTests : ODataTestBase
    {
        [NuwaConfiguration]
        public static void UpdateConfiguration(HttpConfiguration configuration)
        {
            configuration.Services.Replace(
                typeof(IAssembliesResolver),
                new TestAssemblyResolver(
                    typeof(AirportController)));
            configuration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            configuration.Count().Filter().OrderBy().Expand().MaxTop(null).Select();
            //configuration.AddODataQueryFilter();
            //configuration.EnableDependencyInjection();
            configuration.MapODataServiceRoute("Spatial", "api/Spatial", GetEdmModel(configuration));
        }

        [Fact]
        public void OptionsOnIEnumerableTShouldWork()
        {
            /*
            var response = this.Client.GetAsync(this.BaseAddress + "/api/Spatial/Airports").Result;//?$filter=geo.distance(PointLocation,geography'SRID=0;Point(142.1 64.1)') gt 0").Result;
            var actual = response.Content.ReadAsAsync<IEnumerable<Airport>>().Result;
            Assert.Equal(10, actual.Count());
            */

            //string queryUrl = string.Format("{0}/selectexpand/SelectCustomer/?$select=*", BaseAddress);
            string queryUrl = string.Format("{0}/api/Spatial/Airports", BaseAddress);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, queryUrl);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata.metadata=full"));
            HttpClient client = new HttpClient();
            HttpResponseMessage response;
            JObject result;
            string content;

            response = client.SendAsync(request).Result;
            Assert.Equal(response.StatusCode, HttpStatusCode.OK);

            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            content = response.Content.ReadAsStringAsync().Result;

            result = JObject.Parse(content);
            Assert.NotNull(result);
            JsonAssert.ArrayLength(10, "value", result);
            JArray customers = (JArray)result["value"];
            Assert.True(customers.OfType<JObject>().All(x => x.Properties().All(p => p.Name != "#Container.CreditRating")));

        }

        public class AirportController : ODataController
        {
            public IList<Airport> Airports { get; set; }

            public AirportController()
            {
                Airports = Enumerable.Range(0, 10).Select(i => new Airport
                {
                    Id = string.Format("{0}, i"),
                    Name = string.Format("Airport{0}", i),
                }).ToList();
            }

            [EnableQuery]
            public IQueryable<Airport> Get()
            {
                return Airports.AsQueryable();
            }
        }


        public class Airport
        {
            public string Id { get; set; }

            public string Name { get; set; }

            public GeographyPoint PointLocation { get; set; }
        }

        private static Microsoft.OData.Edm.IEdmModel GetEdmModel(HttpConfiguration configuration)
        {
            var builder = new ODataConventionModelBuilder(configuration);
            builder.EntitySet<Airport>("Airports");
            return builder.GetEdmModel();
        }
    }
}
