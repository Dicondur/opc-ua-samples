﻿// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Template10.Mvvm;
using Windows.UI.Xaml.Navigation;

namespace RobotApp.ViewModels
{
    public class AxisPageViewModel : ViewModelBase
    {
        public AxisPageViewModel(IAxisViewModel[] axes)
        {
            this.Axes = axes;
        }

        public IEnumerable<IAxisViewModel> Axes { get; }

        public async override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            foreach (var axis in this.Axes)
            {
                await axis.OnNavigatedToAsync(parameter, mode, state);
            }
        }

        public async override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            foreach (var axis in this.Axes)
            {
                await axis.OnNavigatedFromAsync(pageState, suspending);
            }
        }
    }

    public class AxisPageViewModelDesignInstance : AxisPageViewModel
    {
        public AxisPageViewModelDesignInstance()
            : base(new[] { new Axis1ViewModel(null) })
        {
        }
    }
}