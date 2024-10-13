using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text.Json;
using WebApplication1.Models;

namespace WebApplication1.Controllers;
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    [HttpPost]
    [Route(nameof(UploadFileInTemp))]
    public async Task<IActionResult> UploadFileInTemp(List<IFormFile> files)
    {
        long size = files.Sum(f => f.Length);

        foreach (var formFile in files)
        {
            if (formFile.Length > 0)
            {
                var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

                using (var stream = System.IO.File.Create(filePath)) //Disk
                {
                    await formFile.CopyToAsync(stream);
                }
            }
        }

        // Process uploaded files
        // Don't rely on or trust the FileName property without validation.

        return Ok(new { count = files.Count, size });
    }

    [HttpPost]
    [Route(nameof(UploadFileInMemory))]
    public async Task<IActionResult> UploadFileInMemory(IFormFile file)
    {
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);

            // Upload the file if less than 2 MB
            if (memoryStream.Length < 2097152)
            {
                var content = memoryStream.ToArray();

                //_dbContext.File.Add(file);

                //await _dbContext.SaveChangesAsync();
            }
            else
            {
                ModelState.AddModelError("File", "The file is too large.");
            }
        }

        return Ok();
    }

    [HttpPost]
    [Route(nameof(UploadFileInStream))]
    public async Task<IActionResult> UploadFileInStream(IFormFile file)
    {
        using (var streamReader = new StreamReader(
                   file.OpenReadStream(),
                   detectEncodingFromByteOrderMarks: true,
                   bufferSize: 1024,
                   leaveOpen: true))
        {
            var value = await streamReader.ReadToEndAsync();
        }

        return Ok();
    }

    [HttpPost]
    [Route(nameof(UploadFileWithZipFile))]
    public async Task<IActionResult> UploadFileWithZipFile(IEnumerable<IFormFile> files)
    {
        using (ZipArchive archive = new ZipArchive(files.FirstOrDefault()!.OpenReadStream()))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                Stream s = entry.Open();
                var sr = new StreamReader(s);
                var myStr = sr.ReadToEnd();
            }
        }

        if (Request.Form.Files.Count > 0 && Request.Form.Files[0].ContentType.ToLower() == "application/x-zip-compressed")
        {
            for (int i = 0; i < Request.Form.Files.Count; i++)
            {
                var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".zip");

                using (var stream = System.IO.File.Create(filePath)) //Disk
                {
                    await Request.Form.Files[i].CopyToAsync(stream);
                }

                using var zipFile = ZipFile.OpenRead(filePath);

                foreach (var entry in zipFile.Entries)
                {
                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        using (var stream1 = entry.Open())
                        using (var memoryStream1 = new MemoryStream())
                        {
                            stream1.CopyTo(memoryStream1);
                            var bytes = memoryStream1.ToArray();
                            var base64 = Convert.ToBase64String(bytes);
                            Console.WriteLine($"{entry.FullName} => {base64}");
                        }
                    }
                }
            }
        }
        return Ok();
    }


    [HttpPost]
    [Route(nameof(UploadJsonFile))]
    public async Task<IActionResult> UploadJsonFile(IFormFile files)
    {
        using StreamReader reader = new(files.OpenReadStream());

        var json = await reader.ReadToEndAsync().ConfigureAwait(false);

        var persons = JsonSerializer.Deserialize<List<PersonDto>>(json);

        return Ok(persons);
    }

    [HttpPost]
    [Route(nameof(UploadJsonZipFile))]
    public async Task<IActionResult> UploadJsonZipFile(IFormFile file)
    {
        using (ZipArchive archive = new ZipArchive(file.OpenReadStream()))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                Stream s = entry.Open();
                var sr = new StreamReader(s);

                var json = await sr.ReadToEndAsync().ConfigureAwait(false);
                var persons = JsonSerializer.Deserialize<List<PersonDto>>(json);

                return Ok(persons);
            }
        }

        return Ok();
    }
}

