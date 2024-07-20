// Function to set the Blazor page reference in the global window object
function setBlazorPageReference(blazorPageHook) {
    window.blazorPageHook = blazorPageHook;
}

// Function to save a file to Blazor
function saveFileToBlazor() {
    const fileField = document.querySelector('#file-input');

    // Check if a file has been selected
    if (fileField.files.length <= 0) {
        alert("You need to add a file, goofball");
        return;
    }

    // Get a handle on the selected file
    const file = fileField.files[0];
    // Create a FileReader instance to read the file
    const fileReader = new FileReader();

    // Add a listener to the FileReader instance
    fileReader.addEventListener("load", function () {
        // When the file is loaded, create a Uint8Array of the file's content
        const fileData = new Uint8Array(fileReader.result);

        // Add the data stream function to the global window object
        window.fileDataStream = function () {
            return fileData;
        };

        // Invoke the 'SaveFile' method in Blazor, passing the file name and type
        window.blazorPageHook.invokeMethodAsync('SaveFile', file.name, file.type)
            .then(result => {
                alert(result);
                fileField.value = null; // Clear the file input field
            });
    }, false);

    // Read the file's data as an ArrayBuffer
    if (file) {
        fileReader.readAsArrayBuffer(file);
    } else {
        alert("No file selected");
    }
}
