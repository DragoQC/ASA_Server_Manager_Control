window.iniGeneratorStorage = {
    load(key) {
        const raw = window.localStorage.getItem(key);
        return raw ?? "";
    },

    save(key, value) {
        window.localStorage.setItem(key, value);
    }
};
