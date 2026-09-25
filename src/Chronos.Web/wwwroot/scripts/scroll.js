// Brings an element into view: the day strip on the worklog page jumps to a day's row.
window.chronosScroll = {
    toElement: function (id) {
        var element = document.getElementById(id);
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }
};
