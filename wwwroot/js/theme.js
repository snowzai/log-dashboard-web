window.themeManager = {
    init: function () {
        const saved = localStorage.getItem('logdashboard-theme') || 'dark';
        document.documentElement.setAttribute('data-theme', saved);
        return saved;
    },
    set: function (theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('logdashboard-theme', theme);
    }
};