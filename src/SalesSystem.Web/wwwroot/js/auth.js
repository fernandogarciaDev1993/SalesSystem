window.authStorage = {
    set: function (key, value) {
        sessionStorage.setItem(key, value);
    },
    get: function (key) {
        return sessionStorage.getItem(key);
    },
    remove: function (key) {
        sessionStorage.removeItem(key);
    }
};
