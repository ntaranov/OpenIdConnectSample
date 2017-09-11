/// <reference path="oidc-client.js" />

Oidc.Log.logger = console;
Oidc.Log.level = 4;

var config = {

    authority: "http://localhost:5000", // Адрес нашего IdentityServer
    client_id: "js", // должен совпадать с указанным на IdentityServer
    // Адрес страницы, на которую будет перенаправлен браузер после прохождения пользователем аутентификации
    // и получения от пользователя подтверждений - в соответствии с требованиями OpenId Connect
    redirect_uri: "http://localhost:5003/callback.html",
    // Response Type определяет набор токенов, получаемых от Authorization Endpoint
    // Данное сочетание означает, что мы используем Implicit Flow
    // http://openid.net/specs/openid-connect-core-1_0.html#Authentication
    response_type: "id_token token",
    // Получить subject id пользователя, а также поля профиля в id_token, а также получить access_token для доступа к api1 (см. наcтройки IdentityServer)
    scope: "openid profile api1",
    // Страница, на которую нужно перенаправить пользователя в случае инициированного им логаута
    post_logout_redirect_uri: "http://localhost:5003/index.html",
    // следить за состоянием сессии на IdentityServer, по умолчанию true
    monitorSession: true,
    // интервал в миллисекундах, раз в который нужно проверять сессию пользователя, по умолчанию 2000
    checkSessionInterval: 30000,
    // отзывает access_token в соответствии со стандартом https://tools.ietf.org/html/rfc7009
    revokeAccessTokenOnSignout: true,
    // допустимая погрешность часов на клиенте и серверах, нужна для валидации токенов, по умолчанию 300
    // https://github.com/IdentityModel/oidc-client-js/blob/1.3.0/src/JoseUtil.js#L95
    clockSkew: 300,
    // делать ли запрос к UserInfo endpoint для того, чтоб добавить данные в профиль пользователя
    loadUserInfo: true,
};
var mgr = new Oidc.UserManager(config);


function login() {
    // Инициировать логин
    mgr.signinRedirect();
}

function displayUser() {
    mgr.getUser().then(function (user) {
        if (user) {
            log("User logged in", user.profile);
        }
        else {
            log("User not logged in");
        }
    });
}

function api() {
    // возвращает все claims пользователя
    requestUrl(mgr, "http://localhost:5001/identity");
}

function getSuperpowers() {
    // этот endpoint доступен только админам
    requestUrl(mgr, "http://localhost:5001/superpowers");
}

function logout() {
    // Инициировать логаут
    mgr.signoutRedirect();
}

document.getElementById("login").addEventListener("click", login, false);
document.getElementById("api").addEventListener("click", api, false);
document.getElementById("getSuperpowers").addEventListener("click", getSuperpowers, false);
document.getElementById("logout").addEventListener("click", logout, false);
document.getElementById("getUser").addEventListener("click", displayUser, false);

// отобразить данные о пользователе после загрузки
displayUser();

function requestUrl(mgr, url) {
    mgr.getUser().then(function (user) {
        var xhr = new XMLHttpRequest();
        xhr.open("GET", url);
        xhr.onload = function () {
            log(xhr.status, 200 == xhr.status ? JSON.parse(xhr.responseText) : "An error has occured.");
        }
        // добавляем заголовок Authorization с access_token в качестве Bearer - токена. 
        xhr.setRequestHeader("Authorization", "Bearer " + user.access_token);
        xhr.send();
    });
}

function log() {
    document.getElementById('results').innerText = '';

    Array.prototype.forEach.call(arguments, function (msg) {
        if (msg instanceof Error) {
            msg = "Error: " + msg.message;
        }
        else if (typeof msg !== 'string') {
            msg = JSON.stringify(msg, null, 2);
        }
        document.getElementById('results').innerHTML += msg + '\r\n';
    });
}