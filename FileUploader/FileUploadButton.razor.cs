using System.IO.Compression;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileUploader;


public partial class FileUploadButton
{
    private string acceptedFileTypes = "*";
    private int progress;
    private string status;

    [Inject]
    public required IJSRuntime JSRuntime { get; set; }

    [Parameter]
    public EventCallback<string> OnProgressChange { get; set; }

    [Parameter]
    public EventCallback<string> OnUploadCompleted { get; set; }

    [Parameter]
    public EventCallback<Exception> OnException { get; set; }

    [Parameter]
    public EventCallback<string> OnFileWarning { get; set; }

    [Parameter]
    public int MaxFileSize { get; set; }

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public IEnumerable<string> AcceptedFileTypes { get; set; } = new List<string>() { "*" };

    [Parameter]
    public required string OutputBasePath { get; set; }
    public FileUploaderJsInterop? FileUploaderJsInterop { get; set; }

    public string? ElementId { get; set; }
    public bool Initialized { get; private set; }

    public bool ResetFileInput { get; set; }
    protected override void OnInitialized()
    {
        ElementId = $"file_input_{Guid.NewGuid()}";
        SetFileTypes();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                FileUploaderJsInterop = new FileUploaderJsInterop(JSRuntime);

            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the process
                Console.WriteLine($"Exception: {ex.Message}");
                await OnException.InvokeAsync(ex);
            }

        }
    }

    private async Task InitFileUpload(ChangeEventArgs changeEventArgs)
    {
        try
        {
            if (ElementId is not null)
            {

                var fileUploadButtonReference = DotNetObjectReference.Create(this);
                if (FileUploaderJsInterop is not null)
                {
                    await FileUploaderJsInterop.Init(ElementId, fileUploadButtonReference);
                }

            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during the process
            Console.WriteLine($"Exception: {ex.Message}");
            await OnException.InvokeAsync(ex);
        }

    }

    [JSInvokable]
    public async Task SaveFile(string fileName, string fileType, long fileSize)
    {
        try
        {
            Console.WriteLine($"Saving file: {fileName}, Type: {fileType}, Size: {fileSize}");

            // Get the file data stream from the JavaScript function
            var dataReference = await JSRuntime.InvokeAsync<IJSStreamReference>("fileDataStream");
            await using var dataReferenceStream = await dataReference.OpenReadStreamAsync(maxAllowedSize: fileSize);

            // Define the base directory and ensure it exists
            var baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            // Define the compressed file path
            var compressedFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.zip";
            var compressedFilePath = Path.Combine(baseDirectory, compressedFileName);

            // Open a file stream to write the compressed file
            await using var outputFileStream = File.OpenWrite(compressedFilePath);

            var buffer = new byte[81920]; // 80 KB buffer size
            long totalBytesRead = 0;
            int bytesRead;
            double lastReportedPercentage = 0;

            // Read the data from the dataReferenceStream and write it to the outputFileStream
            while ((bytesRead = await dataReferenceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await outputFileStream.WriteAsync(buffer, 0, bytesRead);
                totalBytesRead += bytesRead;
                var percentage = Math.Ceiling((double)totalBytesRead / fileSize * 100);
                percentage = Math.Min(percentage, 100);

                if (percentage != lastReportedPercentage)
                {
                    var progress = $"Uploading {percentage}%";
                    Console.WriteLine(progress);
                    await OnProgressChange.InvokeAsync(progress);
                    lastReportedPercentage = percentage;
                }
            }

            // Flush and close the output file stream before decompression
            await outputFileStream.FlushAsync();

            // Define the output base path and ensure it exists
            var OutputBasePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "extracted");
            if (!Directory.Exists(OutputBasePath))
            {
                Directory.CreateDirectory(OutputBasePath);
            }

            // Define the output file name and decompress the file
            var outputFileName = Path.Combine(OutputBasePath, Path.GetFileName(fileName));

           

            await DecompressGzipFile(compressedFilePath, outputFileName);


        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during the process
            Console.WriteLine($"Exception: {ex.Message}");
            await OnException.InvokeAsync(ex);
        }
    }


    public async Task DecompressGzipFile(string sourceFile, string destinationFile)
    {
        try
        {
            await using FileStream sourceFileStream = new(sourceFile, FileMode.Open, FileAccess.Read);
            await using FileStream destinationFileStream = new(destinationFile, FileMode.Create, FileAccess.Write);
           
            await using GZipStream decompressionStream = new(sourceFileStream, CompressionMode.Decompress);
            decompressionStream.Seek(0, SeekOrigin.Begin);
            decompressionStream.CopyTo(destinationFileStream);
            decompressionStream.Close();
            await OnUploadCompleted.InvokeAsync(destinationFile);
          

        }
        catch (Exception ex)
        {
            // Handle any exceptions that occur during the process
            Console.WriteLine($"Exception: {ex.Message}");
            await OnException.InvokeAsync(ex);
        }

    }

    [JSInvokable]
    public async Task OnProgress(string progressType, long progressCurrent, long progressTotal)
    {
        string message = "Processing";
        switch (progressType)
        {
            case "read":

                message = $"Reading {Math.Ceiling((double)progressCurrent / progressTotal * 100)}%";
                break;
            case "compress":
                message = "Compressing...";
                break;

        }
        Console.WriteLine(message);
        await OnProgressChange.InvokeAsync(message);

    }

    [JSInvokable]
    public async Task OnJSErrorException(string errorMessage)
    {
        var exception = new Exception(errorMessage);
        await OnException.InvokeAsync(exception);
    }

    [JSInvokable]
    public async Task OnWarning(string warningMessage)
    {

        await OnFileWarning.InvokeAsync(warningMessage);
    }

    private void SetFileTypes()
    {
        if (AcceptedFileTypes is null)
        {
            acceptedFileTypes = "*";
            return;
        }
        if (!AcceptedFileTypes.Any())
        {
            acceptedFileTypes = "*";
            return;
        }

        acceptedFileTypes = string.Join(",", AcceptedFileTypes);
    }




}
