using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace HttpHost.Controllers
{
    [Route("")]
    [ApiController]
    public class ProxyController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public ProxyController(ILogger<ProxyController> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        // GET api/values
        [HttpGet]
        [Route("health")]
        public ActionResult<IEnumerable<string>> Get()
        {
            return Ok("Healthy");
        }

        [HttpPost]
        public ActionResult PostFile(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            {
                var extension = _configuration.GetValue<string>("OutputExtension");
                var command = _configuration.GetValue<string>("Command");
                var argsFormat = _configuration.GetValue<string>("Arguments");
                var timeoutInMs = _configuration.GetValue<int>("TimeoutInMs");
                var name = file.FileName;
                var tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(),
                    Guid.NewGuid().ToString()));
                tempDir.Create();
                tempDir.Refresh();
                var input = GetTempFileWithExtension(tempDir, name);
                var output = GetTempFileWithExtension(tempDir, name, extension);

                try
                {
                    using (var inputStream = input.Create())
                    {
                        stream.CopyTo(inputStream);
                    }

                    var exitCode = Execute(command, string.Format(argsFormat, input.FullName,
                        output.FullName), timeoutInMs);
                    if (exitCode != 0)
                        throw new InvalidOperationException($"Command Returned: {exitCode}");

                    output.Refresh();
                    if (!output.Exists) throw
                        new InvalidOperationException("Unable to convert file");

                    var memoryStream = new MemoryStream();
                    using (var outputStream = output.OpenRead())
                    {
                        outputStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        return File(memoryStream, "application/octet-stream",
                            string.Concat(name, extension));
                    }
                }
                finally
                {
                    input.Delete();
                    output.Delete();
                    tempDir.Delete();
                }
            }
        }

        private int Execute(string command, string arguments, int timeoutInMs)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = Path.GetFileName(command);
                process.StartInfo.Arguments = arguments;
                _logger.LogInformation("Executing -> {0} {1}", command, arguments);
                var watch = new Stopwatch();
                watch.Start();
                process.Start();
                process.WaitForExit(timeoutInMs);
                watch.Stop();
                var exitCode = process.ExitCode;
                _logger.LogInformation(
                    $"Executed [{command} {arguments}] -> [{exitCode}] {watch.ElapsedMilliseconds}ms");
                return exitCode;
            }
        }

        private FileInfo GetTempFileWithExtension(DirectoryInfo di, string name, string extension = null)
        {
            return new FileInfo(Path.Combine(
                    di.FullName,
                    string.Concat(
                        Path.GetFileNameWithoutExtension(name),
                        extension == null ? Path.GetExtension(name) : extension)));
        }
    }
}
