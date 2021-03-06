﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Mikepenz.Materialdrawer;
using GalaSoft.MvvmLight.Helpers;
using MALClient.Android.BindingConverters;
using MALClient.Models.Models.MalSpecific;
using MALClient.XShared.ViewModels;
using MALClient.XShared.ViewModels.Clubs;

namespace MALClient.Android.Fragments.Clubs
{
    public class ClubIndexMyClubsTabFragment : ClubIndexTabFragmentBase
    {
        protected override void Init(Bundle savedInstanceState)
        {

        }

        protected override void InitBindings()
        {
            Bindings.Add(this.SetBinding(() => ViewModel.MyClubs).WhenSourceChanges(() =>
            {
                if (ViewModel.MyClubs == null)
                    List.Adapter = null;
                else
                    List.InjectFlingAdapter(ViewModel.MyClubs, ViewHolderFactory, DataTemplateFull, DataTemplateFling, DataTemplateBasic, ContainerTemplate);
            }));

            Bindings.Add(
                this.SetBinding(() => ViewModel.EmptyNoticeVisibility,
                    () => EmptyNotice.Visibility).ConvertSourceToTarget(Converters.BoolToVisibility));

            
        }

        public override int LayoutResourceId => Resource.Layout.ClubsIndexMyClubsTab;

        #region Views

        private ListView _list;
        private TextView _emptyNotice;

        public ListView List => _list ?? (_list = FindViewById<ListView>(Resource.Id.List));

        public TextView EmptyNotice => _emptyNotice ?? (_emptyNotice = FindViewById<TextView>(Resource.Id.EmptyNotice));

        #endregion
    }
}