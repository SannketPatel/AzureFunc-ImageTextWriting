// using SixLabors.ImageSharp 
// using SixLabors.ImageSharp.Drawing

public class ImageTextTry
{
    private readonly ILogger<ImageTextTry> _logger;

    public ImageTextTry(ILogger<ImageTextTry> logger)
    {
        _logger = logger;
    }

    private void AddCorsHeaders(HttpResponseData response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*"); // or specific domain
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
    }

    [Function("ImageTextTry")]    
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        try
        {

            // 1. Handle CORS Preflight
            if (req.Method == HttpMethods.Options)
            {
                return new OkResult(); // Headers added by host.json
            }

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var payload = JsonSerializer.Deserialize<ImageTextPayload>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            byte[] resultImageBytes = GenerateImageWithText(payload, payload.Text, 600);

            var result = new FileContentResult(resultImageBytes, "image/png");
            result.FileDownloadName = "output.png";
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message + ex.StackTrace);
        }
        return new OkResult();
    }

    private byte[] GenerateImageWithText(ImageTextPayload payload, string text, int targetWidth)
    {
        byte[] imageBytes = Convert.FromBase64String(payload.ImageBase64.Split(',')[1]);

        using var image = Image.Load<Rgba32>(imageBytes);
        float scale = (float)targetWidth / payload.Width;
        int newWidth = (int)(payload.Width * scale);
        int newHeight = (int)(payload.Height * scale);

        image.Mutate(x => x.Resize(newWidth, newHeight));

        float fontSize = payload.FontSizeRatio * newWidth;
        float textX = payload.X * scale;
        float textY = payload.Y * scale;

        var fontCollection = new FontCollection();
        string fontPath = Path.Combine("fonts", $"{payload.FontFamily}.ttf");

        if (!File.Exists(fontPath))
            throw new FileNotFoundException($"Font file not found: {fontPath}");

        FontFamily fontFamily = fontCollection.Add(fontPath);
        Font font = fontFamily.CreateFont(fontSize, FontStyle.Regular);

        var textOptions = new RichTextOptions(font)
        {
            Origin = new PointF(textX, textY),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        var color = Color.Parse(payload.FontColor);
        image.Mutate(ctx => ctx.DrawText(textOptions, text, color));

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }


    public class ImageTextPayload
    {
        public string ImageBase64 { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public int Width { get; set; } // original rendered width
        public int Height { get; set; }
        public string Text { get; set; }
        public float FontSize { get; set; }
        public float FontSizeRatio { get; set; }
        public string FontColor { get; set; }
        public string FontFamily { get; set; }
    }
}
