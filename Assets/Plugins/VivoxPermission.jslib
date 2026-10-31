mergeInto(LibraryManager.library, {

    // =========================================================
    // Browser Microphone Permission
    // =========================================================
    RequestBrowserMicPermission: function (gameObjectNamePtr, callbackMethodPtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);

        if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
            navigator.mediaDevices.getUserMedia({ audio: true })
                .then(function (stream) {
                    stream.getTracks().forEach(function(track) {
                        track.stop();
                    });
                    SendMessage(gameObjectName, callbackMethod, "GRANTED");
                })
                .catch(function (err) {
                    SendMessage(gameObjectName, callbackMethod, "DENIED");
                });
        } else {
            SendMessage(gameObjectName, callbackMethod, "UNSUPPORTED");
        }
    },

    // =========================================================
    // Microphone Monitoring
    // =========================================================
    StartMicrophoneDeviceMonitoring: function (gameObjectNamePtr, callbackMethodPtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);

        if (!navigator.mediaDevices) {
            console.warn("[Mic] MediaDevices API not supported.");
            return;
        }

        if (window.vivoxMicDeviceListener) {
            navigator.mediaDevices.removeEventListener(
                "devicechange",
                window.vivoxMicDeviceListener
            );
        }

        window.vivoxMicDeviceListener = function () {
            console.log("[Mic] Device change detected.");

            navigator.mediaDevices.enumerateDevices()
                .then(function (devices) {
                    var hasMic = devices.some(function (device) {
                        return device.kind === "audioinput";
                    });

                    console.log("[Mic] Microphone available:", hasMic);

                    SendMessage(
                        gameObjectName,
                        callbackMethod,
                        hasMic ? "MIC_CONNECTED" : "MIC_DISCONNECTED"
                    );
                })
                .catch(function (err) {
                    console.error("[Mic] Failed to enumerate devices:", err);
                });
        };

        navigator.mediaDevices.addEventListener(
            "devicechange",
            window.vivoxMicDeviceListener
        );

        console.log("[Mic] Microphone device monitoring started.");
    },



    // =========================================================
    // Microphone Monitoring
    // =========================================================
    StopMicrophoneDeviceMonitoring: function () {
    if (window.vivoxMicDeviceListener && navigator.mediaDevices) {
        navigator.mediaDevices.removeEventListener(
            "devicechange",
            window.vivoxMicDeviceListener
        );
        window.vivoxMicDeviceListener = null;
        console.log("[Mic] Device monitoring stopped.");
    }
  },

    // =========================================================
    // WebGL Background Timer (Tab-Unthrottled via Web Worker)
    // =========================================================
    StartBackgroundTimer: function (gameObjectNamePtr, callbackMethodPtr, durationSeconds) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackMethod = UTF8ToString(callbackMethodPtr);

        // Terminate active worker if running
        if (window.unityBgWorker) {
            window.unityBgWorker.terminate();
            window.unityBgWorker = null;
        }

        // Inline Web Worker prevents browser tab throttling
        var workerCode = `
            self.onmessage = function(e) {
                setTimeout(function() {
                    self.postMessage("EXPIRED");
                }, e.data);
            };
        `;

        var blob = new Blob([workerCode], { type: "application/javascript" });
        window.unityBgWorker = new Worker(URL.createObjectURL(blob));

        window.unityBgWorker.onmessage = function (e) {
            if (e.data === "EXPIRED") {
                console.log("[WebGL] Background timer expired.");
                SendMessage(gameObjectName, callbackMethod, "EXPIRED");

                if (window.unityBgWorker) {
                    window.unityBgWorker.terminate();
                    window.unityBgWorker = null;
                }
            }
        };

        window.unityBgWorker.postMessage(durationSeconds * 1000);
        console.log("[WebGL] Background timer started: " + durationSeconds + "s");
    },

    CancelBackgroundTimer: function () {
        if (window.unityBgWorker) {
            window.unityBgWorker.terminate();
            window.unityBgWorker = null;
            console.log("[WebGL] Background timer cancelled.");
        }
    }
});