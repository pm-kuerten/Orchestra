﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShellViewModel.cs" company="WildGums">
//   Copyright (c) 2008 - 2014 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orchestra.ViewModels
{
    using Catel;
    using Catel.MVVM;
    using Orchestra.Services;

    public class ShellViewModel : ViewModelBase
    {
        public ShellViewModel(IShellValidationDefferingService shellValidationDefferingService)
        {
            Argument.IsNotNull(() => shellValidationDefferingService);

            DeferValidationUntilFirstSaveCall = shellValidationDefferingService.DeferValidationUntilFirstSaveCall;
        }
    }
}
