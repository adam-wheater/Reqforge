function saveAsFile(fileName, bytesBase64, mimeType) {
    var link = document.createElement('a');
    link.download = fileName;
    link.href = "data:" + mimeType + ";base64," + bytesBase64;
    document.body.appendChild(link); // Needed for Firefox
    link.click();
    document.body.removeChild(link);
}