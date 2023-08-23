using System;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using static System.Net.Mime.MediaTypeNames;
using Task = System.Threading.Tasks.Task;

namespace Examples
{
    public class QueriesWritesExample
    {
		const string token = "dZX70HZHCflmc6vBZEWXmz05NYoSKcx8Ru1UHuVtVX4UnOSAuSA6MLx0CTlem38tHm0EgPS4N9CzmlglFzC3xw==";
		const string bucket = "test-bucket";
		const string org = "organization";

		public async Task Write() 
        {

			var options = new InfluxDBClientOptions("http://localhost:8086/");
			options.Bucket = bucket;
			options.Org = org;
			options.Token = token;

			using var client = new InfluxDBClient(options);

			//
			// Write Data
			//
			using (var writeApi = client.GetWriteApi())
			{

				//
				// Write by Point
				//
				Random r = new Random();
				List<PointData> points = new List<PointData>();
				for (int i = 0; i < 1000; i++)
				{
					Console.WriteLine("Writing mock data: "+i);

					var point = PointData.Measurement("temperature")
					.Tag("location", "east")
					.Field("value", r.Next(-25, 25))
					.Timestamp(DateTime.UtcNow.AddSeconds(-i), WritePrecision.Ns);

					points.Add(point);
				}

				writeApi.WritePoints(points, bucket, org);
			}
		}

		public async Task<List<FluxTable>> Read()
        {
			var options = new InfluxDBClientOptions("http://localhost:8086/");
			options.Bucket = bucket;
			options.Org = org;
			options.Token = token;

			using var client = new InfluxDBClient(options);

			//
			// Query data
			//
			DateTime t = DateTime.UtcNow;
            t.AddDays(-1);
            var flux = $"from(bucket: \"test-bucket\")  |> range(start: {DateTime.UtcNow.AddSeconds(-100).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")}, stop: {DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")})  |> filter(fn: (r) => r[\"_measurement\"] == \"temperature\")  |> filter(fn: (r) => r[\"location\"] == \"east\")  |> filter(fn: (r) => r[\"_field\"] == \"value\")";

            var fluxTables = await client.GetQueryApi().QueryAsync(flux, org);
			fluxTables.ForEach(fluxTable =>
			{
				var fluxRecords = fluxTable.Records;
				fluxRecords.ForEach(fluxRecord =>
				{
					Console.WriteLine($"{fluxRecord.GetTime()}: {fluxRecord.GetValue()}");
				});
			});
			return fluxTables;

		}

		[Measurement("temperature")]
        private class Temperature
        {
            [Column("location", IsTag = true)] public string? Location { get; set; }

            [Column("value")] public double Value { get; set; }

            [Column(IsTimestamp = true)] public DateTime Time { get; set; }
        }
    }
}