using System.Net.Http.Headers;
using JetBrains.Space.Client;
using JetBrains.Space.Common;
using Microsoft.AspNetCore.StaticFiles;

namespace SummIt.Extensions;

public static class SpaceUploadClientExtensions
{
    private static readonly FileExtensionContentTypeProvider FileExtensionContentTypeProvider = new();

    public static async Task<string> UploadImageAsync(
        this UploadClient uploadClient,
        string storagePrefix,
        string fileName,
        Stream uploadStream
    )
    {
        if (!FileExtensionContentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "image/gif";
        }

        return await uploadClient.UploadAsync(
            storagePrefix,
            fileName,
            uploadStream,
            httpClient: new ContentEditingHttpClient(
                SharedHttpClient.Instance,
                uploadStream,
                contentType
            )
        );
    }

    private class ContentEditingHttpClient : HttpClient
    {
        private readonly HttpClient _decoratedClient;
        private readonly Stream _uploadStream;
        private readonly string _contentType;

        public ContentEditingHttpClient(HttpClient decoratedClient, Stream uploadStream, string contentType)
        {
            _decoratedClient = decoratedClient;
            _uploadStream = uploadStream;
            _contentType = contentType;
        }

        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Content = new StreamContent(_uploadStream)
            {
                Headers = { ContentType = new MediaTypeHeaderValue(_contentType) }
            };
            return _decoratedClient.SendAsync(request, cancellationToken);
        }
    }
}