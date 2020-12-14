using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Diagnostics;

namespace test_uploader
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private string _targetServiceUrl;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _targetServiceUrl = configuration.GetValue<string>("TargetServiceUrl");
        }

        public class ResponseModel
        {
            public DateTimeOffset Time { get; set; }

            public long Legnth { get; set; }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var sw = new Stopwatch();
                for (int power = 10; power < 25; power++)
                {
                    var size = (long)Math.Pow(2, power);
                    try
                    {
                        var bytes = new byte[size];
                        using var ms = new MemoryStream(bytes);
                        sw.Restart();
                        var response = await new Url(_targetServiceUrl)
                            .AppendPathSegment("api/TestFiles")
                            .PostMultipartAsync((b) => b.AddFile("file", ms, "test"))
                            .ReceiveJson<ResponseModel>();
                        sw.Stop();
                        var rate = size /(double) sw.ElapsedMilliseconds;
                        _logger.LogInformation("上傳成功 大小:{size} 費時:{timeElapsed} 速率:{rate}", size,sw.ElapsedMilliseconds, rate);
                    }
                    catch (System.Exception e)
                    {
                        sw.Stop();
                        _logger.LogError(e, "上傳失敗 大小:{size} 費時:{timeElapsed}", size,sw.ElapsedMilliseconds);
                    }
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
