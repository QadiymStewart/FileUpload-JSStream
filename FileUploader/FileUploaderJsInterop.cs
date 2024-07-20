using Microsoft.JSInterop;

namespace FileUploader
{
    // This class provides an example of how JavaScript functionality can be wrapped
    // in a .NET class for easy consumption. The associated JavaScript module is
    // loaded on demand when first needed.
    //
    // This class can be registered as scoped DI service and then injected into Blazor
    // components for use.

    public class FileUploaderJsInterop : IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> moduleTask;
        private readonly Lazy<Task<IJSObjectReference>> workermoduleTask;


        public FileUploaderJsInterop(IJSRuntime jsRuntime)
        {
            moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/FileUploader/fileUploaderJsInterop.js").AsTask());
            workermoduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
    "import", "./_content/FileUploader/compressionWorker.js").AsTask());



        }

        public async ValueTask Init(string fileInputId, DotNetObjectReference<FileUploadButton> dotNetObjectReference)
        {

            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("saveFileToBlazor", fileInputId, "./_content/FileUploader/compressionWorker.js", dotNetObjectReference);
        }

        public async ValueTask DisposeAsync()
        {
            if (moduleTask.IsValueCreated)
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}
