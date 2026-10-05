// Selecting days on the year of absences (issue #310). The selection is drawn here, in the
// browser, so a drag does not wait for the server on every day it crosses; the server
// hears once, when the drag ends.
window.chronosAbsenceYear = {
    attach: function (grid, dotnet) {
        if (!grid || grid._chronosAbsenceYear) {
            return;
        }

        var start = null;
        var end = null;

        var dayAt = function (x, y) {
            var element = document.elementFromPoint(x, y);
            return element && element.closest ? element.closest('[data-date]') : null;
        };

        var paint = function () {
            var from = start < end ? start : end;
            var to = start < end ? end : start;
            grid.querySelectorAll('[data-date]').forEach(function (cell) {
                var date = cell.getAttribute('data-date');
                cell.classList.toggle('abs-day--selected', start !== null && date >= from && date <= to);
            });
        };

        var down = function (e) {
            if (e.button !== 0) {
                return;
            }
            var cell = dayAt(e.clientX, e.clientY);
            if (!cell || !grid.contains(cell)) {
                return;
            }
            e.preventDefault();
            start = end = cell.getAttribute('data-date');
            paint();
        };

        var move = function (e) {
            if (start === null) {
                return;
            }
            var cell = dayAt(e.clientX, e.clientY);
            if (cell && grid.contains(cell) && cell.getAttribute('data-date') !== end) {
                end = cell.getAttribute('data-date');
                paint();
            }
        };

        var up = function (e) {
            if (start === null) {
                return;
            }
            var from = start < end ? start : end;
            var to = start < end ? end : start;
            start = end = null;
            dotnet.invokeMethodAsync('OnDaysSelected', from, to, e.clientX, e.clientY);
        };

        // On a touch screen the drag belongs to scrolling: the browser cancels the pointer,
        // and a tap picks a single day whose range the composer's date fields then set.
        var cancel = function () {
            start = end = null;
            paint();
        };

        grid.addEventListener('pointerdown', down);
        document.addEventListener('pointermove', move);
        document.addEventListener('pointerup', up);
        document.addEventListener('pointercancel', cancel);

        grid._chronosAbsenceYear = function () {
            grid.removeEventListener('pointerdown', down);
            document.removeEventListener('pointermove', move);
            document.removeEventListener('pointerup', up);
            document.removeEventListener('pointercancel', cancel);
        };
    },

    detach: function (grid) {
        if (grid && grid._chronosAbsenceYear) {
            grid._chronosAbsenceYear();
            delete grid._chronosAbsenceYear;
        }
    },

    clear: function (grid) {
        if (grid) {
            grid.querySelectorAll('.abs-day--selected').forEach(function (cell) {
                cell.classList.remove('abs-day--selected');
            });
        }
    },

    // Keeps the composer next to the pointer but inside the window.
    place: function (element, x, y) {
        if (!element) {
            return;
        }
        var margin = 12;
        var left = Math.min(x + margin, window.innerWidth - element.offsetWidth - margin);
        var top = Math.min(y + margin, window.innerHeight - element.offsetHeight - margin);
        element.style.left = Math.max(margin, left) + 'px';
        element.style.top = Math.max(margin, top) + 'px';
        element.style.visibility = 'visible';
    }
};
