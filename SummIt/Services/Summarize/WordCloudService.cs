using KnowledgePicker.WordCloud;
using KnowledgePicker.WordCloud.Coloring;
using KnowledgePicker.WordCloud.Drawing;
using KnowledgePicker.WordCloud.Layouts;
using KnowledgePicker.WordCloud.Primitives;
using KnowledgePicker.WordCloud.Sizers;
using SkiaSharp;
using SummIt.Models;

namespace SummIt.Services.Summarize;

public class WordCloudService : IWordCloudService
{
    private readonly IColorizer _colorizer = new RandomColorizer();

    public async Task<T> CreateWordCloudAsync<T>(IReadOnlyDictionary<string, int> histogram, Func<Stream, Task<T>> streamConsumer)
    {
        var wordEntries = histogram.Select(pair => new WordCloudEntry(pair.Key, pair.Value));
        var wordCloud = new WordCloudInput(wordEntries)
        {
            Width = ImageDimensions.Width,
            Height = ImageDimensions.Height,
            MinFontSize = 8,
            MaxFontSize = 32
        };
        var sizer = new LogSizer(wordCloud);
        using var engine = new SkGraphicEngine(sizer, wordCloud);
        var layout = new SpiralLayout(wordCloud);
        var generator = new WordCloudGenerator<SKBitmap>(wordCloud, engine, layout, _colorizer);
        
        using var bitmap = new SKBitmap(wordCloud.Width, wordCloud.Height);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(generator.Draw(), 0, 0);

        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        await using var stream = data.AsStream();
        return await streamConsumer(stream);
    }
}