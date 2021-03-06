﻿// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Workstation.ServiceModel.Ua;

namespace StatusHmi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ILoggerFactory loggerFactory;
        private UaTcpSessionClient session;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Setup a logger.
            this.loggerFactory = new LoggerFactory();
            this.loggerFactory.AddDebug(LogLevel.Trace);

            // discover available endpoints of server.
            var getEndpointsRequest = new GetEndpointsRequest
            {
                EndpointUrl = StatusHmi.Properties.Settings.Default.EndpointUrl,
                ProfileUris = new[] { TransportProfileUris.UaTcpTransport }
            };
            var getEndpointsResponse = UaTcpDiscoveryClient.GetEndpointsAsync(getEndpointsRequest).Result;

            var endpoint = getEndpointsResponse.Endpoints
                .Where(d => d.SecurityPolicyUri == SecurityPolicyUris.Basic256
                && d.SecurityMode == MessageSecurityMode.SignAndEncrypt)
                .First();

            // Create the session client for the app.
            this.session = new UaTcpSessionClient(
                new ApplicationDescription()
                {
                    ApplicationName = "Workstation.StatusHmi",
                    ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:Workstation.StatusHmi",
                    ApplicationType = ApplicationType.Client
                },
                new DirectoryStore(
                    Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Workstation.StatusHmi\pki"),
                    loggerFactory: this.loggerFactory),
                this.ProvideUserIdentity,
                endpoint,
                this.loggerFactory);

            // Create the main view model.
            var viewModel = new MainViewModel(this.session);

            // Create and show the main view.
            var view = new MainView { DataContext = viewModel };

            view.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            this.session?.Dispose();
            this.loggerFactory?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Shows a Sign In dialog if the remote endpoint demands a UserNameIdentity token.
        /// Requires MainWindow to derive from MahApps.Metro.Controls.MetroWindow.
        /// </summary>
        /// <param name="endpoint">The remote endpoint.</param>
        /// <returns>A UserIdentity</returns>
        private Task<IUserIdentity> ProvideUserIdentity(EndpointDescription endpoint)
        {
            if (endpoint.UserIdentityTokens.Any(p => p.TokenType == UserTokenType.Anonymous))
            {
                return Task.FromResult<IUserIdentity>(new AnonymousIdentity());
            }

            if (endpoint.UserIdentityTokens.Any(p => p.TokenType == UserTokenType.UserName))
            {
                var tcs = new TaskCompletionSource<IUserIdentity>();

                this.Dispatcher.InvokeAsync(
                    async () =>
                    {
                        var shell = (MetroWindow)this.MainWindow;
                        var userNamesDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(StatusHmi.Properties.Settings.Default.UserNames) ?? new Dictionary<string, string>();
                        var userNameKey = $"userName_{endpoint.EndpointUrl}";

                        var initialUserName = string.Empty;
                        if (userNamesDictionary.ContainsKey(userNameKey))
                        {
                            initialUserName = userNamesDictionary[userNameKey];
                        }

                        LoginDialogSettings loginSettings = new LoginDialogSettings { InitialUsername = initialUserName };
                        var result = await shell.ShowLoginAsync("SIGN IN", $"Connecting to server '{endpoint.Server.ApplicationName}' at '{endpoint.EndpointUrl}'.", loginSettings);
                        if (result != null && !string.IsNullOrEmpty(result.Username))
                        {
                            userNamesDictionary[userNameKey] = result.Username;
                            StatusHmi.Properties.Settings.Default.UserNames = JsonConvert.SerializeObject(userNamesDictionary);
                            StatusHmi.Properties.Settings.Default.Save();

                            tcs.TrySetResult(new UserNameIdentity(result.Username, result.Password));
                        }
                        tcs.TrySetResult(new AnonymousIdentity());
                    },
                    System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                return tcs.Task;
            }

            throw new NotImplementedException("ProvideUserIdentity supports only UserName and Anonymous identity, for now.");
        }
    }
}
