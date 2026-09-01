// Reconnection policy for the Blazor Server circuit, replacing the framework default.
//
// Why a custom handler instead of just tuning reconnectionOptions: on .NET 8
// 'retryIntervalMilliseconds' has to be a plain number — the framework compares it with
// '>' and hands it straight to setTimeout, so passing the backoff *function* that later
// versions accept would silently coerce to a 0ms delay and burn every retry in one tick.
// The default policy is also badly shaped for short drops: one attempt after 3s, then a
// flat 20s between the remaining seven. A two-second blip therefore costs ~23s of dead
// spinner, and the whole budget runs out at ~143s.
//
// What we do instead: a growing backoff that starts fast (a blip heals before the overlay
// even fades in — see the 1s animation-delay in css/custom.css), flattens at 20s, and stops
// inside the server's DisconnectedCircuitRetentionPeriod (3 minutes, Startup.cs) — retrying
// past that window is pointless because the circuit and the page state are gone by then.
// We also retry at once when the network or the tab comes back, and tell 'the server threw
// us away' apart from 'we could not reach the server', which need different screens.
(function () {
    'use strict';

    // Delay before each attempt. Cumulative ~130s; with the time the attempts themselves
    // take, the last one still lands inside the 3-minute retention window.
    var RETRY_DELAYS = [500, 1000, 2000, 3000, 5000, 8000, 12000, 20000, 20000, 20000, 20000, 20000];

    // Hard stop, matching DisconnectedCircuitRetentionPeriod. Bounds the loop even when
    // early retries (see EARLY_RETRY_THROTTLE_MS) keep it from spending the schedule.
    var MAX_TOTAL_MS = 3 * 60 * 1000;

    // An 'online' event or a tab switch cuts the current wait short, but no more often
    // than this — otherwise flapping Wi-Fi would hammer a server that is still down.
    var EARLY_RETRY_THROTTLE_MS = 3000;

    var MODAL_ID = 'components-reconnect-modal';
    var CLASSES = {
        show: 'components-reconnect-show',
        hide: 'components-reconnect-hide',
        failed: 'components-reconnect-failed',
        rejected: 'components-reconnect-rejected'
    };

    var running = false;
    var cancelled = false;
    var wake = null;          // resolves the current wait early; null while not waiting
    var lastEarlyRetry = 0;
    var countdownTimer = null;

    function log(message) {
        console.info('[chronos:reconnect] ' + message);
    }

    function warn(message) {
        console.warn('[chronos:reconnect] ' + message);
    }

    function describe(error) {
        if (!error) {
            return 'unknown error';
        }
        return error.message || String(error);
    }

    function byId(id) {
        return document.getElementById(id);
    }

    // Only the class changes here; which block is visible is decided by css/custom.css.
    function showState(name) {
        var modal = byId(MODAL_ID);
        if (!modal) {
            return;
        }
        modal.classList.remove(CLASSES.show, CLASSES.hide, CLASSES.failed, CLASSES.rejected);
        modal.classList.add(CLASSES[name]);
        if (name !== 'show') {
            setCountdown(0);
        }
    }

    function setAttempt(attempt) {
        var current = byId('components-reconnect-current-attempt');
        if (current) {
            current.innerText = attempt.toString();
        }
        var max = byId('components-reconnect-max-retries');
        if (max) {
            max.innerText = RETRY_DELAYS.length.toString();
        }
    }

    // Without this the overlay looks stuck during the long waits at the end of the schedule.
    function setCountdown(secondsLeft) {
        var element = byId('chr-rc-next-attempt');
        if (!element) {
            return;
        }
        element.innerText = secondsLeft > 1 ? 'Следующая попытка через ' + secondsLeft + ' с' : '';
    }

    // Resolves true when the wait was cut short by wakeNow(), false when it ran its course.
    function waitBeforeAttempt(duration) {
        return new Promise(function (resolve) {
            var deadline = Date.now() + duration;
            var settled = false;

            var tick = function () {
                setCountdown(Math.round((deadline - Date.now()) / 1000));
            };
            tick();
            countdownTimer = setInterval(tick, 1000);

            var finish = function (early) {
                if (settled) {
                    return;
                }
                settled = true;
                clearTimeout(timer);
                clearInterval(countdownTimer);
                countdownTimer = null;
                wake = null;
                setCountdown(0);
                resolve(early);
            };

            var timer = setTimeout(function () {
                finish(false);
            }, duration);

            wake = function () {
                finish(true);
            };
        });
    }

    function wakeNow(reason) {
        if (!running || !wake) {
            return;
        }
        var now = Date.now();
        if (now - lastEarlyRetry < EARLY_RETRY_THROTTLE_MS) {
            return;
        }
        lastEarlyRetry = now;
        log(reason + ' — retrying now instead of waiting');
        wake();
    }

    async function reconnectLoop() {
        running = true;
        cancelled = false;
        showState('show');

        var startedAt = Date.now();
        var index = 0;

        while (index < RETRY_DELAYS.length && Date.now() - startedAt < MAX_TOTAL_MS) {
            setAttempt(index + 1);

            var wokeEarly = await waitBeforeAttempt(RETRY_DELAYS[index]);
            if (cancelled) {
                running = false;
                return;
            }

            try {
                if (await window.Blazor.reconnect()) {
                    // onConnectionUp() has already hidden the overlay.
                    log('reconnected on attempt ' + (index + 1));
                    running = false;
                    return;
                }
                // A definitive answer from the server: the circuit is gone, so is the state.
                warn('the server rejected the circuit — page state is lost, only a reload helps');
                showState('rejected');
                running = false;
                return;
            } catch (error) {
                // Server unreachable (offline, IIS worker restarting, proxy in the way).
                // Transient by nature, so keep going — this is exactly the case where the
                // framework default would have made us wait 20s doing nothing.
                warn('attempt ' + (index + 1) + ' of ' + RETRY_DELAYS.length +
                    ' failed: ' + describe(error));
            }

            // A retry we asked for early does not spend the schedule; only MAX_TOTAL_MS
            // bounds the loop in that case.
            if (!wokeEarly) {
                index++;
            }
        }

        warn('gave up after ' + Math.round((Date.now() - startedAt) / 1000) + 's');
        showState('failed');
        running = false;
    }

    var handler = {
        onConnectionDown: function (options, error) {
            warn(error ? 'connection lost: ' + describe(error) : 'connection closed without an error');
            if (running) {
                return;
            }
            reconnectLoop();
        },
        onConnectionUp: function () {
            cancelled = true;
            if (wake) {
                wake();
            }
            showState('hide');
        }
    };

    // Signals that the drop is probably over: the network came back, or the user returned to
    // a tab/window that was asleep. Waiting out the remaining backoff would be pointless.
    window.addEventListener('online', function () {
        wakeNow('network is back');
    });
    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) {
            wakeNow('tab is visible again');
        }
    });
    window.addEventListener('focus', function () {
        wakeNow('window is focused again');
    });

    // The 'Повторить' button on the exhausted-retries screen: start a fresh schedule.
    // Worth offering even though the retention window has likely passed — if it has, the
    // server answers with a rejection and we say so instead of leaving the user guessing.
    document.addEventListener('click', function (event) {
        var target = event.target;
        if (!target || !target.classList || !target.classList.contains('chr-rc__btn--retry')) {
            return;
        }
        if (!running) {
            lastEarlyRetry = 0;
            reconnectLoop();
        }
    });

    // Blazor.start() is called from _Host.cshtml, not here: if this file ever fails to load
    // or throws, the page falls back to the framework policy instead of never booting at all
    // (with autostart="false", no Blazor.start() means no circuit).
    window.chronosReconnect = { handler: handler };
})();
