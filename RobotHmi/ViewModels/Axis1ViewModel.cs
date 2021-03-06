﻿// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Prism.Regions;
using RobotHmi.Services;
using Workstation.ServiceModel.Ua;

namespace RobotHmi.ViewModels
{
    /// <summary>
    /// A view model for Axis1.
    /// </summary>
    [Subscription(publishingInterval: 500, keepAliveCount: 20)]
    public class Axis1ViewModel : ViewModelBase, IAxisViewModel, INavigationAware
    {
        private PLC1Session session;
        private IDisposable subscriptionToken;

        public Axis1ViewModel(PLC1Session session)
        {
            this.session = session;
        }

        /// <summary>
        /// Gets the value of Axis.
        /// </summary>
        [MonitoredItem(nodeId: "ns=2;s=Robot1_Axis1")]
        public float Axis
        {
            get { return this.axis; }
            private set { this.SetProperty(ref this.axis, value); }
        }

        private float axis;

        /// <summary>
        /// Gets the DisplayName.
        /// </summary>
        public string DisplayName => "Axis 1";

        /// <inheritdoc/>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <inheritdoc/>
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            this.subscriptionToken = this.session?.Subscribe(this);
        }

        /// <inheritdoc/>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            this.subscriptionToken.Dispose();
        }
    }
}