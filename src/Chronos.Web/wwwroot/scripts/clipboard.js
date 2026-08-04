var clipboard = function () {
    return {
        convertToBlob: function (sourceText, sourceType) {
            return new Blob([sourceText], { type: sourceType });
        },
        convertToPromise: function (sourceText, sourceType) {
            var blob = new Blob([sourceText], { type: sourceType });
            return new Promise((resolve) => resolve(blob));
        }
    }
}