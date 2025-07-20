function setupKeyHandler(dotNetRef) {
    document.addEventListener('keydown', function (event) {
        if (event.ctrlKey && event.key === 's') {
            event.preventDefault();  // Prevent the browser's save dialog
            dotNetRef.invokeMethodAsync('HandleCtrlS');
        }
    });
}