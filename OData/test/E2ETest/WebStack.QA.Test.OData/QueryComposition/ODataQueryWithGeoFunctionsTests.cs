using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Dispatcher;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Builder;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using Microsoft.OData.Edm;
using Microsoft.Spatial;
using Newtonsoft.Json.Linq;
using Nuwa;
using WebStack.QA.Test.OData.Common;
using Xunit;

namespace WebStack.QA.Test.OData.QueryComposition
{
    public class ODataQueryWithGeoFunctionsTests: ODataTestBase
    {
        [NuwaConfiguration]
        public static void UpdateConfiguration(HttpConfiguration configuration)
        {
            configuration.Services.Replace(
                  typeof(IAssembliesResolver),
                  new TestAssemblyResolver(
                      typeof(AirportController)
                      ));
            configuration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            configuration.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            configuration.Count().Filter().OrderBy().Expand().MaxTop(null).Select();
            configuration.MapODataServiceRoute("selectexpand", "selectexpand", GetEdmModel(configuration));
        }

        private static IEdmModel GetEdmModel(HttpConfiguration configuration)
        {
            var builder = new ODataConventionModelBuilder(configuration);

            EntitySetConfiguration<Airport> customers = builder.EntitySet<Airport>("Airport");

            IEdmModel model = builder.GetEdmModel();
            return model;
        }

        [Fact]
        public void QueryJustThePropertiesOfTheEntriesOnAFeed()
        {
            string queryUrl = string.Format("{0}/selectexpand/Airport?$filter=geo.distance(PointLocation,geography'SRID=0;Point(142.1 64.1)') gt 0", BaseAddress);
            var response = this.Client.GetAsync(queryUrl).Result;

            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            var content = response.Content.ReadAsStringAsync().Result;
            var result = JObject.Parse(content);
            Assert.NotNull(result);
            JsonAssert.ArrayLength(10, "value", result);
            JArray airports = (JArray)result["value"];
            Assert.True(airports.OfType<JObject>().All(x => x.Properties().All(p => p.Name != "#Container.CreditRating")));
        }
    }

    public class AirportController : ODataController
    {
        public IList<Airport> Airports { get; set; }

        public AirportController()
        {
            Airports = Enumerable.Range(0, 10).Select(i => new Airport
            {
                Id = i,
                Name = string.Format("Airport{0}", i),
                PointLocation = GeographyPoint.Create(142.1, 64.2)
            }).ToList();
        }

        [EnableQuery]
        public IQueryable<Airport> Get()
        {
            var airports = Airports.AsQueryable();
            try
            {
                //var res = Airports.AsQueryable().Where(a => a.PointLocation.Distance(GeographyPoint.Create(142.1, 64.1)) > (double) 0.0);
                var res =
                    airports.FirstOrDefault(
                        a => a.PointLocation.Distance(GeographyPoint.Create(142.1, 64.1)) > (double) 0.0);
            }
            catch(Exception e)
            {
                var msg = e.Message;
            }
            return Airports.AsQueryable();
        }
    }

    public class Airport
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public GeographyPoint PointLocation { get; set; }
    }
}
