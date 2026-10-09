// Tema claro/escuro. Carregado no <head> (síncrono e pequeno) para que o cookie do tema do
// sistema exista já na próxima requisição; as páginas SSR leem esses cookies no servidor.
(function () {
    var maxAge = 60 * 60 * 24 * 365;

    function setCookie(name, value) {
        document.cookie = name + '=' + value + ';path=/;max-age=' + maxAge + ';samesite=lax';
    }

    var prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    setCookie('duorico-theme-sys', prefersDark ? 'dark' : 'light');

    window.duoTheme = {
        setPreference: function (value) {
            setCookie('duorico-theme', value === 'dark' ? 'dark' : 'light');
        }
    };
})();
