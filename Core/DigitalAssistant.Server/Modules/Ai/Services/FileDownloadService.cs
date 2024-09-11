using BlazorBase.MessageHandling.Interfaces;
using Microsoft.Extensions.Localization;
using System.Net.Http.Handlers;

namespace DigitalAssistant.Server.Modules.MessageHandling.Components;

public class FileDownloadService(IStringLocalizer<FileDownloadService> localizer, IMessageHandler messageHandler)
{
    #region Injects
    protected readonly IStringLocalizer<FileDownloadService> Localizer = localizer;
    protected readonly IMessageHandler MessageHandler = messageHandler;
    #endregion

    public async Task<bool> TestFileExistsAsync(string uri)
    {
        var client = new HttpClient();
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DownloadFileAsync(string fileName, string uri, string destination)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var httpClientHandler = new HttpClientHandler();
        var progressMessageHandler = new ProgressMessageHandler(httpClientHandler);
        var progress = 0;

        var directory = Path.GetDirectoryName(destination);
        ArgumentNullException.ThrowIfNullOrEmpty(directory);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var progressIndicatorId = MessageHandler.ShowLoadingProgressMessage(Localizer["Downloading file {0}", fileName], onAborting: (id) =>
        {
            return cancellationTokenSource.CancelAsync();
        });

        try
        {
            progressMessageHandler.HttpReceiveProgress += (_, args) =>
            {
                var oldProgress = progress;
                if (args.TotalBytes != null)
                    progress = (int)((double)args.BytesTransferred / args.TotalBytes * 100);

                if (progress == oldProgress)
                    return;

                var mbTransferred = (int)(args.BytesTransferred / 1048576);
                var mbTotal = (int?)(args.TotalBytes / 1048576) ?? 0;
                MessageHandler.UpdateLoadingProgressMessage(progressIndicatorId, Localizer["Downloading file {0}", fileName], progress, $"{progress} % ({mbTransferred} / {mbTotal} mb)", showProgressInText: false);
            };

            var client = new HttpClient(progressMessageHandler);
            using var responseStream = await client.GetStreamAsync(uri, cancellationTokenSource.Token);
            using var fs = File.Create(destination);
            await responseStream.CopyToAsync(fs, cancellationTokenSource.Token);
        }
        catch (Exception)
        {
            try
            {
                File.Delete(destination);
            }
            catch (Exception) { }
            return false;
        }
        finally
        {
            MessageHandler.CloseLoadingProgressMessage(progressIndicatorId);
        }

        return true;
    }
}
