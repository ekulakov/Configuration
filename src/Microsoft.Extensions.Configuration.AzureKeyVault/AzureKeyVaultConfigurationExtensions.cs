﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for registering <see cref="AzureKeyVaultConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class AzureKeyVaultConfigurationExtensions
    {
        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">The Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="clientSecret">The client secret to use for authentication.</param>
        /// <param name="filter">The predicate to filter secret entries before loading value, <code>null</code> to load all.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            string clientSecret,
            Func<SecretItem, bool> filter)
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }
            if (clientSecret == null)
            {
                throw new ArgumentNullException(nameof(clientSecret));
            }
            KeyVaultClient.AuthenticationCallback callback =
                (authority, resource, scope) => GetTokenFromClientSecret(authority, resource, scope, clientId, clientSecret);

            return configurationBuilder.AddAzureKeyVault(vault, callback, filter);
        }


        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="clientId">The application client id.</param>
        /// <param name="certificate">The <see cref="X509Certificate2"/> to use for authentication.</param>
        /// <param name="filter">The predicate to filter secret entries before loading value, <code>null</code> to load all.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            string clientId,
            X509Certificate2 certificate,
            Func<SecretItem, bool> filter)
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }
            KeyVaultClient.AuthenticationCallback callback =
                (authority, resource, scope) => GetTokenFromClientCertificate(authority, resource, scope, clientId, certificate);

            return configurationBuilder.AddAzureKeyVault(vault, callback, filter);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="authenticationCallback">The <see cref="KeyVaultClient.AuthenticationCallback"/> to use for authentication.</param>
        /// <param name="filter">The predicate to filter secret entries before loading value, <code>null</code> to load all.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            KeyVaultClient.AuthenticationCallback authenticationCallback,
            Func<SecretItem, bool> filter)
        {
            if (authenticationCallback == null)
            {
                throw new ArgumentNullException(nameof(authenticationCallback));
            }
            return configurationBuilder.AddAzureKeyVault(vault, new KeyVaultClient(authenticationCallback), filter);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the Azure KeyVault.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="client">The <see cref="KeyVaultClient"/> to use for retrieving values.</param>
        /// <param name="filter">The predicate to filter secret entries before loading value, <code>null</code> to load all.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddAzureKeyVault(
            this IConfigurationBuilder configurationBuilder,
            string vault,
            KeyVaultClient client,
            Func<SecretItem, bool> filter)
        {
            if (configurationBuilder == null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            if (vault == null)
            {
                throw new ArgumentNullException(nameof(vault));
            }

            configurationBuilder.Add(new AzureKeyVaultConfigurationSource()
            {
                Client = client,
                Vault = vault,
                Filter = filter
            });

            return configurationBuilder;
        }

        private static async Task<string> GetTokenFromClientSecret(string authority, string resource, string scope, string clientId, string clientSecret)
        {
            var authContext = new AuthenticationContext(authority);
            var clientCred = new ClientCredential(clientId, clientSecret);
            var result = await authContext.AcquireTokenAsync(resource, clientCred);
            return result.AccessToken;
        }

        private static async Task<string> GetTokenFromClientCertificate(string authority, string resource, string scope, string clientId, X509Certificate2 certificate)
        {
            var authContext = new AuthenticationContext(authority);
            var result = await authContext.AcquireTokenAsync(resource, new ClientAssertionCertificate(clientId, certificate));
            return result.AccessToken;
        }
    }
}