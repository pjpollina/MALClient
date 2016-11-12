using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content.Res;
using Android.Views;
using Android.Widget;
using MALClient.Android.Activities;

namespace MALClient.Android.Resources
{
    public static class ResourceExtension
    {
        public static readonly int BrushText = ResourcesCompat.GetColor(MainActivity.CurrentContext.Resources,
            Resource.Color.BrushText, null);

        public static readonly int AccentColour = ResourcesCompat.GetColor(MainActivity.CurrentContext.Resources,
            Resource.Color.AccentColour, null);

        public static readonly int BrushAnimeItemInnerBackground = ResourcesCompat.GetColor(MainActivity.CurrentContext.Resources,
            Resource.Color.BrushAnimeItemInnerBackground, null);

        public static readonly int BrushSelectedDialogItem = ResourcesCompat.GetColor(MainActivity.CurrentContext.Resources,
            Resource.Color.BrushSelectedDialogItem, null);

        public static readonly int BrushFlyoutBackground = ResourcesCompat.GetColor(MainActivity.CurrentContext.Resources,
            Resource.Color.BrushFlyoutBackground, null);

        public static readonly int BrushRowAlternate1 = ResourcesCompat.GetColor(MainActivity.CurrentContext.Resources,
            Resource.Color.BrushRowAlternate1, null);

        public static readonly int BrushRowAlternate2 = ResourcesCompat.GetColor(MainActivity.CurrentContext.Resources,
            Resource.Color.BrushRowAlternate2, null);
    }
}