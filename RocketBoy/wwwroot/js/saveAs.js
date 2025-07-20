function saveAsFile(fileName, byteBase64, mimeType) {
    var link = document.createElement('a');
    link.download = fileName;
    link.href = `data:${mimeType};base64,${byteBase64}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}