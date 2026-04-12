window.themeManager = {
    apply: function(config) {
        if (!config) return;
        const r = document.documentElement.style;
        if (config.primaryColor) r.setProperty('--primary', config.primaryColor);
        if (config.primaryDark) r.setProperty('--primary-dark', config.primaryDark);
        if (config.primaryLight) r.setProperty('--primary-light', config.primaryLight);
        if (config.accentColor) r.setProperty('--accent', config.accentColor);
        if (config.sidebarBg) r.setProperty('--sidebar-bg', config.sidebarBg);
        if (config.sidebarText) r.setProperty('--sidebar-text', config.sidebarText);
        if (config.bodyBg) r.setProperty('--body-bg', config.bodyBg);
        if (config.topbarBg) r.setProperty('--topbar-bg', config.topbarBg);
    },
    clear: function() {
        const props = ['--primary','--primary-dark','--primary-light','--accent','--sidebar-bg','--sidebar-text','--body-bg','--topbar-bg'];
        props.forEach(p => document.documentElement.style.removeProperty(p));
    }
};
