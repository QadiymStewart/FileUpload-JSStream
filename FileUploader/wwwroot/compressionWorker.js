self.onmessage = async function (e) {
    const { chunk, index } = e.data;
    const compressedChunk = await compressData(chunk);
    self.postMessage({ compressedChunk, index });
};

async function compressData(data) {
    const stream = new ReadableStream({
        start(controller) {
            controller.enqueue(new Uint8Array(data));
            controller.close();
        }
    });

    const compressionStream = new CompressionStream('gzip');
    const compressedStream = stream.pipeThrough(compressionStream);

    const reader = compressedStream.getReader();
    const chunks = [];
    let done, value;

    while ({ done, value } = await reader.read(), !done) {
        chunks.push(value);
    }

    return new Blob(chunks);
}

