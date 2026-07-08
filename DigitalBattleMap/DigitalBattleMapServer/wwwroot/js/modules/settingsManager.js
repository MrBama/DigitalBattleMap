const SettingsManager = (() => {
    const getSettings = () => {
        const cookie = {};
        decodeURIComponent(document.cookie).split(';').forEach(el => {
            const [key, value] = el.split('=');
            cookie[key.trim()] = value;
        });

        if (cookie.hasOwnProperty("_settings")) {
            return JSON.parse(cookie["_settings"]);
        }
        return null;
    };

    const getConditionVersion = () => {
        const settings = getSettings();
        return (settings && settings.ConditionVersion) ? settings.ConditionVersion : "5.5e";
    };

    return {
        getSettings,
        getConditionVersion
    };
})();
