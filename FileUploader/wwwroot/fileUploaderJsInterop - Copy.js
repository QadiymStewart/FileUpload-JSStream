/**
 * Save a file to Blazor after compressing it in chunks.
 * 
 * @param {string} fileInputId - The ID of the file input element.
 * @param {string} workerFileLocation - The location of the web worker file.
 * @param {Object} dotNetObjectRef - The .NET object reference for invoking Blazor methods.
 */
export async function saveFileToBlazor(fileInputId, workerFileLocation, dotNetObjectRef) {
    const fileField = document.getElementById(fileInputId);

    // Ensure a file is selected
    if (fileField.files.length <= 0) {
        console.log("No file selected");
        dotNetObjectRef.invokeMethodAsync('OnWarning', "A valid file is required.");
        return;
    }

    const file = fileField.files[0];
    console.log("File selected:", file.name);

    // Callback for reporting progress to Blazor
    const progressCallback = (progress) => {
        console.log("Progress:", progress);
        dotNetObjectRef.invokeMethodAsync('OnProgress', progress.type, progress.loaded, progress.total);
    };

    try {
        // Compress the file in chunks
        const compressedBlob = await compressFileInChunks(file, workerFileLocation, progressCallback);
        console.log("File compressed successfully");

        // Provide a function for Blazor to access the compressed data stream
        window.fileDataStream = function () {
            return compressedBlob;
        };

        // Invoke Blazor method to handle the file saving process
        await dotNetObjectRef.invokeMethodAsync('SaveFile', file.name, file.type, file.size);
        console.log("File upload initiated");

        // Clear the file input field and reset fileDataStream
        fileField.value = null;
        window.fileDataStream = null;
    } catch (error) {
        console.error("Error during file processing:", error);
        // Report any errors to Blazor
        dotNetObjectRef.invokeMethodAsync('OnJSErrorException', error.message);
    }
}

/**
 * Compress a file in chunks using a web worker.
 * 
 * @param {File} file - The file to be compressed.
 * @param {string} workerFileLocation - The location of the web worker file.
 * @param {function} progressCallback - Callback for reporting progress.
 * @param {number} [chunkSize=128*1024*1024] - The size of each chunk (default is 128MB).
 * @returns {Promise<Blob>} - A promise that resolves to a compressed Blob.
 */
async function compressFileInChunks(file, workerFileLocation, progressCallback, chunkSize = 128 * 1024 * 1024) {
    const totalChunks = Math.ceil(file.size / chunkSize);
    let offset = 0;
    const workerPromises = [];
    const workerResults = new Array(totalChunks).fill(null);
    const workers = [];

    console.log("Total chunks:", totalChunks);

    // Read and compress the file in chunks
    for (let i = 0; i < totalChunks; i++) {
        const chunk = file.slice(offset, offset + chunkSize);
        const arrayBuffer = await readChunkAsArrayBuffer(chunk);
        offset += chunkSize;

        // Report read progress
        if (progressCallback) {
            progressCallback({
                type: 'read',
                loaded: offset,
                total: file.size,
            });
        }

        // Compress chunk using a web worker
        const worker = new Worker(workerFileLocation);
        workers.push(worker);

        const promise = new Promise((resolve, reject) => {
            worker.onmessage = (e) => {
                const { compressedChunk, index } = e.data;
                workerResults[index] = compressedChunk;

                // Report compression progress
                if (progressCallback) {
                    progressCallback({
                        type: 'compress',
                        loaded: index + 1,
                        total: totalChunks,
                    });
                }

                // Terminate the worker after processing
                worker.terminate();
                resolve();
            };

            worker.onerror = (e) => {
                console.error("Worker error:", e);
                worker.terminate();
                reject(e);
            };
        });

        worker.postMessage({ chunk: arrayBuffer, index: i });
        workerPromises.push(promise);
    }

    await Promise.all(workerPromises);

    // Combine all compressed chunks into a single Blob
    const finalBlob = new Blob(workerResults);
    console.log("Final compressed Blob size:", finalBlob.size);

    // Terminate all workers in case any remain active
    workers.forEach(worker => worker.terminate());

    return finalBlob;
}

/**
 * Read a file chunk as an ArrayBuffer.
 * 
 * @param {Blob} chunk - The file chunk to read.
 * @returns {Promise<ArrayBuffer>} - A promise that resolves to an ArrayBuffer.
 */
function readChunkAsArrayBuffer(chunk) {
    return new Promise((resolve, reject) => {
        const fileReader = new FileReader();
        fileReader.onload = () => resolve(fileReader.result);
        fileReader.onerror = () => reject(fileReader.error);
        fileReader.readAsArrayBuffer(chunk);
    });
}
