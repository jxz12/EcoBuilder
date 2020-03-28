mergeInto(LibraryManager.library, {
    OpenURL: function (url) {
        var link = Pointer_stringify(url)
        document.onmouseup = function()
        {
            window.open(url);
        	document.onmouseup = null;
        } 
    }
});