using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FileUpload.Components.Pages
{
    public partial class Home : IDisposable
    {
        [Inject]
        public required IJSRuntime JsRuntime { get; set; }

        private DotNetObjectReference<Home>? _blazorPageReference;

        /// <summary>
        /// After the component has rendered, set a reference to the Blazor page for JS interop.
        /// </summary>
        /// <param name="firstRender">Indicates if this is the first render of the component.</param>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _blazorPageReference = DotNetObjectReference.Create(this);
                await JsRuntime.InvokeAsync<object>("setBlazorPageReference", _blazorPageReference);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        /// <summary>
        /// Saves the uploaded file to the server.
        /// </summary>
        /// <param name="fileName">Name of the file to be saved.</param>
        /// <param name="fileType">Type of the file to be saved.</param>
        /// <returns>A string indicating the status of the file save operation.</returns>
        [JSInvokable]
        public async Task<string> SaveFile(string fileName, string fileType)
        {
            // Get the data stream reference from JavaScript
            var dataReference = await JsRuntime.InvokeAsync<IJSStreamReference>("fileDataStream");

            // Open the stream for reading with a maximum allowed size of 1GB
            await using var dataReferenceStream = await dataReference.OpenReadStreamAsync(maxAllowedSize: 1000000000);

            // Ensure the uploads directory exists
            string uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadDirectory);

            // Create the file path
            string filePath = Path.Combine(uploadDirectory, fileName);

            // Save the file to the server
            await using var createdFile = File.Create(filePath);
            await dataReferenceStream.CopyToAsync(createdFile);

            // Return a success message
            return $"New file {fileName} created in wwwroot/uploads";
        }

        /// <summary>
        /// Dispose the DotNetObjectReference when the component is disposed.
        /// </summary>
        public void Dispose()
        {
            _blazorPageReference?.Dispose();
        }
    }
}
