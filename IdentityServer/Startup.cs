using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Security.Claims;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Models;
using IdentityServer4.Test;

namespace IdentityServer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddIdentityServer(options =>
            {
                // http://docs.identityserver.io/en/release/reference/options.html#refoptions
                options.Endpoints = new EndpointsOptions
                {
                    // в Implicit Flow используется для получения токенов
                    EnableAuthorizeEndpoint = true,
                    // для получения статуса сессии
                    EnableCheckSessionEndpoint = true,
                    // для логаута по инициативе пользователя
                    EnableEndSessionEndpoint = true,
                    // для получения claims аутентифицированного пользователя 
                    // http://openid.net/specs/openid-connect-core-1_0.html#UserInfo
                    EnableUserInfoEndpoint = true,
                    // используется OpenId Connect для получения метаданных
                    EnableDiscoveryEndpoint = true,

                    // для получения информации о токенах, мы не используем
                    EnableIntrospectionEndpoint = false,
                    // нам не нужен т.к. в Implicit Flow access_token получают через authorization_endpoint
                    EnableTokenEndpoint = false,
                    // мы не используем refresh и reference tokens 
                    // http://docs.identityserver.io/en/release/topics/reference_tokens.html
                    EnableTokenRevocationEndpoint = false
                };

                // IdentitySever использует cookie для хранения своей сессии
                options.Authentication = new IdentityServer4.Configuration.AuthenticationOptions
                {
                    CookieLifetime = TimeSpan.FromDays(1)
                };

            })
                // тестовый x509-сертификат, IdentityServer использует RS256 для подписи JWT
                .AddDeveloperSigningCredential()
                // что включать в id_token
                .AddInMemoryIdentityResources(GetIdentityResources())
                // что включать в access_token
                .AddInMemoryApiResources(GetApiResources())
                // настройки клиентских приложений
                .AddInMemoryClients(GetClients())
                // тестовые пользователи
                .AddTestUsers(GetUsers());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);
            app.UseDeveloperExceptionPage();

            // подключаем middleware IdentityServer
            app.UseIdentityServer();

            // эти 2 строчки нужны, чтобы нормально обрабатывались страницы логина
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            // определяет, какие scopes будут доступны IdentityServer
            return new List<IdentityResource>
            {
                // "sub" claim
                new IdentityResources.OpenId(),
                // стандартные claims в соответствии с profile scope
                // http://openid.net/specs/openid-connect-core-1_0.html#ScopeClaims
                new IdentityResources.Profile(),
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            // claims этих scopes будут включены в access_token
            return new List<ApiResource>
            {
                // определяем scope "api1" для IdentityServer
                new ApiResource("api1", "API 1", 
                    // эти claims войдут в scope api1
                    new[] {"name", "role" })
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
            {
                new Client
                {
                    // обязательный параметр, при помощи client_id сервер различает клиентские приложения 
                    ClientId = "js",
                    ClientName = "JavaScript Client",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,
                    // от этой настройки зависит размер токена, 
                    // при false можно получить недостающую информацию через UserInfo endpoint
                    AlwaysIncludeUserClaimsInIdToken = true,
                    // белый список адресов на который клиентское приложение может попросить
                    // перенаправить User Agent, важно для безопасности
                    RedirectUris = {
                        // адрес перенаправления после логина
                        "http://localhost:5003/callback.html",
                        // адрес перенаправления при автоматическом обновлении access_token через iframe
                        "http://localhost:5003/callback-silent.html"
                    },
                    PostLogoutRedirectUris = { "http://localhost:5003/index.html" },
                    // адрес клиентского приложения, просим сервер возвращать нужные CORS-заголовки
                    AllowedCorsOrigins = { "http://localhost:5003" },
                    // список scopes, разрешённых именно для данного клиентского приложения
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "api1"
                    },

                    AccessTokenLifetime = 300, // секунд, это значение по умолчанию
                    IdentityTokenLifetime = 3600, // секунд, это значение по умолчанию

                    // разрешено ли получение refresh-токенов через указание scope offline_access
                    AllowOfflineAccess = false,
                }
            };
        }

        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "1",
                    Username = "alice",
                    Password = "password",

                    Claims = new List<Claim>
                    {
                        new Claim("name", "Alice"),
                        new Claim("website", "https://alice.com"),
                        new Claim("role", "user"),
                    }
                },
                new TestUser
                {
                    SubjectId = "2",
                    Username = "bob",
                    Password = "password",

                    Claims = new List<Claim>
                    {
                        new Claim("name", "Bob"),
                        new Claim("website", "https://bob.com"),
                        new Claim("role", "admin"),
                    }
                }
            };
        }
    }
}
